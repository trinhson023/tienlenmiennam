using System.Collections.Concurrent;
using System.Security.Cryptography;
using SamLocService.Domain.Cards;
using SamLocService.Domain.Matches;

namespace SamLocService.Application.Matches;

public sealed class SamMatchApplicationService(IMatchStore store, MatchRuntimeSettings settings)
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _roomCreationGates = new();

    public async Task<CreateMatchResult> CreateMatchAsync(CreateMatchRequest request, CancellationToken ct)
    {
        var validation = ValidateCreateRequest(request);
        if (validation is not null) return validation;

        var gate = _roomCreationGates.GetOrAdd(request.RoomId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var existing = await store.GetLatestByRoomAsync(request.RoomId, ct);
            if (existing is not null && existing.Match.Status != SamMatchStatus.Completed)
                return CreateMatchResult.Success(existing.Match.Id);

            return await CreateFreshMatchAsync(request, null, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<CreateMatchResult> CreateRematchAsync(Guid previousMatchId, CreateMatchRequest request, CancellationToken ct)
    {
        var validation = ValidateCreateRequest(request);
        if (validation is not null) return validation;

        var previous = await store.GetAsync(previousMatchId, ct);
        if (previous is null)
            return CreateMatchResult.Failure("rematch_match_not_found", "Không tìm thấy ván Sâm trước.");

        Guid? preferredStarter;
        await previous.Gate.WaitAsync(ct);
        try
        {
            if (previous.Match.Status != SamMatchStatus.Completed)
                return CreateMatchResult.Failure("rematch_not_ready", "Ván hiện tại chưa kết thúc.");
            if (previous.RoomId != request.RoomId)
                return CreateMatchResult.Failure("rematch_room_mismatch", "Room không khớp với ván trước.");

            var oldPlayers = previous.Players.Values.OrderBy(x => x.SeatNumber).ToArray();
            var newPlayers = request.Players.OrderBy(x => x.SeatNumber).ToArray();
            if (oldPlayers.Length != newPlayers.Length ||
                oldPlayers.Zip(newPlayers).Any(pair =>
                    pair.First.UserId != pair.Second.UserId ||
                    pair.First.SeatNumber != pair.Second.SeatNumber ||
                    pair.First.IsBot != pair.Second.IsBot))
                return CreateMatchResult.Failure("rematch_roster_changed", "Danh sách ghế đã thay đổi so với ván trước.");

            preferredStarter = previous.Match.WinnerId;
        }
        finally
        {
            previous.Gate.Release();
        }

        var gate = _roomCreationGates.GetOrAdd(request.RoomId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var latest = await store.GetLatestByRoomAsync(request.RoomId, ct);
            if (latest is not null && latest.Match.Status != SamMatchStatus.Completed)
                return CreateMatchResult.Success(latest.Match.Id);

            return await CreateFreshMatchAsync(request, preferredStarter, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<MatchSummaryView?> GetSummaryAsync(Guid matchId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return null;

        await runtime.Gate.WaitAsync(ct);
        try
        {
            return new MatchSummaryView(runtime.Match.Id, runtime.RoomId, runtime.Match.Status.ToString());
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    public async Task<MatchCommandResult> GetStateAsync(Guid matchId, Guid userId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván Sâm.");

        await runtime.Gate.WaitAsync(ct);
        try
        {
            if (!runtime.Players.ContainsKey(userId) || runtime.IsAbandoned(userId))
                return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này.");
            return MatchCommandResult.Success(Project(runtime, userId));
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetParticipantIdsAsync(Guid matchId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return Array.Empty<Guid>();

        await runtime.Gate.WaitAsync(ct);
        try
        {
            return runtime.Players.Keys.Where(x => !runtime.IsAbandoned(x)).ToArray();
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    public Task<IReadOnlyList<Guid>> GetActiveMatchIdsAsync(CancellationToken ct) => store.GetActiveMatchIdsAsync(ct);

    public async Task<MatchCommandResult> DeclareSamAsync(Guid matchId, Guid userId, long expectedVersion, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván Sâm.");

        await runtime.Gate.WaitAsync(ct);
        try
        {
            var guard = ValidateCommand(runtime, userId, expectedVersion);
            if (guard is not null) return guard;

            var result = runtime.Match.DeclareSam(userId);
            if (!result.IsSuccess)
                return MatchCommandResult.Failure(result.ErrorCode ?? "declare_failed", result.ErrorMessage ?? "Không thể báo Sâm.");

            await CommitActionAsync(runtime, DateTimeOffset.UtcNow, ct);
            return MatchCommandResult.Success(Project(runtime, userId));
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    public async Task<MatchCommandResult> PlayCardsAsync(Guid matchId, Guid userId, IReadOnlyCollection<string>? cardCodes, long expectedVersion, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván Sâm.");

        await runtime.Gate.WaitAsync(ct);
        try
        {
            var guard = ValidateCommand(runtime, userId, expectedVersion);
            if (guard is not null) return guard;
            if (cardCodes is null || cardCodes.Count == 0)
                return MatchCommandResult.Failure("invalid_cards", "Chưa chọn lá bài nào.");

            var cards = new List<Card>(cardCodes.Count);
            foreach (var code in cardCodes)
            {
                if (!CardCode.TryParse(code, out var card))
                    return MatchCommandResult.Failure("invalid_card_code", $"Mã lá bài '{code}' không hợp lệ.");
                cards.Add(card);
            }

            var result = runtime.Match.Play(userId, cards);
            if (!result.IsSuccess)
                return MatchCommandResult.Failure(result.ErrorCode ?? "invalid_play", result.ErrorMessage ?? "Nước đánh không hợp lệ.");

            await CommitActionAsync(runtime, DateTimeOffset.UtcNow, ct);
            return MatchCommandResult.Success(Project(runtime, userId));
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    public async Task<MatchCommandResult> PassAsync(Guid matchId, Guid userId, long expectedVersion, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván Sâm.");

        await runtime.Gate.WaitAsync(ct);
        try
        {
            var guard = ValidateCommand(runtime, userId, expectedVersion);
            if (guard is not null) return guard;

            var result = runtime.Match.Pass(userId);
            if (!result.IsSuccess)
                return MatchCommandResult.Failure(result.ErrorCode ?? "pass_failed", result.ErrorMessage ?? "Không thể bỏ lượt.");

            await CommitActionAsync(runtime, DateTimeOffset.UtcNow, ct);
            return MatchCommandResult.Success(Project(runtime, userId));
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    public async Task<MatchCommandResult> AbandonAsync(Guid matchId, Guid userId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván Sâm.");

        await runtime.Gate.WaitAsync(ct);
        try
        {
            if (!runtime.Players.ContainsKey(userId))
                return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này.");
            if (runtime.Match.Status == SamMatchStatus.Completed)
                return MatchCommandResult.Failure("match_completed", "Ván đã kết thúc.");
            if (runtime.IsAbandoned(userId))
                return MatchCommandResult.Success(ProjectForParticipant(runtime, userId));

            runtime.Abandon(userId);
            runtime.Version++;

            var now = DateTimeOffset.UtcNow;
            if (runtime.Match.Status == SamMatchStatus.InProgress && runtime.Match.CurrentPlayerId == userId)
                runtime.BotActionDueUtc = now.AddMilliseconds(Math.Max(100, settings.BotMinDelayMs));

            await store.SaveAsync(runtime, ct);
            return MatchCommandResult.Success(ProjectForParticipant(runtime, userId));
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    public async Task<bool> ProcessAutomationAsync(Guid matchId, DateTimeOffset now, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct);
        if (runtime is null) return false;

        await runtime.Gate.WaitAsync(ct);
        try
        {
            if (runtime.Match.Status == SamMatchStatus.Completed) return false;

            if (runtime.Match.Status == SamMatchStatus.Declaring)
            {
                if (!runtime.DeclarationDeadlineUtc.HasValue || runtime.DeclarationDeadlineUtc.Value > now) return false;
                var close = runtime.Match.CloseDeclaration(runtime.DefaultStarterUserId);
                if (!close.IsSuccess) return false;
                await CommitActionAsync(runtime, now, ct);
                return true;
            }

            var currentId = runtime.Match.CurrentPlayerId;
            if (!currentId.HasValue || !runtime.Players.TryGetValue(currentId.Value, out var identity)) return false;

            var automated = identity.IsBot || runtime.IsAbandoned(currentId.Value);
            var botDue = automated && runtime.BotActionDueUtc.HasValue && runtime.BotActionDueUtc.Value <= now;
            var timeoutDue = runtime.TurnDeadlineUtc.HasValue && runtime.TurnDeadlineUtc.Value <= now;
            if (!botDue && !timeoutDue) return false;

            var player = runtime.Match.Players.Single(x => x.UserId == currentId.Value);
            var legal = LegalMoveGenerator.FindLowestLegalPlay(runtime.Match, player);
            SamMatchActionResult action;

            if (runtime.Match.Center is null)
            {
                if (legal is null) return false;
                action = runtime.Match.Play(currentId.Value, legal);
            }
            else if (automated && legal is not null)
            {
                action = runtime.Match.Play(currentId.Value, legal);
            }
            else
            {
                action = runtime.Match.Pass(currentId.Value);
            }

            if (!action.IsSuccess) return false;
            await CommitActionAsync(runtime, now, ct);
            return true;
        }
        finally
        {
            runtime.Gate.Release();
        }
    }

    private async Task<CreateMatchResult> CreateFreshMatchAsync(CreateMatchRequest request, Guid? preferredStarterUserId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var orderedPlayers = request.Players.OrderBy(x => x.SeatNumber).ToArray();
        var deck = DeckFactory.CreateStandardDeck();
        Shuffle(deck);

        // SamMatch uses compact 0..N-1 game seats for turn order. Lobby seat numbers
        // remain in MatchPlayerIdentity and are what clients see.
        var setups = orderedPlayers.Select((player, index) => new PlayerSetup(player.UserId, index)).ToArray();
        var match = SamMatch.Create(Guid.NewGuid(), setups, deck);
        var identities = orderedPlayers
            .Select(x => new MatchPlayerIdentity(x.UserId, x.SeatNumber, x.Username, x.DisplayName, x.IsBot))
            .ToArray();

        var starter = preferredStarterUserId.HasValue && identities.Any(x => x.UserId == preferredStarterUserId.Value)
            ? preferredStarterUserId.Value
            : identities[RandomNumberGenerator.GetInt32(identities.Length)].UserId;

        var runtime = new MatchRuntime(
            request.RoomId,
            match,
            identities,
            starter,
            declarationDeadlineUtc: now.AddSeconds(Math.Max(1, settings.DeclarationSeconds)),
            createdAtUtc: now);

        if (!await store.TryAddAsync(runtime, ct))
            return CreateMatchResult.Failure("match_conflict", "Không thể tạo match Sâm mới.");

        return CreateMatchResult.Success(match.Id);
    }

    private static CreateMatchResult? ValidateCreateRequest(CreateMatchRequest request)
    {
        if (request.RoomId == Guid.Empty)
            return CreateMatchResult.Failure("invalid_room", "RoomId không hợp lệ.");
        if (request.Players is null || request.Players.Count is < 2 or > 4)
            return CreateMatchResult.Failure("invalid_players", "Sâm Lốc yêu cầu 2-4 người chơi.");
        if (request.Players.Select(x => x.UserId).Distinct().Count() != request.Players.Count || request.Players.Any(x => x.UserId == Guid.Empty))
            return CreateMatchResult.Failure("duplicate_player", "Danh sách người chơi bị trùng hoặc không hợp lệ.");
        if (request.Players.Select(x => x.SeatNumber).Distinct().Count() != request.Players.Count || request.Players.Any(x => x.SeatNumber is < 0 or > 3))
            return CreateMatchResult.Failure("invalid_seat", "Ghế người chơi không hợp lệ.");
        return null;
    }

    private static MatchCommandResult? ValidateCommand(MatchRuntime runtime, Guid userId, long expectedVersion)
    {
        if (!runtime.Players.ContainsKey(userId) || runtime.IsAbandoned(userId))
            return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này.");
        if (expectedVersion != runtime.Version)
            return MatchCommandResult.Failure("stale_state", "State của client đã cũ, hãy đồng bộ lại.");
        return null;
    }

    private async Task CommitActionAsync(MatchRuntime runtime, DateTimeOffset now, CancellationToken ct)
    {
        runtime.Version++;

        if (runtime.Match.Status == SamMatchStatus.Completed)
        {
            runtime.CompletedAtUtc ??= now;
            runtime.DeclarationDeadlineUtc = null;
            runtime.TurnDeadlineUtc = null;
            runtime.BotActionDueUtc = null;
        }
        else if (runtime.Match.Status == SamMatchStatus.Declaring)
        {
            runtime.DeclarationDeadlineUtc ??= now.AddSeconds(Math.Max(1, settings.DeclarationSeconds));
            runtime.TurnDeadlineUtc = null;
            runtime.BotActionDueUtc = null;
        }
        else
        {
            ScheduleTurn(runtime, now);
        }

        await store.SaveAsync(runtime, ct);
    }

    private void ScheduleTurn(MatchRuntime runtime, DateTimeOffset now)
    {
        runtime.DeclarationDeadlineUtc = null;
        runtime.TurnDeadlineUtc = now.AddSeconds(Math.Max(1, settings.TurnSeconds));
        runtime.BotActionDueUtc = null;

        var current = runtime.Match.CurrentPlayerId;
        if (!current.HasValue || !runtime.Players.TryGetValue(current.Value, out var identity)) return;
        if (!identity.IsBot && !runtime.IsAbandoned(current.Value)) return;

        var min = Math.Max(100, settings.BotMinDelayMs);
        var maxExclusive = Math.Max(min + 1, settings.BotMaxDelayMs + 1);
        runtime.BotActionDueUtc = now.AddMilliseconds(Random.Shared.Next(min, maxExclusive));
    }

    private static MatchStateView Project(MatchRuntime runtime, Guid viewerUserId) => ProjectInternal(runtime, viewerUserId, true);
    private static MatchStateView ProjectForParticipant(MatchRuntime runtime, Guid viewerUserId) => ProjectInternal(runtime, viewerUserId, false);

    private static MatchStateView ProjectInternal(MatchRuntime runtime, Guid viewerUserId, bool includeHand)
    {
        var match = runtime.Match;
        var domainPlayers = match.Players.ToDictionary(x => x.UserId);
        var players = runtime.Players.Values.OrderBy(x => x.SeatNumber).Select(identity =>
        {
            var player = domainPlayers[identity.UserId];
            var automated = identity.IsBot || runtime.IsAbandoned(identity.UserId);
            return new MatchPlayerView(
                identity.UserId,
                identity.SeatNumber,
                identity.Username,
                identity.DisplayName,
                player.Hand.Count,
                player.HasPlayedAny,
                includeHand && identity.UserId == viewerUserId,
                automated);
        }).ToArray();

        var current = match.CurrentPlayerId;
        var currentIsBot = current.HasValue &&
            runtime.Players.TryGetValue(current.Value, out var currentIdentity) &&
            (currentIdentity.IsBot || runtime.IsAbandoned(current.Value));

        var hand = includeHand
            ? domainPlayers[viewerUserId].Hand.Select(x => x.Code).ToArray()
            : Array.Empty<string>();

        return new MatchStateView(
            match.Id,
            runtime.RoomId,
            runtime.Version,
            match.Status.ToString(),
            current,
            currentIsBot,
            runtime.DeclarationDeadlineUtc,
            runtime.TurnDeadlineUtc,
            match.DeclarationState.ToString(),
            match.SamDeclarerId,
            match.Center?.Type.ToString(),
            match.Center?.Cards.Select(x => x.Code).ToArray() ?? Array.Empty<string>(),
            hand,
            players,
            match.WinnerId);
    }

    private static void Shuffle(Card[] cards)
    {
        for (var i = cards.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}

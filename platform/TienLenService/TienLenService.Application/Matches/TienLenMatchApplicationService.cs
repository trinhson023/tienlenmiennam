using System.Collections.Concurrent;
using System.Security.Cryptography;
using TienLenService.Domain.Cards;
using TienLenService.Domain.Matches;

namespace TienLenService.Application.Matches;

public sealed class TienLenMatchApplicationService(IMatchStore store, MatchRuntimeSettings settings)
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _roomCreationGates = new();

    public async Task<CreateMatchResult> CreateMatchAsync(CreateMatchRequest request, CancellationToken ct)
    {
        if (request.RoomId == Guid.Empty) return CreateMatchResult.Failure("invalid_room", "RoomId không hợp lệ.");
        if (request.Players is null || request.Players.Count is < 2 or > 4) return CreateMatchResult.Failure("invalid_players", "Tiến Lên yêu cầu 2-4 người chơi.");
        if (request.Players.Select(x => x.UserId).Distinct().Count() != request.Players.Count) return CreateMatchResult.Failure("duplicate_player", "Danh sách người chơi bị trùng.");
        if (request.Players.Select(x => x.SeatNumber).Distinct().Count() != request.Players.Count || request.Players.Any(x => x.SeatNumber is < 0 or > 3)) return CreateMatchResult.Failure("invalid_seat", "Ghế người chơi không hợp lệ.");
        var gate = _roomCreationGates.GetOrAdd(request.RoomId, _ => new SemaphoreSlim(1, 1)); await gate.WaitAsync(ct);
        try
        {
            var existing = await store.GetLatestByRoomAsync(request.RoomId, ct); if (existing is not null && existing.Match.Status == MatchStatus.InProgress) return CreateMatchResult.Success(existing.Match.Id.Value);
            var deck = DeckFactory.CreateStandardDeck().ToArray(); Shuffle(deck); var orderedPlayers = request.Players.OrderBy(x => x.SeatNumber).ToArray(); var hands = DeckFactory.DealChunked(deck, orderedPlayers.Length);
            var setups = orderedPlayers.Select((player, index) => new PlayerSetup(new PlayerId(player.UserId), new SeatNumber(player.SeatNumber), hands[index])).ToArray(); var now = DateTimeOffset.UtcNow; var match = TienLenMatch.Create(MatchId.New(), setups);
            var identities = orderedPlayers.Select(x => new MatchPlayerIdentity(x.UserId, x.SeatNumber, x.Username, x.DisplayName, x.IsBot)).ToArray(); var runtime = new MatchRuntime(request.RoomId, match, identities, createdAtUtc: now); ScheduleTurn(runtime, now);
            if (!await store.TryAddAsync(runtime, ct)) return CreateMatchResult.Failure("match_conflict", "Không thể tạo match mới."); return CreateMatchResult.Success(match.Id.Value);
        }
        finally { gate.Release(); }
    }

    public async Task<CreateMatchResult> CreateRematchAsync(Guid previousMatchId, CreateMatchRequest request, CancellationToken ct)
    {
        var previous = await store.GetAsync(previousMatchId, ct); if (previous is null) return CreateMatchResult.Failure("rematch_match_not_found", "Không tìm thấy ván trước."); if (request.Players is null) return CreateMatchResult.Failure("invalid_players", "Danh sách người chơi không hợp lệ.");
        await previous.Gate.WaitAsync(ct);
        try
        {
            if (previous.Match.Status != MatchStatus.Completed) return CreateMatchResult.Failure("rematch_not_ready", "Ván hiện tại chưa kết thúc.");
            if (previous.RoomId != request.RoomId) return CreateMatchResult.Failure("rematch_room_mismatch", "Room không khớp với ván trước.");
            var oldPlayers = previous.Players.Values.OrderBy(x => x.SeatNumber).ToArray(); var newPlayers = request.Players.OrderBy(x => x.SeatNumber).ToArray();
            if (oldPlayers.Length != newPlayers.Length || oldPlayers.Zip(newPlayers).Any(pair => pair.First.UserId != pair.Second.UserId || pair.First.SeatNumber != pair.Second.SeatNumber || pair.First.IsBot != pair.Second.IsBot)) return CreateMatchResult.Failure("rematch_roster_changed", "Danh sách ghế đã thay đổi so với ván trước.");
        }
        finally { previous.Gate.Release(); }
        return await CreateMatchAsync(request, ct);
    }

    public async Task<MatchSummaryView?> GetSummaryAsync(Guid matchId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct); if (runtime is null) return null;
        await runtime.Gate.WaitAsync(ct);
        try { return new MatchSummaryView(runtime.Match.Id.Value, runtime.RoomId, runtime.Match.Status.ToString()); }
        finally { runtime.Gate.Release(); }
    }

    public async Task<MatchCommandResult> AbandonAsync(Guid matchId, Guid userId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct); if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván chơi.");
        await runtime.Gate.WaitAsync(ct);
        try
        {
            if (!runtime.Players.ContainsKey(userId)) return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này.");
            if (runtime.Match.Status == MatchStatus.Completed) return MatchCommandResult.Failure("match_completed", "Ván đã kết thúc.");
            if (runtime.IsAbandoned(userId)) return MatchCommandResult.Success(ProjectForParticipant(runtime, userId));
            runtime.Abandon(userId);
            runtime.Version++;
            var now = DateTimeOffset.UtcNow;
            var currentId = runtime.Match.CurrentPlayerId?.Value;
            if (currentId == userId)
                runtime.BotActionDueUtc = now.AddMilliseconds(Math.Max(100, settings.BotMinDelayMs));
            await store.SaveAsync(runtime, ct);
            return MatchCommandResult.Success(ProjectForParticipant(runtime, userId));
        }
        finally { runtime.Gate.Release(); }
    }

    public async Task<MatchCommandResult> GetStateAsync(Guid matchId, Guid userId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct); if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván chơi."); await runtime.Gate.WaitAsync(ct);
        try { if (!runtime.Players.ContainsKey(userId) || runtime.IsAbandoned(userId)) return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này."); return MatchCommandResult.Success(Project(runtime, userId)); }
        finally { runtime.Gate.Release(); }
    }

    public async Task<IReadOnlyList<Guid>> GetParticipantIdsAsync(Guid matchId, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct); if (runtime is null) return [];
        await runtime.Gate.WaitAsync(ct);
        try { return runtime.Players.Keys.Where(x => !runtime.IsAbandoned(x)).ToArray(); }
        finally { runtime.Gate.Release(); }
    }

    public Task<IReadOnlyList<Guid>> GetActiveMatchIdsAsync(CancellationToken ct) => store.GetActiveMatchIdsAsync(ct);

    public async Task<MatchCommandResult> PlayCardsAsync(Guid matchId, Guid userId, IReadOnlyCollection<string>? cardCodes, long expectedVersion, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct); if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván chơi."); await runtime.Gate.WaitAsync(ct);
        try
        {
            if (!runtime.Players.ContainsKey(userId) || runtime.IsAbandoned(userId)) return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này."); if (expectedVersion != runtime.Version) return MatchCommandResult.Failure("stale_state", "State của client đã cũ, hãy đồng bộ lại."); if (cardCodes is null || cardCodes.Count == 0) return MatchCommandResult.Failure("invalid_cards", "Chưa chọn lá bài nào.");
            var cards = new List<Card>(cardCodes.Count); foreach (var code in cardCodes) { if (!CardCode.TryParse(code, out var card)) return MatchCommandResult.Failure("invalid_card_code", $"Mã lá bài '{code}' không hợp lệ."); cards.Add(card); }
            var result = runtime.Match.PlayCards(new PlayerId(userId), cards); if (!result.IsSuccess) return MatchCommandResult.Failure(result.ValidationCode?.ToString() ?? result.Error.ToString(), result.Message ?? "Nước đánh không hợp lệ."); await CommitActionAsync(runtime, DateTimeOffset.UtcNow, ct); return MatchCommandResult.Success(Project(runtime, userId));
        }
        finally { runtime.Gate.Release(); }
    }

    public async Task<MatchCommandResult> PassAsync(Guid matchId, Guid userId, long expectedVersion, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct); if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván chơi."); await runtime.Gate.WaitAsync(ct);
        try { if (!runtime.Players.ContainsKey(userId) || runtime.IsAbandoned(userId)) return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này."); if (expectedVersion != runtime.Version) return MatchCommandResult.Failure("stale_state", "State của client đã cũ, hãy đồng bộ lại."); var result = runtime.Match.Pass(new PlayerId(userId)); if (!result.IsSuccess) return MatchCommandResult.Failure(result.Error.ToString(), result.Message ?? "Không thể bỏ lượt."); await CommitActionAsync(runtime, DateTimeOffset.UtcNow, ct); return MatchCommandResult.Success(Project(runtime, userId)); }
        finally { runtime.Gate.Release(); }
    }

    public async Task<bool> ProcessAutomationAsync(Guid matchId, DateTimeOffset now, CancellationToken ct)
    {
        var runtime = await store.GetAsync(matchId, ct); if (runtime is null) return false; await runtime.Gate.WaitAsync(ct);
        try
        {
            if (runtime.Match.Status == MatchStatus.Completed || runtime.Match.CurrentPlayerId is null) return false; var currentId = runtime.Match.CurrentPlayerId.Value.Value; if (!runtime.Players.TryGetValue(currentId, out var identity)) return false;
            var automated = identity.IsBot || runtime.IsAbandoned(currentId);
            var botDue = automated && runtime.BotActionDueUtc.HasValue && runtime.BotActionDueUtc.Value <= now; var timeoutDue = runtime.TurnDeadlineUtc.HasValue && runtime.TurnDeadlineUtc.Value <= now; if (!botDue && !timeoutDue) return false;
            var player = runtime.Match.Players.Single(x => x.Id.Value == currentId); var legal = LegalMoveGenerator.FindLowestLegalPlay(runtime.Match, player); MatchActionResult action;
            if (runtime.Match.Center.Count == 0) { if (legal is null) return false; action = runtime.Match.PlayCards(player.Id, legal); } else if (automated && legal is not null) action = runtime.Match.PlayCards(player.Id, legal); else action = runtime.Match.Pass(player.Id);
            if (!action.IsSuccess) return false; await CommitActionAsync(runtime, now, ct); return true;
        }
        finally { runtime.Gate.Release(); }
    }

    private async Task CommitActionAsync(MatchRuntime runtime, DateTimeOffset now, CancellationToken ct) { runtime.Version++; if (runtime.Match.Status == MatchStatus.Completed) { runtime.CompletedAtUtc ??= now; runtime.TurnDeadlineUtc = null; runtime.BotActionDueUtc = null; } else ScheduleTurn(runtime, now); await store.SaveAsync(runtime, ct); }
    private void ScheduleTurn(MatchRuntime runtime, DateTimeOffset now) { runtime.TurnDeadlineUtc = now.AddSeconds(settings.TurnSeconds); runtime.BotActionDueUtc = null; var current = runtime.Match.CurrentPlayerId?.Value; if (current.HasValue && runtime.Players.TryGetValue(current.Value, out var identity) && (identity.IsBot || runtime.IsAbandoned(current.Value))) { var maxExclusive = Math.Max(settings.BotMinDelayMs + 1, settings.BotMaxDelayMs + 1); runtime.BotActionDueUtc = now.AddMilliseconds(Random.Shared.Next(settings.BotMinDelayMs, maxExclusive)); } }

    private static MatchStateView Project(MatchRuntime runtime, Guid viewerUserId) => ProjectInternal(runtime, viewerUserId, true);
    private static MatchStateView ProjectForParticipant(MatchRuntime runtime, Guid viewerUserId) => ProjectInternal(runtime, viewerUserId, false);

    private static MatchStateView ProjectInternal(MatchRuntime runtime, Guid viewerUserId, bool includeHand)
    {
        var match = runtime.Match;
        var players = match.Players.OrderBy(x => x.Seat.Value).Select(player =>
        {
            var identity = runtime.Players[player.Id.Value];
            var automated = identity.IsBot || runtime.IsAbandoned(player.Id.Value);
            return new MatchPlayerView(player.Id.Value, player.Seat.Value, identity.Username, identity.DisplayName, player.Hand.Count, player.HasFinished, player.FinishPosition, includeHand && player.Id.Value == viewerUserId, automated);
        }).ToArray();
        var currentId = match.CurrentPlayerId?.Value;
        var currentIsBot = currentId.HasValue && runtime.Players.TryGetValue(currentId.Value, out var currentIdentity) && (currentIdentity.IsBot || runtime.IsAbandoned(currentId.Value));
        var hand = includeHand ? match.Players.Single(x => x.Id.Value == viewerUserId).Hand.Select(x => x.Code).ToArray() : [];
        return new MatchStateView(match.Id.Value, runtime.RoomId, runtime.Version, match.Status.ToString(), currentId, currentIsBot, runtime.TurnDeadlineUtc, match.IsOpeningPlay, match.CenterType?.ToString(), match.Center.Select(x => x.Code).ToArray(), hand, players, match.WinnerOrder.Select(x => x.Value).ToArray());
    }

    private static void Shuffle(Card[] cards) { for (var i = cards.Length - 1; i > 0; i--) { var j = RandomNumberGenerator.GetInt32(i + 1); (cards[i], cards[j]) = (cards[j], cards[i]); } }
}

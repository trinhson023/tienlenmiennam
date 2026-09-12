using System.Security.Cryptography;
using TienLenService.Domain.Cards;
using TienLenService.Domain.Matches;

namespace TienLenService.Application.Matches;

public sealed class TienLenMatchApplicationService(IMatchStore store)
{
    public CreateMatchResult CreateMatch(CreateMatchRequest request)
    {
        if (request.RoomId == Guid.Empty) return CreateMatchResult.Failure("invalid_room", "RoomId không hợp lệ.");
        if (request.Players is null || request.Players.Count is < 2 or > 4)
            return CreateMatchResult.Failure("invalid_players", "Tiến Lên yêu cầu 2-4 người chơi.");
        if (request.Players.Select(x => x.UserId).Distinct().Count() != request.Players.Count)
            return CreateMatchResult.Failure("duplicate_player", "Danh sách người chơi bị trùng.");
        if (request.Players.Select(x => x.SeatNumber).Distinct().Count() != request.Players.Count || request.Players.Any(x => x.SeatNumber is < 0 or > 3))
            return CreateMatchResult.Failure("invalid_seat", "Ghế người chơi không hợp lệ.");

        var existing = store.GetLatestByRoom(request.RoomId);
        if (existing is not null && existing.Match.Status == MatchStatus.InProgress)
            return CreateMatchResult.Success(existing.Match.Id.Value);

        var deck = DeckFactory.CreateStandardDeck().ToArray();
        Shuffle(deck);
        var orderedPlayers = request.Players.OrderBy(x => x.SeatNumber).ToArray();
        var hands = DeckFactory.DealChunked(deck, orderedPlayers.Length);
        var setups = orderedPlayers.Select((player, index) =>
            new PlayerSetup(new PlayerId(player.UserId), new SeatNumber(player.SeatNumber), hands[index])).ToArray();

        var match = TienLenMatch.Create(MatchId.New(), setups);
        var identities = orderedPlayers.Select(x => new MatchPlayerIdentity(x.UserId, x.SeatNumber, x.Username, x.DisplayName)).ToArray();
        var runtime = new MatchRuntime(request.RoomId, match, identities);
        if (!store.TryAdd(runtime)) return CreateMatchResult.Failure("match_conflict", "Không thể tạo match mới.");
        return CreateMatchResult.Success(match.Id.Value);
    }

    public MatchCommandResult GetState(Guid matchId, Guid userId)
    {
        var runtime = store.Get(matchId);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván chơi.");
        lock (runtime.SyncRoot)
        {
            if (!runtime.Players.ContainsKey(userId)) return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này.");
            return MatchCommandResult.Success(Project(runtime, userId));
        }
    }

    public IReadOnlyList<Guid> GetParticipantIds(Guid matchId)
    {
        var runtime = store.Get(matchId);
        return runtime is null ? [] : runtime.Players.Keys.ToArray();
    }

    public MatchCommandResult PlayCards(Guid matchId, Guid userId, IReadOnlyCollection<string>? cardCodes, long expectedVersion)
    {
        var runtime = store.Get(matchId);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván chơi.");
        lock (runtime.SyncRoot)
        {
            if (!runtime.Players.ContainsKey(userId)) return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này.");
            if (expectedVersion != runtime.Version) return MatchCommandResult.Failure("stale_state", "State của client đã cũ, hãy đồng bộ lại.");
            if (cardCodes is null || cardCodes.Count == 0) return MatchCommandResult.Failure("invalid_cards", "Chưa chọn lá bài nào.");

            var cards = new List<Card>(cardCodes.Count);
            foreach (var code in cardCodes)
            {
                if (!CardCode.TryParse(code, out var card)) return MatchCommandResult.Failure("invalid_card_code", $"Mã lá bài '{code}' không hợp lệ.");
                cards.Add(card);
            }

            var result = runtime.Match.PlayCards(new PlayerId(userId), cards);
            if (!result.IsSuccess) return MatchCommandResult.Failure(result.ValidationCode?.ToString() ?? result.Error.ToString(), result.Message ?? "Nước đánh không hợp lệ.");
            runtime.Version++;
            return MatchCommandResult.Success(Project(runtime, userId));
        }
    }

    public MatchCommandResult Pass(Guid matchId, Guid userId, long expectedVersion)
    {
        var runtime = store.Get(matchId);
        if (runtime is null) return MatchCommandResult.Failure("match_not_found", "Không tìm thấy ván chơi.");
        lock (runtime.SyncRoot)
        {
            if (!runtime.Players.ContainsKey(userId)) return MatchCommandResult.Failure("not_match_player", "Bạn không thuộc ván này.");
            if (expectedVersion != runtime.Version) return MatchCommandResult.Failure("stale_state", "State của client đã cũ, hãy đồng bộ lại.");
            var result = runtime.Match.Pass(new PlayerId(userId));
            if (!result.IsSuccess) return MatchCommandResult.Failure(result.Error.ToString(), result.Message ?? "Không thể bỏ lượt.");
            runtime.Version++;
            return MatchCommandResult.Success(Project(runtime, userId));
        }
    }

    private static MatchStateView Project(MatchRuntime runtime, Guid viewerUserId)
    {
        var match = runtime.Match;
        var players = match.Players.OrderBy(x => x.Seat.Value).Select(player =>
        {
            var identity = runtime.Players[player.Id.Value];
            return new MatchPlayerView(
                player.Id.Value,
                player.Seat.Value,
                identity.Username,
                identity.DisplayName,
                player.Hand.Count,
                player.HasFinished,
                player.FinishPosition,
                player.Id.Value == viewerUserId);
        }).ToArray();

        var ownPlayer = match.Players.Single(x => x.Id.Value == viewerUserId);
        return new MatchStateView(
            match.Id.Value,
            runtime.RoomId,
            runtime.Version,
            match.Status.ToString(),
            match.CurrentPlayerId?.Value,
            match.IsOpeningPlay,
            match.CenterType?.ToString(),
            match.Center.Select(x => x.Code).ToArray(),
            ownPlayer.Hand.Select(x => x.Code).ToArray(),
            players,
            match.WinnerOrder.Select(x => x.Value).ToArray());
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

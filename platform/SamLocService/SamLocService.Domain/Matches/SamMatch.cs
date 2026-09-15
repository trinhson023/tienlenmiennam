using SamLocService.Domain.Cards;
using SamLocService.Domain.Rules;

namespace SamLocService.Domain.Matches;

public sealed class SamMatch
{
    private readonly List<SamMatchPlayer> _players;
    private readonly HashSet<Guid> _activePlayers;

    private SamMatch(
        Guid id,
        IEnumerable<SamMatchPlayer> players,
        SamMatchStatus status,
        Guid? currentPlayerId,
        Combination? center,
        Guid? lastPlayBy,
        IEnumerable<Guid> activePlayers,
        Guid? samDeclarerId,
        bool samWasBlocked,
        bool samSucceeded,
        Guid? winnerId)
    {
        Id = id;
        _players = players.OrderBy(x => x.SeatNumber).ToList();
        Status = status;
        CurrentPlayerId = currentPlayerId;
        Center = center;
        LastPlayBy = lastPlayBy;
        _activePlayers = activePlayers.ToHashSet();
        SamDeclarerId = samDeclarerId;
        SamWasBlocked = samWasBlocked;
        SamSucceeded = samSucceeded;
        WinnerId = winnerId;
    }

    public Guid Id { get; }
    public SamMatchStatus Status { get; private set; }
    public IReadOnlyList<SamMatchPlayer> Players => _players;
    public Guid? CurrentPlayerId { get; private set; }
    public Combination? Center { get; private set; }
    public Guid? LastPlayBy { get; private set; }
    public Guid? SamDeclarerId { get; private set; }
    public bool SamWasBlocked { get; private set; }
    public bool SamSucceeded { get; private set; }
    public Guid? WinnerId { get; private set; }
    public SamDeclarationState DeclarationState => SamDeclarerId is null ? SamDeclarationState.None : SamSucceeded ? SamDeclarationState.Succeeded : SamWasBlocked ? SamDeclarationState.Failed : SamDeclarationState.Active;

    public static SamMatch Create(Guid matchId, IReadOnlyCollection<PlayerSetup> setups, IReadOnlyList<Card> shuffledDeck)
    {
        if (matchId == Guid.Empty) throw new ArgumentException("MatchId is required.", nameof(matchId));
        if (setups.Count is < 2 or > 4) throw new ArgumentException("Sâm Lốc requires 2-4 players.", nameof(setups));
        var ordered = setups.OrderBy(x => x.SeatNumber).ToArray();
        if (ordered.Select(x => x.UserId).Distinct().Count() != ordered.Length || ordered.Any(x => x.UserId == Guid.Empty))
            throw new ArgumentException("Players must be unique and non-empty.", nameof(setups));
        if (ordered.Select(x => x.SeatNumber).Distinct().Count() != ordered.Length || ordered.Select(x => x.SeatNumber).OrderBy(x => x).Where((seat, index) => seat != index).Any())
            throw new ArgumentException("Seats must be contiguous from 0.", nameof(setups));
        var required = ordered.Length * 10;
        if (shuffledDeck.Count < required || shuffledDeck.Take(required).Distinct().Count() != required)
            throw new ArgumentException("Deck does not contain enough unique cards.", nameof(shuffledDeck));

        var hands = ordered.ToDictionary(x => x.UserId, _ => new List<Card>(10));
        for (var i = 0; i < required; i++) hands[ordered[i % ordered.Length].UserId].Add(shuffledDeck[i]);
        var players = ordered.Select(x => new SamMatchPlayer(x.UserId, x.SeatNumber, hands[x.UserId])).ToArray();
        return new SamMatch(matchId, players, SamMatchStatus.Declaring, null, null, null, players.Select(x => x.UserId), null, false, false, null);
    }

    public static SamMatch Restore(SamMatchSnapshot snapshot)
    {
        if (snapshot.Players.Length is < 2 or > 4) throw new ArgumentException("Snapshot player count is invalid.", nameof(snapshot));
        var players = snapshot.Players.Select(x => new SamMatchPlayer(x.UserId, x.SeatNumber, x.Hand, x.HasPlayedAny)).ToArray();
        var center = snapshot.CenterCards.Length == 0 ? null : CombinationDetector.TryCreate(snapshot.CenterCards)
            ?? throw new ArgumentException("Snapshot center is invalid.", nameof(snapshot));
        return new SamMatch(snapshot.MatchId, players, snapshot.Status, snapshot.CurrentPlayerId, center, snapshot.LastPlayBy,
            snapshot.ActivePlayerIds, snapshot.SamDeclarerId, snapshot.SamWasBlocked, snapshot.SamSucceeded, snapshot.WinnerId);
    }

    public SamMatchSnapshot CaptureSnapshot() => new(
        Id,
        Status,
        _players.Select(x => new SamMatchPlayerSnapshot(x.UserId, x.SeatNumber, x.Hand.ToArray(), x.HasPlayedAny)).ToArray(),
        CurrentPlayerId,
        Center?.Cards.ToArray() ?? [],
        LastPlayBy,
        _activePlayers.ToArray(),
        SamDeclarerId,
        SamWasBlocked,
        SamSucceeded,
        WinnerId);

    public SamMatchActionResult DeclareSam(Guid playerId)
    {
        if (Status != SamMatchStatus.Declaring) return SamMatchActionResult.Failure("declaration_closed", "Cửa sổ báo Sâm đã đóng.");
        if (FindPlayer(playerId) is null) return SamMatchActionResult.Failure("player_not_found", "Người chơi không thuộc ván.");
        SamDeclarerId = playerId;
        CurrentPlayerId = playerId;
        Status = SamMatchStatus.InProgress;
        return SamMatchActionResult.Success();
    }

    public SamMatchActionResult CloseDeclaration(Guid starterPlayerId)
    {
        if (Status != SamMatchStatus.Declaring) return SamMatchActionResult.Failure("declaration_closed", "Cửa sổ báo Sâm đã đóng.");
        if (FindPlayer(starterPlayerId) is null) return SamMatchActionResult.Failure("player_not_found", "Người chơi mở lượt không thuộc ván.");
        CurrentPlayerId = starterPlayerId;
        Status = SamMatchStatus.InProgress;
        return SamMatchActionResult.Success();
    }

    public SamMatchActionResult Play(Guid playerId, IReadOnlyCollection<Card> cards)
    {
        if (Status != SamMatchStatus.InProgress) return SamMatchActionResult.Failure("match_not_in_progress", "Ván chưa ở trạng thái chơi.");
        if (CurrentPlayerId != playerId) return SamMatchActionResult.Failure("not_your_turn", "Chưa tới lượt của bạn.");
        if (!_activePlayers.Contains(playerId)) return SamMatchActionResult.Failure("player_passed", "Bạn đã bỏ lượt trong vòng này.");
        if (cards.Count == 0 || cards.Distinct().Count() != cards.Count) return SamMatchActionResult.Failure("invalid_cards", "Bộ bài đánh ra không hợp lệ.");

        var player = FindPlayer(playerId)!;
        if (!player.Owns(cards)) return SamMatchActionResult.Failure("cards_not_owned", "Bạn không sở hữu đủ các lá bài này.");
        var candidate = CombinationDetector.TryCreate(cards);
        if (candidate is null) return SamMatchActionResult.Failure("invalid_combination", "Tổ hợp bài không hợp lệ trong Sâm Lốc.");
        var validation = PlayValidation.Validate(candidate, Center, player.Hand.Count);
        if (!validation.IsValid) return SamMatchActionResult.Failure(validation.ErrorCode!, validation.ErrorMessage!);

        if (SamDeclarerId.HasValue && !SamWasBlocked && LastPlayBy == SamDeclarerId && playerId != SamDeclarerId)
            SamWasBlocked = true;

        player.Play(candidate.Cards);
        Center = candidate;
        LastPlayBy = playerId;

        if (player.Hand.Count == 0)
        {
            WinnerId = playerId;
            SamSucceeded = SamDeclarerId == playerId && !SamWasBlocked;
            CurrentPlayerId = null;
            Status = SamMatchStatus.Completed;
            return SamMatchActionResult.Success();
        }

        CurrentPlayerId = NextActiveAfter(playerId);
        return SamMatchActionResult.Success();
    }

    public SamMatchActionResult Pass(Guid playerId)
    {
        if (Status != SamMatchStatus.InProgress) return SamMatchActionResult.Failure("match_not_in_progress", "Ván chưa ở trạng thái chơi.");
        if (CurrentPlayerId != playerId) return SamMatchActionResult.Failure("not_your_turn", "Chưa tới lượt của bạn.");
        if (Center is null || LastPlayBy is null) return SamMatchActionResult.Failure("cannot_pass_open_round", "Không thể bỏ lượt khi đang có quyền mở vòng.");

        _activePlayers.Remove(playerId);
        if (_activePlayers.Count <= 1)
        {
            var leader = LastPlayBy.Value;
            _activePlayers.Clear();
            foreach (var player in _players) _activePlayers.Add(player.UserId);
            Center = null;
            LastPlayBy = null;
            CurrentPlayerId = leader;
            return SamMatchActionResult.Success();
        }

        CurrentPlayerId = NextActiveAfter(playerId);
        return SamMatchActionResult.Success();
    }

    private SamMatchPlayer? FindPlayer(Guid playerId) => _players.SingleOrDefault(x => x.UserId == playerId);

    private Guid NextActiveAfter(Guid playerId)
    {
        var current = FindPlayer(playerId) ?? throw new InvalidOperationException("Current player not found.");
        for (var offset = 1; offset <= _players.Count; offset++)
        {
            var seat = (current.SeatNumber + offset) % _players.Count;
            var candidate = _players.Single(x => x.SeatNumber == seat);
            if (_activePlayers.Contains(candidate.UserId)) return candidate.UserId;
        }
        throw new InvalidOperationException("No active Sâm player found.");
    }
}

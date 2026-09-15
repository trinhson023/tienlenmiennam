using TienLenService.Domain.Cards;
using TienLenService.Domain.Rules;

namespace TienLenService.Domain.Matches;

public sealed class TienLenMatch
{
    private static readonly Card ThreeOfSpades = new(Rank.Three, Suit.Spades);
    private readonly List<MatchPlayer> _players;
    private readonly List<Card> _center = [];
    private readonly List<Card> _lastPlayedCards = [];
    private readonly List<PlayerId> _winnerOrder = [];
    private readonly HashSet<SeatNumber> _activeSeats = [];
    private SeatNumber? _lastPlaySeat;

    private TienLenMatch(MatchId id, IReadOnlyCollection<PlayerSetup> setups)
    {
        Id = id;
        _players = setups.Select(x => new MatchPlayer(x)).OrderBy(x => x.Seat.Value).ToList();
        foreach (var player in _players) _activeSeats.Add(player.Seat);
        CurrentSeat = OpeningRules.DetermineFirstSeat(_players);
        Status = MatchStatus.InProgress;
        IsOpeningPlay = true;
    }

    public MatchId Id { get; }
    public MatchStatus Status { get; private set; }
    public SeatNumber? CurrentSeat { get; private set; }
    public PlayerId? CurrentPlayerId => CurrentSeat is { } seat ? FindPlayer(seat).Id : null;
    public bool IsOpeningPlay { get; private set; }
    public IReadOnlyList<MatchPlayer> Players => _players;
    public IReadOnlyList<Card> Center => _center;
    public IReadOnlyList<Card> LastPlayedCards => _lastPlayedCards;
    public CombinationType? CenterType => _center.Count == 0 ? null : CombinationDetector.Detect(_center);
    public IReadOnlyList<PlayerId> WinnerOrder => _winnerOrder;

    public static TienLenMatch Create(MatchId id, IReadOnlyCollection<PlayerSetup> players)
    {
        if (players.Count is < 2 or > 4) throw new ArgumentOutOfRangeException(nameof(players), "Tiến Lên requires 2-4 players.");
        if (players.Select(x => x.PlayerId).Distinct().Count() != players.Count) throw new ArgumentException("Player ids must be unique.", nameof(players));
        if (players.Select(x => x.Seat).Distinct().Count() != players.Count) throw new ArgumentException("Seats must be unique.", nameof(players));
        if (players.Any(x => x.Hand.Count is < 1 or > 13)) throw new ArgumentException("Each hand must contain 1-13 cards.", nameof(players));
        var allCards = players.SelectMany(x => x.Hand).ToArray();
        if (allCards.Distinct().Count() != allCards.Length) throw new ArgumentException("The match contains duplicate physical cards.", nameof(players));
        return new TienLenMatch(id, players);
    }

    public TienLenMatchSnapshot CaptureSnapshot() => new(
        Id.Value,
        Status,
        CurrentSeat?.Value,
        IsOpeningPlay,
        _center.Select(x => x.Code).ToArray(),
        _winnerOrder.Select(x => x.Value).ToArray(),
        _activeSeats.Select(x => x.Value).OrderBy(x => x).ToArray(),
        _lastPlaySeat?.Value,
        _players.Select(x => new MatchPlayerSnapshot(
            x.Id.Value,
            x.Seat.Value,
            x.Hand.Select(card => card.Code).ToArray(),
            x.FinishPosition)).ToArray(),
        _lastPlayedCards.Select(x => x.Code).ToArray());

    public static TienLenMatch Restore(TienLenMatchSnapshot snapshot)
    {
        if (snapshot.Players.Count is < 2 or > 4) throw new InvalidOperationException("Persisted Tiến Lên match has invalid player count.");
        var setups = snapshot.Players.Select(x => new PlayerSetup(
            new PlayerId(x.PlayerId),
            new SeatNumber(x.SeatNumber),
            x.Hand.Select(CardCode.Parse).ToArray())).ToArray();

        var match = new TienLenMatch(new MatchId(snapshot.MatchId), setups)
        {
            Status = snapshot.Status,
            CurrentSeat = snapshot.CurrentSeat.HasValue ? new SeatNumber(snapshot.CurrentSeat.Value) : null,
            IsOpeningPlay = snapshot.IsOpeningPlay,
            _lastPlaySeat = snapshot.LastPlaySeat.HasValue ? new SeatNumber(snapshot.LastPlaySeat.Value) : null
        };

        match._center.Clear();
        match._center.AddRange(snapshot.Center.Select(CardCode.Parse));
        match._lastPlayedCards.Clear();
        match._lastPlayedCards.AddRange((snapshot.LastPlayedCards ?? snapshot.Center).Select(CardCode.Parse));
        match._winnerOrder.Clear();
        match._winnerOrder.AddRange(snapshot.WinnerOrder.Select(x => new PlayerId(x)));
        match._activeSeats.Clear();
        foreach (var seat in snapshot.ActiveSeats) match._activeSeats.Add(new SeatNumber(seat));

        foreach (var persisted in snapshot.Players)
        {
            if (!persisted.FinishPosition.HasValue) continue;
            match.FindPlayer(new PlayerId(persisted.PlayerId))!.MarkFinished(persisted.FinishPosition.Value);
        }

        return match;
    }

    public MatchActionResult PlayCards(PlayerId playerId, IReadOnlyCollection<Card>? cards)
    {
        var readiness = ValidateActor(playerId);
        if (readiness is not null) return readiness;
        if (cards is null || cards.Count == 0 || cards.Distinct().Count() != cards.Count)
            return MatchActionResult.Failure(MatchActionError.InvalidSelection, "Bài đã chọn không hợp lệ.");

        var player = FindPlayer(playerId)!;
        if (!player.HasCards(cards)) return MatchActionResult.Failure(MatchActionError.CardNotOwned, "Có lá bài không thuộc tay người chơi.");

        var openingThreeRequired = IsOpeningPlay && player.Hand.Contains(ThreeOfSpades);
        var validation = PlayValidator.Validate(cards, _center, openingThreeRequired, player.Hand.Count);
        if (!validation.IsValid)
            return MatchActionResult.Failure(MatchActionError.InvalidPlay, validation.Message, validation.Code);

        var orderedPlay = cards.OrderBy(x => x, CardComparer.Instance).ToArray();
        player.RemoveCards(cards);
        _center.Clear();
        _center.AddRange(orderedPlay);
        _lastPlayedCards.Clear();
        _lastPlayedCards.AddRange(orderedPlay);
        _lastPlaySeat = player.Seat;
        IsOpeningPlay = false;

        var playerFinished = player.Hand.Count == 0;
        if (playerFinished)
        {
            player.MarkFinished(_winnerOrder.Count + 1);
            _winnerOrder.Add(player.Id);
            _activeSeats.Remove(player.Seat);
        }

        if (CompleteIfNeeded())
            return MatchActionResult.Success(validation.IsChop, playerFinished, false, true);

        var trickReset = AdvanceAfterAction(player.Seat);
        return MatchActionResult.Success(validation.IsChop, playerFinished, trickReset, false);
    }

    public MatchActionResult Pass(PlayerId playerId)
    {
        var readiness = ValidateActor(playerId);
        if (readiness is not null) return readiness;
        if (_center.Count == 0)
            return MatchActionResult.Failure(MatchActionError.CannotPassOpenRound, "Không thể bỏ lượt khi đang mở vòng.");

        var player = FindPlayer(playerId)!;
        _activeSeats.Remove(player.Seat);
        var trickReset = AdvanceAfterAction(player.Seat);
        return MatchActionResult.Success(trickReset: trickReset);
    }

    private MatchActionResult? ValidateActor(PlayerId playerId)
    {
        if (Status == MatchStatus.Completed) return MatchActionResult.Failure(MatchActionError.MatchCompleted, "Ván đã kết thúc.");
        var player = FindPlayer(playerId);
        if (player is null) return MatchActionResult.Failure(MatchActionError.PlayerNotFound, "Người chơi không thuộc ván này.");
        if (CurrentSeat is null || player.Seat != CurrentSeat.Value) return MatchActionResult.Failure(MatchActionError.NotYourTurn, "Chưa đến lượt người chơi.");
        return null;
    }

    private bool AdvanceAfterAction(SeatNumber actingSeat)
    {
        if (_activeSeats.Count <= 1)
        {
            var leader = _lastPlaySeat ?? actingSeat;
            if (FindPlayer(leader).HasFinished) leader = FindNextNonFinishedSeat(leader);

            _center.Clear();
            _lastPlaySeat = null;
            _activeSeats.Clear();
            foreach (var player in _players.Where(x => !x.HasFinished)) _activeSeats.Add(player.Seat);
            CurrentSeat = leader;
            return true;
        }

        CurrentSeat = FindNextActiveSeat(actingSeat);
        return false;
    }

    private bool CompleteIfNeeded()
    {
        if (_winnerOrder.Count != _players.Count - 1) return false;
        var remaining = _players.Single(x => !x.HasFinished);
        remaining.MarkFinished(_winnerOrder.Count + 1);
        _winnerOrder.Add(remaining.Id);
        Status = MatchStatus.Completed;
        CurrentSeat = null;
        _activeSeats.Clear();
        return true;
    }

    private SeatNumber FindNextActiveSeat(SeatNumber after) => FindNextSeat(after, player => _activeSeats.Contains(player.Seat));
    private SeatNumber FindNextNonFinishedSeat(SeatNumber after) => FindNextSeat(after, player => !player.HasFinished);

    private SeatNumber FindNextSeat(SeatNumber after, Func<MatchPlayer, bool> predicate)
    {
        var start = _players.FindIndex(x => x.Seat == after);
        for (var offset = 1; offset <= _players.Count; offset++)
        {
            var candidate = _players[(start + offset) % _players.Count];
            if (predicate(candidate)) return candidate.Seat;
        }
        throw new InvalidOperationException("No eligible next player.");
    }

    private MatchPlayer? FindPlayer(PlayerId id) => _players.SingleOrDefault(x => x.Id == id);
    private MatchPlayer FindPlayer(SeatNumber seat) => _players.Single(x => x.Seat == seat);
}

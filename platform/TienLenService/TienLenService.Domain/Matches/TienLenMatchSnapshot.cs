namespace TienLenService.Domain.Matches;

public sealed record MatchPlayerSnapshot(
    Guid PlayerId,
    int SeatNumber,
    IReadOnlyList<string> Hand,
    int? FinishPosition);

public sealed record TienLenMatchSnapshot(
    Guid MatchId,
    MatchStatus Status,
    int? CurrentSeat,
    bool IsOpeningPlay,
    IReadOnlyList<string> Center,
    IReadOnlyList<Guid> WinnerOrder,
    IReadOnlyList<int> ActiveSeats,
    int? LastPlaySeat,
    IReadOnlyList<MatchPlayerSnapshot> Players,
    IReadOnlyList<string>? LastPlayedCards = null);

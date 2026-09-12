namespace TienLenService.Application.Matches;

public sealed record MatchPlayerSeed(Guid UserId, int SeatNumber, string Username, string DisplayName);
public sealed record CreateMatchRequest(Guid RoomId, IReadOnlyList<MatchPlayerSeed> Players);
public sealed record CreateMatchResult(bool IsSuccess, Guid? MatchId, string? ErrorCode, string? ErrorMessage)
{
    public static CreateMatchResult Success(Guid matchId) => new(true, matchId, null, null);
    public static CreateMatchResult Failure(string code, string message) => new(false, null, code, message);
}

public sealed record MatchPlayerView(
    Guid UserId,
    int SeatNumber,
    string Username,
    string DisplayName,
    int CardCount,
    bool HasFinished,
    int? FinishPosition,
    bool IsSelf);

public sealed record MatchStateView(
    Guid MatchId,
    Guid RoomId,
    long Version,
    string Status,
    Guid? CurrentPlayerUserId,
    bool IsOpeningPlay,
    string? CenterType,
    IReadOnlyList<string> Center,
    IReadOnlyList<string> Hand,
    IReadOnlyList<MatchPlayerView> Players,
    IReadOnlyList<Guid> WinnerOrder);

public sealed record MatchCommandResult(bool IsSuccess, string? ErrorCode, string? ErrorMessage, MatchStateView? State)
{
    public static MatchCommandResult Success(MatchStateView state) => new(true, null, null, state);
    public static MatchCommandResult Failure(string code, string message) => new(false, code, message, null);
}

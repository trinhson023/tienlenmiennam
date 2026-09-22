namespace SamLocService.Application.Matches;

public sealed record MatchPlayerSeed(Guid UserId, int SeatNumber, string Username, string DisplayName, bool IsBot = false);
public sealed record CreateMatchRequest(Guid RoomId, IReadOnlyList<MatchPlayerSeed> Players);

public sealed record CreateMatchResult(bool IsSuccess, Guid? MatchId, string? ErrorCode, string? ErrorMessage)
{
    public static CreateMatchResult Success(Guid matchId) => new(true, matchId, null, null);
    public static CreateMatchResult Failure(string code, string message) => new(false, null, code, message);
}

public sealed record MatchSummaryView(Guid MatchId, Guid RoomId, string Status);

public sealed record MatchPlayerView(
    Guid UserId,
    int SeatNumber,
    string Username,
    string DisplayName,
    int CardCount,
    bool HasPlayedAny,
    bool IsSelf,
    bool IsBot);

public sealed record MatchStateView(
    Guid MatchId,
    Guid RoomId,
    long Version,
    string Status,
    Guid? CurrentPlayerUserId,
    bool CurrentPlayerIsBot,
    DateTimeOffset? DeclarationDeadlineUtc,
    DateTimeOffset? TurnDeadlineUtc,
    string DeclarationState,
    Guid? SamDeclarerUserId,
    string? CenterType,
    IReadOnlyList<string> Center,
    IReadOnlyList<string> Hand,
    IReadOnlyList<MatchPlayerView> Players,
    Guid? WinnerUserId);

public sealed record MatchCommandResult(bool IsSuccess, string? ErrorCode, string? ErrorMessage, MatchStateView? State)
{
    public static MatchCommandResult Success(MatchStateView state) => new(true, null, null, state);
    public static MatchCommandResult Failure(string code, string message) => new(false, code, message, null);
}

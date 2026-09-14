namespace LobbyService.Application.Abstractions;

public sealed record MatchLaunchPlayer(Guid UserId, int SeatNumber, string Username, string DisplayName, bool IsBot);
public sealed record MatchLaunchResult(bool IsSuccess, Guid? MatchId, string? ErrorCode, string? ErrorMessage)
{
    public static MatchLaunchResult Success(Guid matchId) => new(true, matchId, null, null);
    public static MatchLaunchResult Failure(string code, string message) => new(false, null, code, message);
}

public sealed record MatchSummaryResult(bool IsSuccess, Guid? MatchId, Guid? RoomId, string? Status, string? ErrorCode, string? ErrorMessage)
{
    public static MatchSummaryResult Success(Guid matchId, Guid roomId, string status) => new(true, matchId, roomId, status, null, null);
    public static MatchSummaryResult Failure(string code, string message) => new(false, null, null, null, code, message);
}

public interface IMatchLauncher
{
    Task<MatchLaunchResult> StartAsync(string gameSlug, Guid roomId, IReadOnlyCollection<MatchLaunchPlayer> players, CancellationToken cancellationToken);
    Task<MatchLaunchResult> RematchAsync(string gameSlug, Guid previousMatchId, Guid roomId, IReadOnlyCollection<MatchLaunchPlayer> players, CancellationToken cancellationToken);
    Task<MatchSummaryResult> GetSummaryAsync(string gameSlug, Guid matchId, CancellationToken cancellationToken);
}

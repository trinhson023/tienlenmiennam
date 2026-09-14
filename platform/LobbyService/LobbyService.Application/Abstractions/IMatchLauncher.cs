namespace LobbyService.Application.Abstractions;

public sealed record MatchLaunchPlayer(Guid UserId, int SeatNumber, string Username, string DisplayName, bool IsBot);
public sealed record MatchLaunchResult(bool IsSuccess, Guid? MatchId, string? ErrorCode, string? ErrorMessage)
{
    public static MatchLaunchResult Success(Guid matchId) => new(true, matchId, null, null);
    public static MatchLaunchResult Failure(string code, string message) => new(false, null, code, message);
}

public interface IMatchLauncher
{
    Task<MatchLaunchResult> StartAsync(string gameSlug, Guid roomId, IReadOnlyCollection<MatchLaunchPlayer> players, CancellationToken cancellationToken);
}

namespace SamLocService.Application.Matches;

public interface IMatchStore
{
    Task<bool> TryAddAsync(MatchRuntime runtime, CancellationToken cancellationToken);
    Task<MatchRuntime?> GetAsync(Guid matchId, CancellationToken cancellationToken);
    Task<MatchRuntime?> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetActiveMatchIdsAsync(CancellationToken cancellationToken);
    Task SaveAsync(MatchRuntime runtime, CancellationToken cancellationToken);
}

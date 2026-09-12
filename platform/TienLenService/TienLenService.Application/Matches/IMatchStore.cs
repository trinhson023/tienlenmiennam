namespace TienLenService.Application.Matches;

public interface IMatchStore
{
    bool TryAdd(MatchRuntime runtime);
    MatchRuntime? Get(Guid matchId);
    MatchRuntime? GetLatestByRoom(Guid roomId);
}

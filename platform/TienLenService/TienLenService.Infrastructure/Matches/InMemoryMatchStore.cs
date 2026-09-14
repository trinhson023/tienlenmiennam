using System.Collections.Concurrent;
using TienLenService.Application.Matches;
using TienLenService.Domain.Matches;

namespace TienLenService.Infrastructure.Matches;

public sealed class InMemoryMatchStore : IMatchStore
{
    private readonly ConcurrentDictionary<Guid, MatchRuntime> _matches = new();
    private readonly ConcurrentDictionary<Guid, Guid> _latestByRoom = new();

    public Task<bool> TryAddAsync(MatchRuntime runtime, CancellationToken cancellationToken)
    {
        var matchId = runtime.Match.Id.Value;
        if (!_matches.TryAdd(matchId, runtime)) return Task.FromResult(false);
        _latestByRoom[runtime.RoomId] = matchId;
        return Task.FromResult(true);
    }

    public Task<MatchRuntime?> GetAsync(Guid matchId, CancellationToken cancellationToken) =>
        Task.FromResult(_matches.TryGetValue(matchId, out var runtime) ? runtime : null);

    public Task<MatchRuntime?> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken) =>
        Task.FromResult(_latestByRoom.TryGetValue(roomId, out var matchId) && _matches.TryGetValue(matchId, out var runtime) ? runtime : null);

    public Task<IReadOnlyList<Guid>> GetActiveMatchIdsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> ids = _matches.Values
            .Where(x => x.Match.Status == MatchStatus.InProgress)
            .Select(x => x.Match.Id.Value)
            .ToArray();
        return Task.FromResult(ids);
    }

    public Task SaveAsync(MatchRuntime runtime, CancellationToken cancellationToken)
    {
        _matches[runtime.Match.Id.Value] = runtime;
        return Task.CompletedTask;
    }
}

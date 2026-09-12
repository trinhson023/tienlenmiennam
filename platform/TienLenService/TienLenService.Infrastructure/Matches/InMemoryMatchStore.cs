using System.Collections.Concurrent;
using TienLenService.Application.Matches;

namespace TienLenService.Infrastructure.Matches;

public sealed class InMemoryMatchStore : IMatchStore
{
    private readonly ConcurrentDictionary<Guid, MatchRuntime> _matches = new();
    private readonly ConcurrentDictionary<Guid, Guid> _latestByRoom = new();

    public bool TryAdd(MatchRuntime runtime)
    {
        var matchId = runtime.Match.Id.Value;
        if (!_matches.TryAdd(matchId, runtime)) return false;
        _latestByRoom[runtime.RoomId] = matchId;
        return true;
    }

    public MatchRuntime? Get(Guid matchId) => _matches.TryGetValue(matchId, out var runtime) ? runtime : null;

    public MatchRuntime? GetLatestByRoom(Guid roomId)
    {
        return _latestByRoom.TryGetValue(roomId, out var matchId) ? Get(matchId) : null;
    }
}

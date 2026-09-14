using System.Collections.Concurrent;

namespace LobbyService.Application.Lobby;

public sealed class RoomLifecycleLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<T> ExecuteAsync<T>(Guid roomId, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var gate = _locks.GetOrAdd(roomId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }
}

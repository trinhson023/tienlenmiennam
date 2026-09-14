using System.Collections.Concurrent;

namespace MediaService.Application;

public sealed class MediaRoomMutationLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    public async Task<T> ExecuteAsync<T>(Guid roomId, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var gate = _gates.GetOrAdd(roomId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try { return await action(); }
        finally { gate.Release(); }
    }
}

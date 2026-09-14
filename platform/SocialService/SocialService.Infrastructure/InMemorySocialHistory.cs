using System.Collections.Concurrent;
using SocialService.Application;

namespace SocialService.Infrastructure;

public sealed class InMemorySocialHistory : IRecentSocialHistory
{
    private const int CapacityPerRoom = 50;
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<SocialChatEvent>> _rooms = new();

    public Task AddAsync(SocialChatEvent message, CancellationToken cancellationToken)
    {
        var queue = _rooms.GetOrAdd(message.RoomId, _ => new ConcurrentQueue<SocialChatEvent>());
        queue.Enqueue(message);
        while (queue.Count > CapacityPerRoom) queue.TryDequeue(out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SocialChatEvent>> GetRecentAsync(Guid roomId, int limit, CancellationToken cancellationToken)
    {
        if (!_rooms.TryGetValue(roomId, out var queue)) return Task.FromResult<IReadOnlyList<SocialChatEvent>>([]);
        var items = queue.ToArray();
        return Task.FromResult<IReadOnlyList<SocialChatEvent>>(items.Skip(Math.Max(0, items.Length - limit)).ToArray());
    }
}

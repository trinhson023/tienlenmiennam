using Microsoft.EntityFrameworkCore;
using SocialService.Application;

namespace SocialService.Infrastructure.Persistence;

public sealed class PersistentSocialHistory(IDbContextFactory<SocialDbContext> factory) : IRecentSocialHistory
{
    private const int CapacityPerRoom = 50;

    public async Task AddAsync(SocialChatEvent message, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Messages.Add(new SocialMessageRecord
        {
            EventId = message.EventId,
            RoomId = message.RoomId,
            SenderUserId = message.SenderUserId,
            SenderDisplayName = message.SenderDisplayName,
            Text = message.Text,
            Kind = message.Kind,
            SentAtUtc = message.SentAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);

        var staleIds = await db.Messages.AsNoTracking()
            .Where(x => x.RoomId == message.RoomId)
            .OrderByDescending(x => x.SentAtUtc)
            .ThenByDescending(x => x.EventId)
            .Skip(CapacityPerRoom)
            .Select(x => x.EventId)
            .ToListAsync(cancellationToken);

        if (staleIds.Count > 0)
            await db.Messages.Where(x => staleIds.Contains(x.EventId)).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SocialChatEvent>> GetRecentAsync(Guid roomId, int limit, CancellationToken cancellationToken)
    {
        var bounded = Math.Clamp(limit, 1, CapacityPerRoom);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Messages.AsNoTracking()
            .Where(x => x.RoomId == roomId)
            .OrderByDescending(x => x.SentAtUtc)
            .ThenByDescending(x => x.EventId)
            .Take(bounded)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.SentAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => new SocialChatEvent(x.EventId, x.RoomId, x.SenderUserId, x.SenderDisplayName, x.Text, x.Kind, x.SentAtUtc))
            .ToArray();
    }
}

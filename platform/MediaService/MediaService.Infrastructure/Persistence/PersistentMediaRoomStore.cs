using System.Text.Json;
using MediaService.Application;
using MediaService.Domain;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence;

public sealed class PersistentMediaRoomStore(IDbContextFactory<MediaDbContext> factory) : IMediaRoomStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<MediaRoomState> GetAsync(Guid roomId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.Rooms.AsNoTracking().SingleOrDefaultAsync(x => x.RoomId == roomId, ct);
        if (row is null) return new MediaRoomState(roomId, null, Array.Empty<MediaTrack>(), false, 0, null, 0);
        return new MediaRoomState(roomId,
            string.IsNullOrWhiteSpace(row.CurrentJson) ? null : JsonSerializer.Deserialize<MediaTrack>(row.CurrentJson, Json),
            JsonSerializer.Deserialize<MediaTrack[]>(row.QueueJson, Json) ?? Array.Empty<MediaTrack>(),
            row.Playing, row.PositionSeconds, row.StartedAtUtc, row.Revision);
    }

    public async Task SaveAsync(MediaRoomState state, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.Rooms.SingleOrDefaultAsync(x => x.RoomId == state.RoomId, ct);
        if (row is null) { row = new MediaRoomRecord { RoomId = state.RoomId }; db.Rooms.Add(row); }
        row.CurrentJson = state.Current is null ? null : JsonSerializer.Serialize(state.Current, Json);
        row.QueueJson = JsonSerializer.Serialize(state.Queue, Json); row.Playing = state.Playing; row.PositionSeconds = state.PositionSeconds;
        row.StartedAtUtc = state.StartedAtUtc; row.Revision = state.Revision; row.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

using LobbyService.Application.Abstractions;
using LobbyService.Domain.Games;
using LobbyService.Domain.Rooms;
using LobbyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LobbyService.Infrastructure.Repositories;

public sealed class LobbyRepository(LobbyDbContext db) : ILobbyRepository
{
    public async Task<IReadOnlyList<GameDefinition>> ListGamesAsync(CancellationToken ct) =>
        await db.Games.AsNoTracking().OrderBy(x => x.Type).ToListAsync(ct);

    public Task<GameDefinition?> GetGameBySlugAsync(string slug, CancellationToken ct) =>
        db.Games.SingleOrDefaultAsync(x => x.Slug == slug, ct);

    public async Task<IReadOnlyList<Room>> ListRoomsAsync(string? gameSlug, CancellationToken ct)
    {
        var query = db.Rooms.AsNoTracking().Include(x => x.GameDefinition).Include("_members")
            .Where(x => x.Status != RoomStatus.Closed);
        if (!string.IsNullOrWhiteSpace(gameSlug)) query = query.Where(x => x.GameDefinition.Slug == gameSlug);
        return await query.OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(ct);
    }

    public Task<Room?> GetRoomAsync(Guid roomId, CancellationToken ct) =>
        db.Rooms.Include(x => x.GameDefinition).Include("_members").SingleOrDefaultAsync(x => x.Id == roomId, ct);

    public Task<Room?> GetRoomForUserAsync(Guid userId, CancellationToken ct) =>
        db.Rooms.Include(x => x.GameDefinition).Include("_members")
            .SingleOrDefaultAsync(x => x.Status != RoomStatus.Closed && db.RoomMembers.Any(m => m.RoomId == x.Id && m.UserId == userId), ct);

    public Task AddRoomAsync(Room room, CancellationToken ct) => db.Rooms.AddAsync(room, ct).AsTask();
    public void RemoveRoom(Room room) => db.Rooms.Remove(room);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

using LobbyService.Domain.Games;
using LobbyService.Domain.Rooms;

namespace LobbyService.Application.Abstractions;

public interface ILobbyRepository
{
    Task<IReadOnlyList<GameDefinition>> ListGamesAsync(CancellationToken cancellationToken);
    Task<GameDefinition?> GetGameBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<Room>> ListRoomsAsync(string? gameSlug, CancellationToken cancellationToken);
    Task<Room?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken);
    Task<Room?> GetRoomForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task AddRoomAsync(Room room, CancellationToken cancellationToken);
    void RemoveRoom(Room room);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

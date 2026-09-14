using MediaService.Domain;

namespace MediaService.Application;

public interface IRoomMediaAccessGateway
{
    Task<RoomMediaContext?> GetRoomAsync(Guid roomId, CancellationToken ct);
}

public interface IMediaRoomStore
{
    Task<MediaRoomState> GetAsync(Guid roomId, CancellationToken ct);
    Task SaveAsync(MediaRoomState state, CancellationToken ct);
}

public interface IYouTubeSearch
{
    Task<IReadOnlyList<MediaSearchResult>> SearchAsync(string query, int maxResults, bool shortOnly, CancellationToken ct);
}

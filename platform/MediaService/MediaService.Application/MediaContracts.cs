using MediaService.Domain;

namespace MediaService.Application;

public sealed record RoomMediaMember(Guid UserId, string DisplayName, bool IsBot);
public sealed record RoomMediaContext(Guid RoomId, Guid HostUserId, string Status, IReadOnlyList<RoomMediaMember> Members);
public sealed record MediaRoomState(Guid RoomId, MediaTrack? Current, IReadOnlyList<MediaTrack> Queue, bool Playing, double PositionSeconds, DateTimeOffset? StartedAtUtc, long Revision);
public sealed record MediaSearchResult(string VideoId, string Title, string ChannelTitle, string Thumbnail);
public sealed record MediaResult<T>(bool IsSuccess, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static MediaResult<T> Success(T value) => new(true, value, null, null);
    public static MediaResult<T> Failure(string code, string message) => new(false, default, code, message);
}

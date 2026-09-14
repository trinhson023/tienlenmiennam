using MediaService.Domain;

namespace MediaService.Application;

public sealed class MediaApplicationService(
    IRoomMediaAccessGateway roomAccess,
    IMediaRoomStore store,
    IYouTubeSearch youtube,
    MediaRoomMutationLock mutationLock)
{
    public async Task<MediaResult<MediaRoomState>> GetStateAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var access = await RequireMemberAsync(roomId, userId, ct);
        return access.IsSuccess
            ? MediaResult<MediaRoomState>.Success(await store.GetAsync(roomId, ct))
            : MediaResult<MediaRoomState>.Failure(access.ErrorCode!, access.ErrorMessage!);
    }

    public async Task<MediaResult<IReadOnlyList<Guid>>> GetAudienceAsync(Guid roomId, CancellationToken ct)
    {
        var room = await roomAccess.GetRoomAsync(roomId, ct);
        if (room is null) return MediaResult<IReadOnlyList<Guid>>.Failure("room_not_found", "Không tìm thấy phòng.");
        return MediaResult<IReadOnlyList<Guid>>.Success(room.Members.Where(x => !x.IsBot).Select(x => x.UserId).Distinct().ToArray());
    }

    public Task<MediaResult<MediaRoomState>> SelectAsync(Guid roomId, Guid userId, MediaTrackInput input, CancellationToken ct) =>
        mutationLock.ExecuteAsync(roomId, async () =>
        {
            var host = await RequireHostAsync(roomId, userId, ct);
            if (!host.IsSuccess) return Fail(host);
            var track = MediaTrack.Create(input.VideoId, input.Title, input.ChannelTitle, input.Thumbnail);
            if (track is null) return MediaResult<MediaRoomState>.Failure("invalid_track", "Video YouTube không hợp lệ.");
            var state = await store.GetAsync(roomId, ct);
            var next = state with
            {
                Current = track,
                Playing = true,
                PositionSeconds = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                Revision = state.Revision + 1
            };
            await store.SaveAsync(next, ct);
            return MediaResult<MediaRoomState>.Success(next);
        }, ct);

    public Task<MediaResult<MediaRoomState>> QueueAsync(Guid roomId, Guid userId, MediaTrackInput input, CancellationToken ct) =>
        mutationLock.ExecuteAsync(roomId, async () =>
        {
            var host = await RequireHostAsync(roomId, userId, ct);
            if (!host.IsSuccess) return Fail(host);
            var track = MediaTrack.Create(input.VideoId, input.Title, input.ChannelTitle, input.Thumbnail);
            if (track is null) return MediaResult<MediaRoomState>.Failure("invalid_track", "Video YouTube không hợp lệ.");
            var state = await store.GetAsync(roomId, ct);
            var queue = state.Queue.Concat([track]).Take(20).ToArray();
            var next = state with { Queue = queue, Revision = state.Revision + 1 };
            await store.SaveAsync(next, ct);
            return MediaResult<MediaRoomState>.Success(next);
        }, ct);

    public Task<MediaResult<MediaRoomState>> ToggleAsync(Guid roomId, Guid userId, bool playing, double position, CancellationToken ct) =>
        mutationLock.ExecuteAsync(roomId, async () =>
        {
            var host = await RequireHostAsync(roomId, userId, ct);
            if (!host.IsSuccess) return Fail(host);
            var state = await store.GetAsync(roomId, ct);
            if (state.Current is null) return MediaResult<MediaRoomState>.Failure("no_track", "Chưa có bài nhạc nào.");
            var pos = NormalizePosition(position);
            var next = state with
            {
                Playing = playing,
                PositionSeconds = pos,
                StartedAtUtc = playing ? DateTimeOffset.UtcNow.AddSeconds(-pos) : null,
                Revision = state.Revision + 1
            };
            await store.SaveAsync(next, ct);
            return MediaResult<MediaRoomState>.Success(next);
        }, ct);

    public Task<MediaResult<MediaRoomState>> SeekAsync(Guid roomId, Guid userId, double position, CancellationToken ct) =>
        mutationLock.ExecuteAsync(roomId, async () =>
        {
            var host = await RequireHostAsync(roomId, userId, ct);
            if (!host.IsSuccess) return Fail(host);
            var state = await store.GetAsync(roomId, ct);
            if (state.Current is null) return MediaResult<MediaRoomState>.Failure("no_track", "Chưa có bài nhạc nào.");
            var pos = NormalizePosition(position);
            var next = state with
            {
                PositionSeconds = pos,
                StartedAtUtc = state.Playing ? DateTimeOffset.UtcNow.AddSeconds(-pos) : null,
                Revision = state.Revision + 1
            };
            await store.SaveAsync(next, ct);
            return MediaResult<MediaRoomState>.Success(next);
        }, ct);

    public Task<MediaResult<MediaRoomState>> NextAsync(Guid roomId, Guid userId, CancellationToken ct) =>
        mutationLock.ExecuteAsync(roomId, async () =>
        {
            var host = await RequireHostAsync(roomId, userId, ct);
            if (!host.IsSuccess) return Fail(host);
            var state = await store.GetAsync(roomId, ct);
            if (state.Queue.Count == 0)
            {
                var stopped = state with { Playing = false, PositionSeconds = 0, StartedAtUtc = null, Revision = state.Revision + 1 };
                await store.SaveAsync(stopped, ct);
                return MediaResult<MediaRoomState>.Success(stopped);
            }

            var current = state.Queue[0];
            var queue = state.Queue.Skip(1).ToArray();
            var next = state with
            {
                Current = current,
                Queue = queue,
                Playing = true,
                PositionSeconds = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                Revision = state.Revision + 1
            };
            await store.SaveAsync(next, ct);
            return MediaResult<MediaRoomState>.Success(next);
        }, ct);

    public Task<MediaResult<MediaRoomState>> ClearQueueAsync(Guid roomId, Guid userId, CancellationToken ct) =>
        mutationLock.ExecuteAsync(roomId, async () =>
        {
            var host = await RequireHostAsync(roomId, userId, ct);
            if (!host.IsSuccess) return Fail(host);
            var state = await store.GetAsync(roomId, ct);
            var next = state with { Queue = Array.Empty<MediaTrack>(), Revision = state.Revision + 1 };
            await store.SaveAsync(next, ct);
            return MediaResult<MediaRoomState>.Success(next);
        }, ct);

    public async Task<MediaResult<IReadOnlyList<MediaSearchResult>>> SearchAsync(string? query, bool shortOnly, CancellationToken ct)
    {
        var clean = (query ?? string.Empty).Trim();
        if (clean.Length == 0) return MediaResult<IReadOnlyList<MediaSearchResult>>.Failure("missing_query", shortOnly ? "Thiếu chủ đề Shorts." : "Thiếu từ khóa tìm kiếm.");
        if (clean.Length > 100) return MediaResult<IReadOnlyList<MediaSearchResult>>.Failure("query_too_long", "Từ khóa tìm kiếm quá dài.");
        try
        {
            return MediaResult<IReadOnlyList<MediaSearchResult>>.Success(await youtube.SearchAsync(clean, shortOnly ? 20 : 8, shortOnly, ct));
        }
        catch (InvalidOperationException ex)
        {
            return MediaResult<IReadOnlyList<MediaSearchResult>>.Failure("youtube_not_configured", ex.Message);
        }
        catch
        {
            return MediaResult<IReadOnlyList<MediaSearchResult>>.Failure("youtube_unavailable", "YouTube hiện không phản hồi. Hãy thử lại sau.");
        }
    }

    private async Task<MediaResult<RoomMediaContext>> RequireMemberAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var room = await roomAccess.GetRoomAsync(roomId, ct);
        if (room is null) return MediaResult<RoomMediaContext>.Failure("room_not_found", "Không tìm thấy phòng.");
        return room.Members.Any(x => !x.IsBot && x.UserId == userId)
            ? MediaResult<RoomMediaContext>.Success(room)
            : MediaResult<RoomMediaContext>.Failure("not_in_room", "Bạn không thuộc phòng này.");
    }

    private async Task<MediaResult<RoomMediaContext>> RequireHostAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var member = await RequireMemberAsync(roomId, userId, ct);
        if (!member.IsSuccess) return member;
        return member.Value!.HostUserId == userId
            ? member
            : MediaResult<RoomMediaContext>.Failure("host_only", "Chỉ chủ bàn mới điều khiển Music Room.");
    }

    private static double NormalizePosition(double position) => double.IsFinite(position) ? Math.Max(0, position) : 0;
    private static MediaResult<MediaRoomState> Fail(MediaResult<RoomMediaContext> result) => MediaResult<MediaRoomState>.Failure(result.ErrorCode!, result.ErrorMessage!);
}

public sealed record MediaTrackInput(string VideoId, string? Title, string? ChannelTitle, string? Thumbnail);

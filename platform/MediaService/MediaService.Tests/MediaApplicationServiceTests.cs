using MediaService.Application;
using MediaService.Domain;

namespace MediaService.Tests;

public sealed class MediaApplicationServiceTests
{
    private static readonly Guid RoomId = Guid.NewGuid();
    private static readonly Guid Host = Guid.NewGuid();
    private static readonly Guid Guest = Guid.NewGuid();

    [Fact]
    public async Task Guest_CanRead_ButCannotControl()
    {
        var service = Create();
        Assert.True((await service.GetStateAsync(RoomId, Guest, CancellationToken.None)).IsSuccess);
        Assert.Equal("host_only", (await service.SelectAsync(RoomId, Guest, new("dQw4w9WgXcQ", "x", null, null), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Host_SelectsAndQueues()
    {
        var service = Create();
        var selected = await service.SelectAsync(RoomId, Host, new("dQw4w9WgXcQ", "Track", null, null), CancellationToken.None);
        Assert.True(selected.IsSuccess);
        Assert.True(selected.Value!.Playing);
        var queued = await service.QueueAsync(RoomId, Host, new("M7lc1UVf-VE", "Next", null, null), CancellationToken.None);
        Assert.Single(queued.Value!.Queue);
    }

    [Fact]
    public async Task Outsider_IsRejected()
    {
        Assert.Equal("not_in_room", (await Create().GetStateAsync(RoomId, Guid.NewGuid(), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task ConcurrentHostQueueMutations_AreSerialized()
    {
        var store = new Store(saveDelayMs: 25);
        var service = Create(store);
        await service.SelectAsync(RoomId, Host, new("dQw4w9WgXcQ", "Current", null, null), CancellationToken.None);

        var first = service.QueueAsync(RoomId, Host, new("M7lc1UVf-VE", "One", null, null), CancellationToken.None);
        var second = service.QueueAsync(RoomId, Host, new("aqz-KE-bpKQ", "Two", null, null), CancellationToken.None);
        await Task.WhenAll(first, second);

        var state = (await service.GetStateAsync(RoomId, Host, CancellationToken.None)).Value!;
        Assert.Equal(2, state.Queue.Count);
        Assert.Equal(3, state.Revision);
    }

    private static MediaApplicationService Create(Store? store = null) =>
        new(new Rooms(), store ?? new Store(), new Search(), new MediaRoomMutationLock());

    private sealed class Rooms : IRoomMediaAccessGateway
    {
        public Task<RoomMediaContext?> GetRoomAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<RoomMediaContext?>(id == RoomId
                ? new(RoomId, Host, "Open", [new(Host, "Host", false), new(Guest, "Guest", false)])
                : null);
    }

    private sealed class Store(int saveDelayMs = 0) : IMediaRoomStore
    {
        private MediaRoomState _state = new(RoomId, null, [], false, 0, null, 0);
        public Task<MediaRoomState> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(_state);
        public async Task SaveAsync(MediaRoomState state, CancellationToken ct)
        {
            if (saveDelayMs > 0) await Task.Delay(saveDelayMs, ct);
            _state = state;
        }
    }

    private sealed class Search : IYouTubeSearch
    {
        public Task<IReadOnlyList<MediaSearchResult>> SearchAsync(string q, int max, bool shorts, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MediaSearchResult>>([]);
    }
}

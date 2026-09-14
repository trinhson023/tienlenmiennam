using System.Security.Claims;
using MediaService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MediaService.Api.Hubs;

[Authorize]
public sealed class MediaHub(MediaApplicationService media) : Hub
{
    public async Task<MediaRoomState> JoinRoom(Guid roomId)
    {
        var userId = GetUserId(); var result = await media.GetStateAsync(roomId, userId, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không vào được Media Room.");
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(roomId, userId), Context.ConnectionAborted); return result.Value;
    }
    public Task LeaveRoom(Guid roomId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(roomId, GetUserId()), Context.ConnectionAborted);
    public Task Select(Guid roomId, MediaTrackInput input) => Mutate(roomId, ct => media.SelectAsync(roomId, GetUserId(), input, ct));
    public Task Queue(Guid roomId, MediaTrackInput input) => Mutate(roomId, ct => media.QueueAsync(roomId, GetUserId(), input, ct));
    public Task Toggle(Guid roomId, bool playing, double position) => Mutate(roomId, ct => media.ToggleAsync(roomId, GetUserId(), playing, position, ct));
    public Task Seek(Guid roomId, double position) => Mutate(roomId, ct => media.SeekAsync(roomId, GetUserId(), position, ct));
    public Task Next(Guid roomId) => Mutate(roomId, ct => media.NextAsync(roomId, GetUserId(), ct));
    public Task ClearQueue(Guid roomId) => Mutate(roomId, ct => media.ClearQueueAsync(roomId, GetUserId(), ct));

    private async Task Mutate(Guid roomId, Func<CancellationToken, Task<MediaResult<MediaRoomState>>> action)
    {
        var result = await action(Context.ConnectionAborted); if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không cập nhật được Media Room.");
        var audience = await media.GetAudienceAsync(roomId, Context.ConnectionAborted); if (!audience.IsSuccess || audience.Value is null) throw new HubException("Không xác minh được thành viên phòng.");
        foreach (var userId in audience.Value) await Clients.Group(UserGroup(roomId, userId)).SendAsync("MediaStateUpdated", result.Value, Context.ConnectionAborted);
    }
    private Guid GetUserId() { var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier); if (!Guid.TryParse(raw, out var id)) throw new HubException("Unauthorized user."); return id; }
    private static string UserGroup(Guid roomId, Guid userId) => $"media:room:{roomId:N}:user:{userId:N}";
}

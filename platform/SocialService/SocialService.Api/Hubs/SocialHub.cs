using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialService.Application;

namespace SocialService.Api.Hubs;

[Authorize]
public sealed class SocialHub(SocialApplicationService social, SocialThrottle throttle) : Hub
{
    public async Task<IReadOnlyList<SocialChatEvent>> JoinRoom(Guid roomId)
    {
        var result = await social.GetRecentAsync(roomId, GetUserId(), 30, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không vào được kênh xã hội của phòng.");
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomId), Context.ConnectionAborted);
        return result.Value;
    }

    public Task LeaveRoom(Guid roomId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(roomId), Context.ConnectionAborted);

    public async Task SendQuickChat(Guid roomId, string text)
    {
        var userId = GetUserId(); EnforceThrottle(userId);
        var result = await social.SendQuickChatAsync(roomId, userId, text, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không gửi được lời gáy.");
        await Clients.Group(RoomGroup(roomId)).SendAsync("QuickChatReceived", result.Value, Context.ConnectionAborted);
    }

    public async Task SendRoomChat(Guid roomId, string text)
    {
        var userId = GetUserId(); EnforceThrottle(userId);
        var result = await social.SendRoomChatAsync(roomId, userId, text, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không gửi được tin nhắn.");
        await Clients.Group(RoomGroup(roomId)).SendAsync("RoomChatReceived", result.Value, Context.ConnectionAborted);
    }

    public async Task ThrowReaction(Guid roomId, string reactionType, Guid targetUserId)
    {
        var userId = GetUserId(); EnforceThrottle(userId);
        var result = await social.CreateReactionAsync(roomId, userId, reactionType, targetUserId, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không ném được vật phẩm.");
        await Clients.Group(RoomGroup(roomId)).SendAsync("ThrowReactionReceived", result.Value, Context.ConnectionAborted);
    }

    private void EnforceThrottle(Guid userId)
    {
        if (!throttle.TryAcquire(userId)) throw new HubException("Thao tác quá nhanh, thử lại sau một chút.");
    }

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId)) throw new HubException("Unauthorized user.");
        return userId;
    }

    private static string RoomGroup(Guid roomId) => $"social:room:{roomId:N}";
}

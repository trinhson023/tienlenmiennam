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
        var userId = GetUserId();
        var result = await social.GetRecentAsync(roomId, userId, 30, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không vào được kênh xã hội của phòng.");
        await Groups.AddToGroupAsync(Context.ConnectionId, UserRoomGroup(roomId, userId), Context.ConnectionAborted);
        return result.Value;
    }

    public Task LeaveRoom(Guid roomId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, UserRoomGroup(roomId, GetUserId()), Context.ConnectionAborted);

    public async Task SendQuickChat(Guid roomId, string text)
    {
        var userId = GetUserId(); EnforceThrottle(userId);
        var result = await social.SendQuickChatAsync(roomId, userId, text, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không gửi được lời gáy.");
        await BroadcastToCurrentMembersAsync(roomId, "QuickChatReceived", result.Value);
    }

    public async Task SendRoomChat(Guid roomId, string text)
    {
        var userId = GetUserId(); EnforceThrottle(userId);
        var result = await social.SendRoomChatAsync(roomId, userId, text, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không gửi được tin nhắn.");
        await BroadcastToCurrentMembersAsync(roomId, "RoomChatReceived", result.Value);
    }

    public async Task ThrowReaction(Guid roomId, string reactionType, Guid targetUserId)
    {
        var userId = GetUserId(); EnforceThrottle(userId);
        var result = await social.CreateReactionAsync(roomId, userId, reactionType, targetUserId, Context.ConnectionAborted);
        if (!result.IsSuccess || result.Value is null) throw new HubException(result.ErrorMessage ?? "Không ném được vật phẩm.");
        await BroadcastToCurrentMembersAsync(roomId, "ThrowReactionReceived", result.Value);
    }

    private async Task BroadcastToCurrentMembersAsync<T>(Guid roomId, string method, T payload)
    {
        var audience = await social.GetHumanAudienceAsync(roomId, Context.ConnectionAborted);
        if (!audience.IsSuccess || audience.Value is null) throw new HubException(audience.ErrorMessage ?? "Không xác minh được thành viên phòng.");
        foreach (var memberUserId in audience.Value)
            await Clients.Group(UserRoomGroup(roomId, memberUserId)).SendAsync(method, payload, Context.ConnectionAborted);
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

    private static string UserRoomGroup(Guid roomId, Guid userId) => $"social:room:{roomId:N}:user:{userId:N}";
}

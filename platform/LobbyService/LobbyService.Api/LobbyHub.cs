using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LobbyService.Api;

[Authorize]
public sealed class LobbyHub : Hub
{
    public Task SubscribeLobby() => Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
    public Task UnsubscribeLobby() => Groups.RemoveFromGroupAsync(Context.ConnectionId, "lobby");
    public Task SubscribeRoom(Guid roomId) => Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");
    public Task UnsubscribeRoom(Guid roomId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");
}

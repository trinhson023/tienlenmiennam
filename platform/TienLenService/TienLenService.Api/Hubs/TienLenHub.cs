using Microsoft.AspNetCore.SignalR;

namespace TienLenService.Api.Hubs;

public sealed class TienLenHub : Hub
{
    public Task JoinMatch(string matchId) => Groups.AddToGroupAsync(Context.ConnectionId, $"match:{matchId}");
    public Task LeaveMatch(string matchId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match:{matchId}");
}

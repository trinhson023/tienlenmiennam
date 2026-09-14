using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TienLenService.Application.Matches;

namespace TienLenService.Api.Hubs;

[Authorize]
public sealed class TienLenHub(TienLenMatchApplicationService matches, MatchBroadcaster broadcaster) : Hub
{
    public async Task JoinMatch(Guid matchId)
    {
        var userId = GetUserId(); var state = await matches.GetStateAsync(matchId, userId, Context.ConnectionAborted); if (!state.IsSuccess || state.State is null) throw new HubException(state.ErrorMessage ?? "Không vào được ván."); await Groups.AddToGroupAsync(Context.ConnectionId, MatchBroadcaster.UserGroup(matchId, userId), Context.ConnectionAborted); await Clients.Caller.SendAsync("MatchStateUpdated", state.State, Context.ConnectionAborted);
    }
    public Task LeaveMatch(Guid matchId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, MatchBroadcaster.UserGroup(matchId, GetUserId()), Context.ConnectionAborted);
    public async Task PlayCards(Guid matchId, string[] cardCodes, long expectedVersion) { var result = await matches.PlayCardsAsync(matchId, GetUserId(), cardCodes, expectedVersion, Context.ConnectionAborted); if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}"); await broadcaster.BroadcastMatchAsync(matchId, Context.ConnectionAborted); }
    public async Task Pass(Guid matchId, long expectedVersion) { var result = await matches.PassAsync(matchId, GetUserId(), expectedVersion, Context.ConnectionAborted); if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}"); await broadcaster.BroadcastMatchAsync(matchId, Context.ConnectionAborted); }
    private Guid GetUserId() { var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier); if (!Guid.TryParse(value, out var userId)) throw new HubException("Unauthorized user."); return userId; }
}

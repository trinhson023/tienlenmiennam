using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TienLenService.Application.Matches;

namespace TienLenService.Api.Hubs;

[Authorize]
public sealed class TienLenHub(TienLenMatchApplicationService matches) : Hub
{
    public async Task JoinMatch(Guid matchId)
    {
        var userId = GetUserId();
        var state = matches.GetState(matchId, userId);
        if (!state.IsSuccess || state.State is null) throw new HubException(state.ErrorMessage ?? "Không vào được ván.");
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(matchId, userId));
        await Clients.Caller.SendAsync("MatchStateUpdated", state.State);
    }

    public Task LeaveMatch(Guid matchId)
    {
        var userId = GetUserId();
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(matchId, userId));
    }

    public async Task PlayCards(Guid matchId, string[] cardCodes, long expectedVersion)
    {
        var result = matches.PlayCards(matchId, GetUserId(), cardCodes, expectedVersion);
        if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}");
        await BroadcastMatch(matchId);
    }

    public async Task Pass(Guid matchId, long expectedVersion)
    {
        var result = matches.Pass(matchId, GetUserId(), expectedVersion);
        if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}");
        await BroadcastMatch(matchId);
    }

    private async Task BroadcastMatch(Guid matchId)
    {
        foreach (var userId in matches.GetParticipantIds(matchId))
        {
            var state = matches.GetState(matchId, userId);
            if (state.IsSuccess && state.State is not null)
                await Clients.Group(UserGroup(matchId, userId)).SendAsync("MatchStateUpdated", state.State);
        }
    }

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId)) throw new HubException("Unauthorized user.");
        return userId;
    }

    private static string UserGroup(Guid matchId, Guid userId) => $"match:{matchId}:user:{userId}";
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TienLenService.Application.Matches;

namespace TienLenService.Api.Hubs;

[Authorize]
public sealed class TienLenHub(TienLenMatchApplicationService matches, MatchBroadcaster broadcaster) : Hub
{
    public async Task JoinMatch(Guid matchId, CancellationToken ct)
    {
        var userId = GetUserId();
        var state = await matches.GetStateAsync(matchId, userId, ct);
        if (!state.IsSuccess || state.State is null) throw new HubException(state.ErrorMessage ?? "Không vào được ván.");
        await Groups.AddToGroupAsync(Context.ConnectionId, MatchBroadcaster.UserGroup(matchId, userId), ct);
        await Clients.Caller.SendAsync("MatchStateUpdated", state.State, ct);
    }

    public Task LeaveMatch(Guid matchId, CancellationToken ct) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, MatchBroadcaster.UserGroup(matchId, GetUserId()), ct);

    public async Task PlayCards(Guid matchId, string[] cardCodes, long expectedVersion, CancellationToken ct)
    {
        var result = await matches.PlayCardsAsync(matchId, GetUserId(), cardCodes, expectedVersion, ct);
        if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}");
        await broadcaster.BroadcastMatchAsync(matchId, ct);
    }

    public async Task Pass(Guid matchId, long expectedVersion, CancellationToken ct)
    {
        var result = await matches.PassAsync(matchId, GetUserId(), expectedVersion, ct);
        if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}");
        await broadcaster.BroadcastMatchAsync(matchId, ct);
    }

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId)) throw new HubException("Unauthorized user.");
        return userId;
    }
}

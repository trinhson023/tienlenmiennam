using Microsoft.AspNetCore.SignalR;
using TienLenService.Api.Hubs;
using TienLenService.Application.Matches;

namespace TienLenService.Api;

public sealed class MatchBroadcaster(IHubContext<TienLenHub> hub, TienLenMatchApplicationService matches)
{
    public async Task BroadcastMatchAsync(Guid matchId, CancellationToken ct)
    {
        foreach (var userId in await matches.GetParticipantIdsAsync(matchId, ct))
        {
            var state = await matches.GetStateAsync(matchId, userId, ct);
            if (state.IsSuccess && state.State is not null)
                await hub.Clients.Group(UserGroup(matchId, userId)).SendAsync("MatchStateUpdated", state.State, ct);
        }
    }

    public static string UserGroup(Guid matchId, Guid userId) => $"match:{matchId}:user:{userId}";
}

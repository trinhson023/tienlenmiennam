using Microsoft.AspNetCore.SignalR;
using SamLocService.Api.Hubs;
using SamLocService.Application.Matches;

namespace SamLocService.Api;

public sealed class MatchBroadcaster(IHubContext<SamLocHub> hub, SamMatchApplicationService matches)
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

    public async Task BroadcastRematchAsync(Guid previousMatchId, Guid newMatchId, CancellationToken ct)
    {
        foreach (var userId in await matches.GetParticipantIdsAsync(previousMatchId, ct))
            await hub.Clients.Group(UserGroup(previousMatchId, userId)).SendAsync("RematchStarted", newMatchId, ct);
    }

    public static string UserGroup(Guid matchId, Guid userId) => $"sam-match:{matchId}:user:{userId}";
}

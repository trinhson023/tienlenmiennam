using System.Security.Claims;
using StatisticsService.Application;

namespace StatisticsService.Api;

public static class StatisticsEndpoints
{
    public static IEndpointRouteBuilder MapStatisticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").RequireAuthorization();
        group.MapGet("/me", async (string? game, ClaimsPrincipal principal, StatisticsApplicationService statistics, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
            return Results.Ok(await statistics.GetPlayerAsync(userId.Value, game ?? "tien-len", ct));
        });
        group.MapGet("/users/{userId:guid}", async (Guid userId, string? game, StatisticsApplicationService statistics, CancellationToken ct) =>
            Results.Ok(await statistics.GetPlayerAsync(userId, game ?? "tien-len", ct)));
        group.MapGet("/leaderboard", async (string? game, int? limit, StatisticsApplicationService statistics, CancellationToken ct) =>
            Results.Ok(await statistics.GetLeaderboardAsync(game ?? "tien-len", limit ?? 20, ct)));
        return endpoints;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

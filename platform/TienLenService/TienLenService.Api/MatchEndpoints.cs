using System.Security.Claims;
using TienLenService.Application.Matches;

namespace TienLenService.Api;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/internal/matches", async (HttpContext http, CreateMatchRequest request, TienLenMatchApplicationService matches, IConfiguration configuration, CancellationToken ct) => { if (!HasInternalKey(http, configuration)) return Results.Unauthorized(); var result = await matches.CreateMatchAsync(request, ct); return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result); });
        endpoints.MapGet("/api/internal/matches/{matchId:guid}/summary", async (Guid matchId, HttpContext http, TienLenMatchApplicationService matches, IConfiguration configuration, CancellationToken ct) => { if (!HasInternalKey(http, configuration)) return Results.Unauthorized(); var summary = await matches.GetSummaryAsync(matchId, ct); return summary is null ? Results.NotFound() : Results.Ok(summary); });
        endpoints.MapPost("/api/internal/matches/{matchId:guid}/rematch", async (Guid matchId, HttpContext http, CreateMatchRequest request, TienLenMatchApplicationService matches, MatchBroadcaster broadcaster, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!HasInternalKey(http, configuration)) return Results.Unauthorized();

            // A browser can miss RematchStarted while reconnecting/rebuilding containers. In that
            // case Lobby already points at the new in-progress match and a stale client can call
            // rematch again with that active match id. Treat that as an idempotent success instead
            // of returning rematch_not_ready, so the client receives ActiveMatchId and catches up.
            var current = await matches.GetSummaryAsync(matchId, ct);
            if (current is not null && string.Equals(current.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
                return Results.Ok(CreateMatchResult.Success(matchId));

            var result = await matches.CreateRematchAsync(matchId, request, ct);
            if (!result.IsSuccess)
                return result.ErrorCode is "rematch_not_ready" or "rematch_roster_changed" or "rematch_room_mismatch" ? Results.Conflict(result) : Results.BadRequest(result);
            await broadcaster.BroadcastRematchAsync(matchId, result.MatchId!.Value, ct);
            return Results.Ok(result);
        });
        endpoints.MapPost("/api/internal/matches/{matchId:guid}/players/{userId:guid}/abandon", async (Guid matchId, Guid userId, HttpContext http, TienLenMatchApplicationService matches, MatchBroadcaster broadcaster, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!HasInternalKey(http, configuration)) return Results.Unauthorized();
            var result = await matches.AbandonAsync(matchId, userId, ct);
            if (!result.IsSuccess) return result.ErrorCode == "match_not_found" ? Results.NotFound(result) : Results.Conflict(result);
            await broadcaster.BroadcastMatchAsync(matchId, ct);
            return Results.Ok(new { isSuccess = true });
        });
        var group = endpoints.MapGroup("/api/matches").RequireAuthorization();
        group.MapGet("/{matchId:guid}", async (Guid matchId, ClaimsPrincipal principal, TienLenMatchApplicationService matches, CancellationToken ct) => { var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); var result = await matches.GetStateAsync(matchId, userId.Value, ct); if (result.IsSuccess) return Results.Ok(result.State); return result.ErrorCode == "match_not_found" ? Results.NotFound(new { error = result.ErrorCode, message = result.ErrorMessage }) : Results.Forbid(); });
        return endpoints;
    }
    private static bool HasInternalKey(HttpContext http, IConfiguration configuration) { var expectedKey = configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me"; return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) && provided == expectedKey; }
    private static Guid? GetUserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}

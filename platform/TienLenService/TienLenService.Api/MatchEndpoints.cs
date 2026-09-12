using System.Security.Claims;
using TienLenService.Application.Matches;

namespace TienLenService.Api;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/internal/matches", (HttpContext http, CreateMatchRequest request, TienLenMatchApplicationService matches, IConfiguration configuration) =>
        {
            var expectedKey = configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me";
            if (!http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) || provided != expectedKey) return Results.Unauthorized();
            var result = matches.CreateMatch(request);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        });

        var group = endpoints.MapGroup("/api/matches").RequireAuthorization();
        group.MapGet("/{matchId:guid}", (Guid matchId, ClaimsPrincipal principal, TienLenMatchApplicationService matches) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var result = matches.GetState(matchId, userId.Value);
            if (result.IsSuccess) return Results.Ok(result.State);
            return result.ErrorCode == "match_not_found" ? Results.NotFound(new { error = result.ErrorCode, message = result.ErrorMessage }) : Results.Forbid();
        });
        return endpoints;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}

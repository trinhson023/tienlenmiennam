using System.Security.Claims;
using SocialService.Application;

namespace SocialService.Api;

public static class SocialEndpoints
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rooms/{roomId:guid}/messages", async (Guid roomId, int? limit, ClaimsPrincipal principal, SocialApplicationService social, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var result = await social.GetRecentAsync(roomId, userId.Value, limit ?? 30, ct);
            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.ErrorCode == "room_not_found"
                ? Results.NotFound(new { error = result.ErrorCode, message = result.ErrorMessage })
                : Results.Forbid();
        }).RequireAuthorization();
        return endpoints;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

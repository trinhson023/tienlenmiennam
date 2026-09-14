using System.Security.Claims;
using MediaService.Application;

namespace MediaService.Api;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").RequireAuthorization();
        group.MapGet("/search", async (Guid roomId, string? q, ClaimsPrincipal principal, MediaApplicationService media, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
            return Map(await media.SearchAsync(roomId, userId.Value, q, false, ct));
        });
        group.MapGet("/shorts", async (Guid roomId, string? q, ClaimsPrincipal principal, MediaApplicationService media, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
            return Map(await media.SearchAsync(roomId, userId.Value, q, true, ct));
        });
        group.MapGet("/rooms/{roomId:guid}", async (Guid roomId, ClaimsPrincipal principal, MediaApplicationService media, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); return Map(await media.GetStateAsync(roomId, userId.Value, ct));
        });
        return endpoints;
    }

    private static Guid? GetUserId(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static IResult Map<T>(MediaResult<T> result)
    {
        if (result.IsSuccess) return Results.Ok(result.Value);
        var status = result.ErrorCode switch { "not_in_room" or "host_only" => 403, "room_not_found" => 404, "youtube_unavailable" => 502, "youtube_not_configured" => 503, _ => 400 };
        return Results.Json(new { error = result.ErrorCode, message = result.ErrorMessage }, statusCode: status);
    }
}

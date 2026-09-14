using LobbyService.Application.Lobby;

namespace LobbyService.Api;

public static class MediaContextEndpoints
{
    public static IEndpointRouteBuilder MapMediaContextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/internal/rooms/{roomId:guid}/media-context", async (Guid roomId, HttpContext http, LobbyApplicationService service, IConfiguration configuration, CancellationToken ct) =>
        {
            var expected = configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me";
            if (!http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) || provided != expected)
                return Results.Unauthorized();

            var result = await service.GetRoomAsync(roomId, ct);
            if (!result.IsSuccess || result.Value is null)
                return Results.NotFound(new { error = "room_not_found", message = "Không tìm thấy phòng." });

            var room = result.Value;
            return Results.Ok(new
            {
                id = room.Id,
                hostUserId = room.HostUserId,
                status = room.Status,
                members = room.Members.Select(x => new { x.UserId, x.Username, x.DisplayName, x.IsBot }).ToArray()
            });
        });
        return endpoints;
    }
}

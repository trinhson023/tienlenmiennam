using System.Security.Claims;
using LobbyService.Application.Common;
using LobbyService.Application.Lobby;
using Microsoft.AspNetCore.SignalR;

namespace LobbyService.Api;

public static class LobbyEndpoints
{
    public static IEndpointRouteBuilder MapLobbyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").RequireAuthorization();

        group.MapGet("/games", async (LobbyApplicationService service, CancellationToken ct) => Results.Ok(await service.ListGamesAsync(ct)));
        group.MapGet("/rooms", async (string? game, LobbyApplicationService service, CancellationToken ct) => Results.Ok(await service.ListRoomsAsync(game, ct)));
        group.MapGet("/rooms/{roomId:guid}", async (Guid roomId, LobbyApplicationService service, CancellationToken ct) => Map(await service.GetRoomAsync(roomId, ct)));

        group.MapPost("/rooms", async (CreateRoomRequest request, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) =>
        {
            var identity = GetIdentity(principal);
            if (identity is null) return Results.Unauthorized();
            var result = await service.CreateRoomAsync(identity, request, ct);
            if (!result.IsSuccess) return Map(result);
            await hub.Clients.Group("lobby").SendAsync("RoomChanged", result.Value!.Id, ct);
            return Results.Created($"/api/rooms/{result.Value.Id}", result.Value);
        });

        group.MapPost("/rooms/{roomId:guid}/join", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) =>
        {
            var identity = GetIdentity(principal);
            if (identity is null) return Results.Unauthorized();
            var result = await service.JoinRoomAsync(roomId, identity, ct);
            if (!result.IsSuccess) return Map(result);
            await hub.Clients.Group("lobby").SendAsync("RoomChanged", roomId, ct);
            await hub.Clients.Group($"room:{roomId}").SendAsync("RoomUpdated", result.Value, ct);
            return Results.Ok(result.Value);
        });

        group.MapPost("/rooms/{roomId:guid}/leave", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();
            var result = await service.LeaveRoomAsync(roomId, userId.Value, ct);
            if (!result.IsSuccess) return Map(result);
            if (result.Value!.RoomRemoved)
            {
                await hub.Clients.Group("lobby").SendAsync("RoomRemoved", roomId, ct);
                await hub.Clients.Group($"room:{roomId}").SendAsync("RoomRemoved", roomId, ct);
            }
            else
            {
                await hub.Clients.Group("lobby").SendAsync("RoomChanged", roomId, ct);
                await hub.Clients.Group($"room:{roomId}").SendAsync("RoomUpdated", result.Value.Room, ct);
            }
            return Results.Ok(result.Value);
        });

        return endpoints;
    }

    private static PlayerIdentity? GetIdentity(ClaimsPrincipal principal)
    {
        var id = GetUserId(principal);
        var username = principal.FindFirstValue(ClaimTypes.Name);
        var displayName = principal.FindFirstValue("display_name") ?? username;
        return id.HasValue && !string.IsNullOrWhiteSpace(username)
            ? new PlayerIdentity(id.Value, username, displayName ?? username)
            : null;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static IResult Map<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess) return Results.Ok(result.Value);
        var status = result.ErrorCode switch
        {
            "room_not_found" or "game_not_found" => StatusCodes.Status404NotFound,
            "already_in_room" or "room_full" or "room_not_open" or "join_conflict" => StatusCodes.Status409Conflict,
            "game_not_enabled" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(new { error = result.ErrorCode, message = result.ErrorMessage }, statusCode: status);
    }
}

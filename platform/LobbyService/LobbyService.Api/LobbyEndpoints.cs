using System.Security.Claims;
using LobbyService.Application.Common;
using LobbyService.Application.Lobby;
using Microsoft.AspNetCore.SignalR;

namespace LobbyService.Api;

public static class LobbyEndpoints
{
    public static IEndpointRouteBuilder MapLobbyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/internal/rooms/{roomId:guid}/social-context", async (Guid roomId, HttpContext http, LobbyApplicationService service, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!HasInternalKey(http, configuration)) return Results.Unauthorized();
            var result = await service.GetRoomAsync(roomId, ct);
            if (!result.IsSuccess || result.Value is null) return Results.NotFound(new { error = "room_not_found", message = "Không tìm thấy phòng." });
            var room = result.Value;
            return Results.Ok(new { id = room.Id, status = room.Status, members = room.Members.Select(x => new { x.UserId, x.Username, x.DisplayName, x.IsBot }).ToArray() });
        });

        var group = endpoints.MapGroup("/api").RequireAuthorization();
        group.MapGet("/games", async (LobbyApplicationService service, CancellationToken ct) => Results.Ok(await service.ListGamesAsync(ct)));
        group.MapGet("/rooms", async (string? game, LobbyApplicationService service, CancellationToken ct) => Results.Ok(await service.ListRoomsAsync(game, ct)));
        group.MapGet("/rooms/me", async (ClaimsPrincipal principal, LobbyApplicationService service, CancellationToken ct) => { var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); var result = await service.GetCurrentRoomForUserAsync(userId.Value, ct); return result.IsSuccess ? Results.Ok(result.Value) : Results.Ok(null); });
        group.MapGet("/rooms/{roomId:guid}", async (Guid roomId, LobbyApplicationService service, CancellationToken ct) => Map(await service.GetRoomAsync(roomId, ct)));
        group.MapPost("/rooms", async (CreateRoomRequest request, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) => { var identity = GetIdentity(principal); if (identity is null) return Results.Unauthorized(); var result = await service.CreateRoomAsync(identity, request, ct); if (!result.IsSuccess) return Map(result); await hub.Clients.Group("lobby").SendAsync("RoomChanged", result.Value!.Id, ct); return Results.Created($"/api/rooms/{result.Value.Id}", result.Value); });
        group.MapPost("/rooms/{roomId:guid}/join", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) => { var identity = GetIdentity(principal); if (identity is null) return Results.Unauthorized(); var result = await service.JoinRoomAsync(roomId, identity, ct); if (!result.IsSuccess) return Map(result); await BroadcastRoomChanged(hub, roomId, result.Value!, ct); return Results.Ok(result.Value); });
        group.MapPost("/rooms/{roomId:guid}/bots", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) => { var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); var result = await service.AddBotAsync(roomId, userId.Value, ct); if (!result.IsSuccess) return Map(result); await BroadcastRoomChanged(hub, roomId, result.Value!, ct); return Results.Ok(result.Value); });
        group.MapPost("/rooms/{roomId:guid}/bots/fill", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) => { var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); var result = await service.FillBotsAsync(roomId, userId.Value, ct); if (!result.IsSuccess) return Map(result); await BroadcastRoomChanged(hub, roomId, result.Value!, ct); return Results.Ok(result.Value); });
        group.MapDelete("/rooms/{roomId:guid}/bots/{botUserId:guid}", async (Guid roomId, Guid botUserId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) => { var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); var result = await service.RemoveBotAsync(roomId, botUserId, userId.Value, ct); if (!result.IsSuccess) return Map(result); await BroadcastRoomChanged(hub, roomId, result.Value!, ct); return Results.Ok(result.Value); });
        group.MapPost("/rooms/{roomId:guid}/members/{memberUserId:guid}/kick", async (Guid roomId, Guid memberUserId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
            var result = await service.KickMemberAsync(roomId, memberUserId, userId.Value, ct); if (!result.IsSuccess) return Map(result);
            await BroadcastRoomChanged(hub, roomId, result.Value!, ct);
            await hub.Clients.Group($"room:{roomId}").SendAsync("RoomAccessRevoked", memberUserId, ct);
            return Results.Ok(result.Value);
        });
        group.MapDelete("/rooms/{roomId:guid}", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
            var result = await service.DeleteRoomAsync(roomId, userId.Value, ct); if (!result.IsSuccess) return Map(result);
            await hub.Clients.Group("lobby").SendAsync("RoomRemoved", roomId, ct);
            await hub.Clients.Group($"room:{roomId}").SendAsync("RoomRemoved", roomId, ct);
            return Results.Ok(new { removed = true });
        });
        group.MapPost("/rooms/{roomId:guid}/start", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) => { var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); var result = await service.StartMatchAsync(roomId, userId.Value, ct); if (!result.IsSuccess) return Map(result); await BroadcastMatchStarted(hub, roomId, result.Value!, ct); return Results.Ok(result.Value); });
        group.MapPost("/rooms/{roomId:guid}/return", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
            var result = await service.ReturnToLobbyAsync(roomId, userId.Value, ct);
            if (!result.IsSuccess && result.ErrorCode == "match_in_progress")
            {
                // A stale browser may still show the completed match while another participant
                // has already started the rematch. Return the authoritative room instead of 409;
                // LobbyView will see InGame + ActiveMatchId and route this client to the new match.
                var current = await service.GetRoomAsync(roomId, ct);
                if (current.IsSuccess && current.Value is not null && string.Equals(current.Value.Status, "InGame", StringComparison.OrdinalIgnoreCase) && current.Value.ActiveMatchId.HasValue)
                {
                    await BroadcastRoomChanged(hub, roomId, current.Value, ct);
                    return Results.Ok(current.Value);
                }
            }
            if (!result.IsSuccess) return Map(result);
            await BroadcastRoomChanged(hub, roomId, result.Value!, ct);
            return Results.Ok(result.Value);
        });
        group.MapPost("/rooms/{roomId:guid}/rematch", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) => { var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized(); var result = await service.RematchAsync(roomId, userId.Value, ct); if (!result.IsSuccess) return Map(result); await BroadcastMatchStarted(hub, roomId, result.Value!, ct); return Results.Ok(result.Value); });
        group.MapPost("/rooms/{roomId:guid}/leave", async (Guid roomId, ClaimsPrincipal principal, LobbyApplicationService service, IHubContext<LobbyHub> hub, CancellationToken ct) =>
        {
            var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
            var result = await service.LeaveRoomAsync(roomId, userId.Value, ct); if (!result.IsSuccess) return Map(result);
            await hub.Clients.Group($"room:{roomId}").SendAsync("RoomAccessRevoked", userId.Value, ct);
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

    private static Task BroadcastRoomChanged(IHubContext<LobbyHub> hub, Guid roomId, RoomDetails room, CancellationToken ct) => Task.WhenAll(hub.Clients.Group("lobby").SendAsync("RoomChanged", roomId, ct), hub.Clients.Group($"room:{roomId}").SendAsync("RoomUpdated", room, ct));
    private static Task BroadcastMatchStarted(IHubContext<LobbyHub> hub, Guid roomId, RoomDetails room, CancellationToken ct) => Task.WhenAll(hub.Clients.Group("lobby").SendAsync("RoomChanged", roomId, ct), hub.Clients.Group($"room:{roomId}").SendAsync("RoomUpdated", room, ct), hub.Clients.Group($"room:{roomId}").SendAsync("MatchStarted", room.ActiveMatchId, ct));
    private static PlayerIdentity? GetIdentity(ClaimsPrincipal principal) { var id = GetUserId(principal); var username = principal.FindFirstValue(ClaimTypes.Name); var displayName = principal.FindFirstValue("display_name") ?? username; return id.HasValue && !string.IsNullOrWhiteSpace(username) ? new PlayerIdentity(id.Value, username, displayName ?? username) : null; }
    private static Guid? GetUserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static bool HasInternalKey(HttpContext http, IConfiguration configuration) { var expected = configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me"; return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) && provided == expected; }
    private static IResult Map<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess) return Results.Ok(result.Value);
        var status = result.ErrorCode switch
        {
            "room_not_found" or "game_not_found" or "bot_not_found" or "member_not_found" or "rematch_match_not_found" or "match_not_found" => StatusCodes.Status404NotFound,
            "host_only" or "game_not_enabled" or "not_in_room" => StatusCodes.Status403Forbidden,
            "already_in_room" or "room_full" or "room_not_open" or "join_conflict" or "bot_conflict" or "not_enough_players" or "match_in_progress" or "match_completed" or "cannot_kick_host" or "rematch_not_available" or "rematch_not_ready" or "rematch_roster_changed" or "rematch_room_mismatch" or "match_room_mismatch" or "room_state_invalid" => StatusCodes.Status409Conflict,
            "game_service_unavailable" or "game_service_error" or "game_not_supported" => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(new { error = result.ErrorCode, message = result.ErrorMessage }, statusCode: status);
    }
}

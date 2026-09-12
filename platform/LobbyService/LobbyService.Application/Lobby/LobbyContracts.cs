namespace LobbyService.Application.Lobby;

public sealed record PlayerIdentity(Guid UserId, string Username, string DisplayName);
public sealed record CreateRoomRequest(string GameSlug, string? Name, int? MaxPlayers);

public sealed record GameCatalogItem(
    string Slug,
    string DisplayName,
    string Icon,
    int MinPlayers,
    int MaxPlayers,
    bool IsEnabled);

public sealed record RoomMemberDto(
    Guid UserId,
    string Username,
    string DisplayName,
    int SeatNumber,
    bool IsHost,
    DateTimeOffset JoinedAtUtc);

public sealed record RoomSummary(
    Guid Id,
    string Name,
    string GameSlug,
    string GameName,
    string GameIcon,
    int PlayerCount,
    int MaxPlayers,
    string Status,
    Guid HostUserId,
    DateTimeOffset UpdatedAtUtc);

public sealed record RoomDetails(
    Guid Id,
    string Name,
    string GameSlug,
    string GameName,
    string GameIcon,
    int MinPlayers,
    int MaxPlayers,
    string Status,
    Guid HostUserId,
    Guid? ActiveMatchId,
    IReadOnlyList<RoomMemberDto> Members,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record LeaveRoomResult(bool RoomRemoved, RoomDetails? Room);

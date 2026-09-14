using LobbyService.Application.Abstractions;
using LobbyService.Application.Common;
using LobbyService.Domain.Rooms;

namespace LobbyService.Application.Lobby;

public sealed class LobbyApplicationService(ILobbyRepository repository, IMatchLauncher matchLauncher)
{
    public async Task<IReadOnlyList<GameCatalogItem>> ListGamesAsync(CancellationToken ct)
    {
        var games = await repository.ListGamesAsync(ct);
        return games.Select(x => new GameCatalogItem(x.Slug, x.DisplayName, x.Icon, x.MinPlayers, x.MaxPlayers, x.IsEnabled)).ToList();
    }

    public async Task<IReadOnlyList<RoomSummary>> ListRoomsAsync(string? gameSlug, CancellationToken ct)
    {
        var rooms = await repository.ListRoomsAsync(gameSlug, ct);
        return rooms.Select(MapSummary).ToList();
    }

    public async Task<ServiceResult<RoomDetails>> GetRoomAsync(Guid roomId, CancellationToken ct)
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        return room is null
            ? ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng.")
            : ServiceResult<RoomDetails>.Success(MapDetails(room));
    }

    public async Task<ServiceResult<RoomDetails>> GetCurrentRoomForUserAsync(Guid userId, CancellationToken ct)
    {
        var room = await repository.GetRoomForUserAsync(userId, ct);
        return room is null
            ? ServiceResult<RoomDetails>.Failure("not_in_room", "Bạn chưa vào phòng nào.")
            : ServiceResult<RoomDetails>.Success(MapDetails(room));
    }

    public async Task<ServiceResult<RoomDetails>> CreateRoomAsync(PlayerIdentity player, CreateRoomRequest request, CancellationToken ct)
    {
        var currentRoom = await repository.GetRoomForUserAsync(player.UserId, ct);
        if (currentRoom is not null)
            return ServiceResult<RoomDetails>.Failure("already_in_room", "Bạn đang ở trong một phòng khác.");

        var game = await repository.GetGameBySlugAsync((request.GameSlug ?? string.Empty).Trim().ToLowerInvariant(), ct);
        if (game is null) return ServiceResult<RoomDetails>.Failure("game_not_found", "Game không tồn tại.");
        if (!game.IsEnabled) return ServiceResult<RoomDetails>.Failure("game_not_enabled", "Game này chưa mở chơi.");

        var maxPlayers = request.MaxPlayers ?? game.MaxPlayers;
        if (maxPlayers < game.MinPlayers || maxPlayers > game.MaxPlayers)
            return ServiceResult<RoomDetails>.Failure("invalid_capacity", $"Số người phải từ {game.MinPlayers} đến {game.MaxPlayers}.");

        var name = string.IsNullOrWhiteSpace(request.Name) ? $"Bàn của {player.DisplayName}" : request.Name.Trim();
        if (name.Length > 60) return ServiceResult<RoomDetails>.Failure("invalid_room_name", "Tên phòng tối đa 60 ký tự.");

        var room = new Room(Guid.NewGuid(), game.Id, player.UserId, name, maxPlayers, player.Username, player.DisplayName);
        await repository.AddRoomAsync(room, ct);
        await repository.SaveChangesAsync(ct);
        var persisted = await repository.GetRoomAsync(room.Id, ct) ?? room;
        return ServiceResult<RoomDetails>.Success(MapDetails(persisted));
    }

    public async Task<ServiceResult<RoomDetails>> JoinRoomAsync(Guid roomId, PlayerIdentity player, CancellationToken ct)
    {
        var currentRoom = await repository.GetRoomForUserAsync(player.UserId, ct);
        if (currentRoom is not null)
        {
            if (currentRoom.Id == roomId) return ServiceResult<RoomDetails>.Success(MapDetails(currentRoom));
            return ServiceResult<RoomDetails>.Failure("already_in_room", "Bạn đang ở trong một phòng khác.");
        }

        var room = await repository.GetRoomAsync(roomId, ct);
        if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng.");
        if (room.Status != RoomStatus.Open) return ServiceResult<RoomDetails>.Failure("room_not_open", "Phòng đã bắt đầu ván hoặc đã đóng.");
        if (room.Members.Count >= room.MaxPlayers) return ServiceResult<RoomDetails>.Failure("room_full", "Phòng đã đủ người.");

        room.Join(player.UserId, player.Username, player.DisplayName);
        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch
        {
            return ServiceResult<RoomDetails>.Failure("join_conflict", "Có người vừa chiếm ghế này. Hãy thử lại.");
        }
        return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }

    public async Task<ServiceResult<RoomDetails>> AddBotAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        var validation = ValidateBotMutation(room, userId);
        if (validation is not null) return validation;
        if (room!.Members.Count >= room.MaxPlayers) return ServiceResult<RoomDetails>.Failure("room_full", "Phòng đã đủ người.");

        room.AddBot();
        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch
        {
            return ServiceResult<RoomDetails>.Failure("bot_conflict", "Ghế bot vừa bị chiếm. Hãy thử lại.");
        }
        return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }

    public async Task<ServiceResult<RoomDetails>> FillBotsAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        var validation = ValidateBotMutation(room, userId);
        if (validation is not null) return validation;

        while (room!.Members.Count < room.MaxPlayers) room.AddBot();
        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch
        {
            return ServiceResult<RoomDetails>.Failure("bot_conflict", "Không thể lấp đầy bot do có thay đổi ghế đồng thời.");
        }
        return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }

    public async Task<ServiceResult<RoomDetails>> RemoveBotAsync(Guid roomId, Guid botUserId, Guid userId, CancellationToken ct)
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        var validation = ValidateBotMutation(room, userId);
        if (validation is not null) return validation;
        if (!room!.RemoveBot(botUserId)) return ServiceResult<RoomDetails>.Failure("bot_not_found", "Không tìm thấy bot trong phòng.");

        await repository.SaveChangesAsync(ct);
        return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }

    public async Task<ServiceResult<RoomDetails>> StartMatchAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng.");
        if (room.HostUserId != userId) return ServiceResult<RoomDetails>.Failure("host_only", "Chỉ host mới được bắt đầu ván.");
        if (room.Status != RoomStatus.Open) return ServiceResult<RoomDetails>.Failure("room_not_open", "Phòng đã bắt đầu ván.");
        if (room.Members.Count < room.GameDefinition.MinPlayers)
            return ServiceResult<RoomDetails>.Failure("not_enough_players", $"Cần ít nhất {room.GameDefinition.MinPlayers} người để bắt đầu.");

        var players = room.Members.OrderBy(x => x.SeatNumber)
            .Select(x => new MatchLaunchPlayer(x.UserId, x.SeatNumber, x.Username, x.DisplayName, x.IsBot)).ToArray();
        var launch = await matchLauncher.StartAsync(room.GameDefinition.Slug, room.Id, players, ct);
        if (!launch.IsSuccess || launch.MatchId is null)
            return ServiceResult<RoomDetails>.Failure(launch.ErrorCode ?? "game_service_unavailable", launch.ErrorMessage ?? "Không khởi tạo được ván chơi.");

        room.AttachMatch(launch.MatchId.Value);
        await repository.SaveChangesAsync(ct);
        return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }

    public async Task<ServiceResult<LeaveRoomResult>> LeaveRoomAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        if (room is null) return ServiceResult<LeaveRoomResult>.Failure("room_not_found", "Không tìm thấy phòng.");
        if (room.Status == RoomStatus.InGame) return ServiceResult<LeaveRoomResult>.Failure("match_in_progress", "Không thể rời phòng khi ván đang diễn ra.");
        if (!room.Leave(userId)) return ServiceResult<LeaveRoomResult>.Failure("not_in_room", "Bạn không ở trong phòng này.");

        if (room.Members.Count == 0)
        {
            repository.RemoveRoom(room);
            await repository.SaveChangesAsync(ct);
            return ServiceResult<LeaveRoomResult>.Success(new LeaveRoomResult(true, null));
        }

        await repository.SaveChangesAsync(ct);
        return ServiceResult<LeaveRoomResult>.Success(new LeaveRoomResult(false, MapDetails(room)));
    }

    private static ServiceResult<RoomDetails>? ValidateBotMutation(Room? room, Guid userId)
    {
        if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng.");
        if (room.HostUserId != userId) return ServiceResult<RoomDetails>.Failure("host_only", "Chỉ host mới được quản lý bot.");
        if (room.Status != RoomStatus.Open) return ServiceResult<RoomDetails>.Failure("room_not_open", "Chỉ quản lý bot khi phòng đang mở.");
        return null;
    }

    private static RoomSummary MapSummary(Room room) => new(
        room.Id, room.Name, room.GameDefinition.Slug, room.GameDefinition.DisplayName, room.GameDefinition.Icon,
        room.Members.Count, room.MaxPlayers, room.Status.ToString(), room.HostUserId, room.UpdatedAtUtc);

    private static RoomDetails MapDetails(Room room) => new(
        room.Id, room.Name, room.GameDefinition.Slug, room.GameDefinition.DisplayName, room.GameDefinition.Icon,
        room.GameDefinition.MinPlayers, room.MaxPlayers, room.Status.ToString(), room.HostUserId, room.ActiveMatchId,
        room.Members.OrderBy(x => x.SeatNumber).Select(x => new RoomMemberDto(
            x.UserId, x.Username, x.DisplayName, x.SeatNumber, x.UserId == room.HostUserId, x.IsBot, x.JoinedAtUtc)).ToList(),
        room.CreatedAtUtc, room.UpdatedAtUtc);
}

using LobbyService.Application.Abstractions;
using LobbyService.Application.Common;
using LobbyService.Domain.Rooms;

namespace LobbyService.Application.Lobby;

public sealed class LobbyApplicationService(ILobbyRepository repository, IMatchLauncher matchLauncher, RoomLifecycleLock lifecycleLock)
{
    public async Task<IReadOnlyList<GameCatalogItem>> ListGamesAsync(CancellationToken ct) => (await repository.ListGamesAsync(ct)).Select(x => new GameCatalogItem(x.Slug, x.DisplayName, x.Icon, x.MinPlayers, x.MaxPlayers, x.IsEnabled)).ToList();
    public async Task<IReadOnlyList<RoomSummary>> ListRoomsAsync(string? gameSlug, CancellationToken ct) => (await repository.ListRoomsAsync(gameSlug, ct)).Select(MapSummary).ToList();
    public async Task<ServiceResult<RoomDetails>> GetRoomAsync(Guid roomId, CancellationToken ct) { var room = await repository.GetRoomAsync(roomId, ct); return room is null ? ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng.") : ServiceResult<RoomDetails>.Success(MapDetails(room)); }
    public async Task<ServiceResult<RoomDetails>> GetCurrentRoomForUserAsync(Guid userId, CancellationToken ct) { var room = await repository.GetRoomForUserAsync(userId, ct); return room is null ? ServiceResult<RoomDetails>.Failure("not_in_room", "Bạn chưa vào phòng nào.") : ServiceResult<RoomDetails>.Success(MapDetails(room)); }

    public async Task<ServiceResult<RoomDetails>> CreateRoomAsync(PlayerIdentity player, CreateRoomRequest request, CancellationToken ct)
    {
        var currentRoom = await repository.GetRoomForUserAsync(player.UserId, ct); if (currentRoom is not null) return ServiceResult<RoomDetails>.Failure("already_in_room", "Bạn đang ở trong một phòng khác.");
        var game = await repository.GetGameBySlugAsync((request.GameSlug ?? string.Empty).Trim().ToLowerInvariant(), ct); if (game is null) return ServiceResult<RoomDetails>.Failure("game_not_found", "Game không tồn tại."); if (!game.IsEnabled) return ServiceResult<RoomDetails>.Failure("game_not_enabled", "Game này chưa mở chơi.");
        var maxPlayers = request.MaxPlayers ?? game.MaxPlayers; if (maxPlayers < game.MinPlayers || maxPlayers > game.MaxPlayers) return ServiceResult<RoomDetails>.Failure("invalid_capacity", $"Số người phải từ {game.MinPlayers} đến {game.MaxPlayers}.");
        var name = string.IsNullOrWhiteSpace(request.Name) ? $"Bàn của {player.DisplayName}" : request.Name.Trim(); if (name.Length > 60) return ServiceResult<RoomDetails>.Failure("invalid_room_name", "Tên phòng tối đa 60 ký tự.");
        var room = new Room(Guid.NewGuid(), game.Id, player.UserId, name, maxPlayers, player.Username, player.DisplayName); await repository.AddRoomAsync(room, ct); await repository.SaveChangesAsync(ct); var persisted = await repository.GetRoomAsync(room.Id, ct) ?? room; return ServiceResult<RoomDetails>.Success(MapDetails(persisted));
    }

    public Task<ServiceResult<RoomDetails>> JoinRoomAsync(Guid roomId, PlayerIdentity player, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
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
        try { await repository.SaveChangesAsync(ct); }
        catch { return ServiceResult<RoomDetails>.Failure("join_conflict", "Có người vừa chiếm ghế này. Hãy thử lại."); }
        return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<RoomDetails>> AddBotAsync(Guid roomId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct); var validation = ValidateBotMutation(room, userId); if (validation is not null) return validation; if (room!.Members.Count >= room.MaxPlayers) return ServiceResult<RoomDetails>.Failure("room_full", "Phòng đã đủ người.");
        room.AddBot(); try { await repository.SaveChangesAsync(ct); } catch { return ServiceResult<RoomDetails>.Failure("bot_conflict", "Ghế bot vừa bị chiếm. Hãy thử lại."); } return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<RoomDetails>> FillBotsAsync(Guid roomId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct); var validation = ValidateBotMutation(room, userId); if (validation is not null) return validation; while (room!.Members.Count < room.MaxPlayers) room.AddBot();
        try { await repository.SaveChangesAsync(ct); } catch { return ServiceResult<RoomDetails>.Failure("bot_conflict", "Không thể lấp đầy bot do có thay đổi ghế đồng thời."); } return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<RoomDetails>> RemoveBotAsync(Guid roomId, Guid botUserId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct); var validation = ValidateBotMutation(room, userId); if (validation is not null) return validation; if (!room!.RemoveBot(botUserId)) return ServiceResult<RoomDetails>.Failure("bot_not_found", "Không tìm thấy bot trong phòng."); await repository.SaveChangesAsync(ct); return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<RoomDetails>> KickMemberAsync(Guid roomId, Guid targetUserId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng.");
        if (room.HostUserId != userId) return ServiceResult<RoomDetails>.Failure("host_only", "Chỉ host mới được mời người chơi ra khỏi bàn.");
        if (room.Status != RoomStatus.Open) return ServiceResult<RoomDetails>.Failure("room_not_open", "Chỉ có thể kick khi bàn chưa bắt đầu ván.");
        if (targetUserId == userId) return ServiceResult<RoomDetails>.Failure("cannot_kick_host", "Host không thể tự kick chính mình.");
        if (!room.Kick(targetUserId)) return ServiceResult<RoomDetails>.Failure("member_not_found", "Không tìm thấy người chơi cần kick.");
        await repository.SaveChangesAsync(ct);
        return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<bool>> DeleteRoomAsync(Guid roomId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        if (room is null) return ServiceResult<bool>.Failure("room_not_found", "Không tìm thấy phòng.");
        if (room.HostUserId != userId) return ServiceResult<bool>.Failure("host_only", "Chỉ host mới được xóa bàn.");
        if (room.Status != RoomStatus.Open) return ServiceResult<bool>.Failure("match_in_progress", "Không thể xóa bàn khi ván đang diễn ra.");
        repository.RemoveRoom(room); await repository.SaveChangesAsync(ct); return ServiceResult<bool>.Success(true);
    }, ct);

    public Task<ServiceResult<RoomDetails>> StartMatchAsync(Guid roomId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct); if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng."); if (room.HostUserId != userId) return ServiceResult<RoomDetails>.Failure("host_only", "Chỉ host mới được bắt đầu ván."); if (room.Status != RoomStatus.Open) return ServiceResult<RoomDetails>.Failure("room_not_open", "Phòng đã bắt đầu ván."); if (room.Members.Count < room.GameDefinition.MinPlayers) return ServiceResult<RoomDetails>.Failure("not_enough_players", $"Cần ít nhất {room.GameDefinition.MinPlayers} người để bắt đầu.");
        var launch = await matchLauncher.StartAsync(room.GameDefinition.Slug, room.Id, BuildLaunchPlayers(room), ct); if (!launch.IsSuccess || launch.MatchId is null) return ServiceResult<RoomDetails>.Failure(launch.ErrorCode ?? "game_service_unavailable", launch.ErrorMessage ?? "Không khởi tạo được ván chơi.");
        room.AttachMatch(launch.MatchId.Value); await repository.SaveChangesAsync(ct); return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<RoomDetails>> ReturnToLobbyAsync(Guid roomId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct); if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng."); if (!room.Members.Any(x => !x.IsBot && x.UserId == userId)) return ServiceResult<RoomDetails>.Failure("not_in_room", "Bạn không thuộc phòng này."); if (room.Status == RoomStatus.Open && room.ActiveMatchId is null) return ServiceResult<RoomDetails>.Success(MapDetails(room)); if (room.Status != RoomStatus.InGame || room.ActiveMatchId is null) return ServiceResult<RoomDetails>.Failure("room_state_invalid", "Trạng thái phòng không hợp lệ.");
        var activeMatchId = room.ActiveMatchId.Value; var summary = await matchLauncher.GetSummaryAsync(room.GameDefinition.Slug, activeMatchId, ct); if (!summary.IsSuccess) return ServiceResult<RoomDetails>.Failure(summary.ErrorCode ?? "game_service_error", summary.ErrorMessage ?? "Không kiểm tra được trạng thái ván."); if (summary.MatchId != activeMatchId || summary.RoomId != room.Id) return ServiceResult<RoomDetails>.Failure("match_room_mismatch", "Ván chơi không thuộc phòng này."); if (!string.Equals(summary.Status, "Completed", StringComparison.OrdinalIgnoreCase)) return ServiceResult<RoomDetails>.Failure("match_in_progress", "Ván vẫn đang diễn ra.");
        room.CompleteActiveMatch(activeMatchId); await repository.SaveChangesAsync(ct); return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<RoomDetails>> RematchAsync(Guid roomId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct); if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng."); if (!room.Members.Any(x => !x.IsBot && x.UserId == userId)) return ServiceResult<RoomDetails>.Failure("not_in_room", "Bạn không thuộc phòng này.");
        var previousMatchId = room.Status switch { RoomStatus.InGame when room.ActiveMatchId.HasValue => room.ActiveMatchId, RoomStatus.Open when room.LastCompletedMatchId.HasValue => room.LastCompletedMatchId, _ => null }; if (previousMatchId is null) return ServiceResult<RoomDetails>.Failure("rematch_not_available", "Chưa có ván hoàn tất để đánh lại.");
        var launch = await matchLauncher.RematchAsync(room.GameDefinition.Slug, previousMatchId.Value, room.Id, BuildLaunchPlayers(room), ct); if (!launch.IsSuccess || launch.MatchId is null) return ServiceResult<RoomDetails>.Failure(launch.ErrorCode ?? "game_service_unavailable", launch.ErrorMessage ?? "Không thể tạo ván đánh lại."); room.AttachMatch(launch.MatchId.Value); await repository.SaveChangesAsync(ct); return ServiceResult<RoomDetails>.Success(MapDetails(room));
    }, ct);

    public Task<ServiceResult<LeaveRoomResult>> LeaveRoomAsync(Guid roomId, Guid userId, CancellationToken ct) => lifecycleLock.ExecuteAsync(roomId, async () =>
    {
        var room = await repository.GetRoomAsync(roomId, ct);
        if (room is null) return ServiceResult<LeaveRoomResult>.Failure("room_not_found", "Không tìm thấy phòng.");
        if (!room.Members.Any(x => !x.IsBot && x.UserId == userId)) return ServiceResult<LeaveRoomResult>.Failure("not_in_room", "Bạn không ở trong phòng này.");

        if (room.Status == RoomStatus.InGame)
        {
            if (room.ActiveMatchId is null) return ServiceResult<LeaveRoomResult>.Failure("room_state_invalid", "Phòng đang chơi nhưng không có match hợp lệ.");
            var activeMatchId = room.ActiveMatchId.Value;
            var abandoned = await matchLauncher.AbandonAsync(room.GameDefinition.Slug, activeMatchId, userId, ct);
            if (!abandoned.IsSuccess)
            {
                if (abandoned.ErrorCode != "match_completed")
                    return ServiceResult<LeaveRoomResult>.Failure(abandoned.ErrorCode ?? "game_service_error", abandoned.ErrorMessage ?? "Không thể bỏ ván hiện tại.");

                var summary = await matchLauncher.GetSummaryAsync(room.GameDefinition.Slug, activeMatchId, ct);
                if (!summary.IsSuccess || summary.MatchId != activeMatchId || summary.RoomId != room.Id || !string.Equals(summary.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    return ServiceResult<LeaveRoomResult>.Failure(summary.ErrorCode ?? "game_service_error", summary.ErrorMessage ?? "Không xác nhận được trạng thái ván vừa kết thúc.");
                room.CompleteActiveMatch(activeMatchId);
            }
        }

        if (!room.Leave(userId)) return ServiceResult<LeaveRoomResult>.Failure("not_in_room", "Bạn không ở trong phòng này.");
        if (room.Members.Count == 0)
        {
            repository.RemoveRoom(room); await repository.SaveChangesAsync(ct); return ServiceResult<LeaveRoomResult>.Success(new LeaveRoomResult(true, null));
        }
        await repository.SaveChangesAsync(ct);
        return ServiceResult<LeaveRoomResult>.Success(new LeaveRoomResult(false, MapDetails(room)));
    }, ct);

    private static MatchLaunchPlayer[] BuildLaunchPlayers(Room room) => room.Members.OrderBy(x => x.SeatNumber).Select(x => new MatchLaunchPlayer(x.UserId, x.SeatNumber, x.Username, x.DisplayName, x.IsBot)).ToArray();
    private static ServiceResult<RoomDetails>? ValidateBotMutation(Room? room, Guid userId) { if (room is null) return ServiceResult<RoomDetails>.Failure("room_not_found", "Không tìm thấy phòng."); if (room.HostUserId != userId) return ServiceResult<RoomDetails>.Failure("host_only", "Chỉ host mới được quản lý bot."); if (room.Status != RoomStatus.Open) return ServiceResult<RoomDetails>.Failure("room_not_open", "Chỉ quản lý bot khi phòng đang mở."); return null; }
    private static RoomSummary MapSummary(Room room) => new(room.Id, room.Name, room.GameDefinition.Slug, room.GameDefinition.DisplayName, room.GameDefinition.Icon, room.Members.Count, room.MaxPlayers, room.Status.ToString(), room.HostUserId, room.UpdatedAtUtc);
    private static RoomDetails MapDetails(Room room) => new(room.Id, room.Name, room.GameDefinition.Slug, room.GameDefinition.DisplayName, room.GameDefinition.Icon, room.GameDefinition.MinPlayers, room.MaxPlayers, room.Status.ToString(), room.HostUserId, room.ActiveMatchId, room.LastCompletedMatchId, room.Members.OrderBy(x => x.SeatNumber).Select(x => new RoomMemberDto(x.UserId, x.Username, x.DisplayName, x.SeatNumber, x.UserId == room.HostUserId, x.IsBot, x.JoinedAtUtc)).ToList(), room.CreatedAtUtc, room.UpdatedAtUtc);
}

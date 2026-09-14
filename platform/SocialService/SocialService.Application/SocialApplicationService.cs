using SocialService.Domain;

namespace SocialService.Application;

public sealed class SocialApplicationService(IRoomAccessGateway roomAccess, IRecentSocialHistory history)
{
    public async Task<SocialResult<IReadOnlyList<SocialChatEvent>>> GetRecentAsync(Guid roomId, Guid userId, int limit, CancellationToken ct)
    {
        var access = await RequireHumanMemberAsync(roomId, userId, ct);
        if (!access.IsSuccess) return SocialResult<IReadOnlyList<SocialChatEvent>>.Failure(access.ErrorCode!, access.ErrorMessage!);
        var bounded = Math.Clamp(limit, 1, 50);
        return SocialResult<IReadOnlyList<SocialChatEvent>>.Success(await history.GetRecentAsync(roomId, bounded, ct));
    }

    public Task<SocialResult<SocialChatEvent>> SendQuickChatAsync(Guid roomId, Guid userId, string? text, CancellationToken ct) =>
        SendChatAsync(roomId, userId, text, "taunt", 80, ct);

    public Task<SocialResult<SocialChatEvent>> SendRoomChatAsync(Guid roomId, Guid userId, string? text, CancellationToken ct) =>
        SendChatAsync(roomId, userId, text, "chat", 300, ct);

    public async Task<SocialResult<SocialReactionEvent>> CreateReactionAsync(Guid roomId, Guid userId, string? reactionType, Guid targetUserId, CancellationToken ct)
    {
        if (targetUserId == userId) return SocialResult<SocialReactionEvent>.Failure("self_target", "Không thể tự ném chính mình.");
        var access = await RequireHumanMemberAsync(roomId, userId, ct);
        if (!access.IsSuccess) return SocialResult<SocialReactionEvent>.Failure(access.ErrorCode!, access.ErrorMessage!);
        var room = access.Value!.Room;
        if (!room.Members.Any(x => x.UserId == targetUserId)) return SocialResult<SocialReactionEvent>.Failure("target_not_in_room", "Mục tiêu không thuộc phòng này.");
        if (!ReactionCatalog.TryResolve(reactionType, out var type, out var emoji)) return SocialResult<SocialReactionEvent>.Failure("invalid_reaction", "Vật phẩm không hợp lệ.");
        var sender = access.Value.Member;
        return SocialResult<SocialReactionEvent>.Success(new SocialReactionEvent(Guid.NewGuid(), roomId, userId, sender.DisplayName, targetUserId, type, emoji, DateTimeOffset.UtcNow));
    }

    public async Task<SocialResult<IReadOnlyList<Guid>>> GetHumanAudienceAsync(Guid roomId, CancellationToken ct)
    {
        if (roomId == Guid.Empty) return SocialResult<IReadOnlyList<Guid>>.Failure("invalid_room", "RoomId không hợp lệ.");
        var room = await roomAccess.GetRoomAsync(roomId, ct);
        if (room is null) return SocialResult<IReadOnlyList<Guid>>.Failure("room_not_found", "Không tìm thấy phòng.");
        return SocialResult<IReadOnlyList<Guid>>.Success(room.Members.Where(x => !x.IsBot).Select(x => x.UserId).Distinct().ToArray());
    }

    private async Task<SocialResult<SocialAccess>> RequireHumanMemberAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        if (roomId == Guid.Empty) return SocialResult<SocialAccess>.Failure("invalid_room", "RoomId không hợp lệ.");
        var room = await roomAccess.GetRoomAsync(roomId, ct);
        if (room is null) return SocialResult<SocialAccess>.Failure("room_not_found", "Không tìm thấy phòng.");
        var member = room.Members.SingleOrDefault(x => x.UserId == userId && !x.IsBot);
        return member is null
            ? SocialResult<SocialAccess>.Failure("not_in_room", "Bạn không thuộc phòng này.")
            : SocialResult<SocialAccess>.Success(new SocialAccess(room, member));
    }

    private async Task<SocialResult<SocialChatEvent>> SendChatAsync(Guid roomId, Guid userId, string? text, string kind, int maxLength, CancellationToken ct)
    {
        var access = await RequireHumanMemberAsync(roomId, userId, ct);
        if (!access.IsSuccess) return SocialResult<SocialChatEvent>.Failure(access.ErrorCode!, access.ErrorMessage!);
        var clean = SocialText.Normalize(text, maxLength);
        if (string.IsNullOrWhiteSpace(clean)) return SocialResult<SocialChatEvent>.Failure("empty_message", "Tin nhắn trống.");
        var message = new SocialChatEvent(Guid.NewGuid(), roomId, userId, access.Value!.Member.DisplayName, clean, kind, DateTimeOffset.UtcNow);
        await history.AddAsync(message, ct);
        return SocialResult<SocialChatEvent>.Success(message);
    }

    private sealed record SocialAccess(RoomSocialContext Room, RoomSocialMember Member);
}

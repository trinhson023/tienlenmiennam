namespace SocialService.Application;

public sealed record RoomSocialMember(Guid UserId, string Username, string DisplayName, bool IsBot);
public sealed record RoomSocialContext(Guid RoomId, string Status, IReadOnlyList<RoomSocialMember> Members);

public sealed record SocialChatEvent(
    Guid EventId,
    Guid RoomId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    string Kind,
    DateTimeOffset SentAtUtc);

public sealed record SocialReactionEvent(
    Guid EventId,
    Guid RoomId,
    Guid SenderUserId,
    string SenderDisplayName,
    Guid TargetUserId,
    string Type,
    string Emoji,
    DateTimeOffset SentAtUtc);

public sealed record SocialResult<T>(bool IsSuccess, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static SocialResult<T> Success(T value) => new(true, value, null, null);
    public static SocialResult<T> Failure(string code, string message) => new(false, default, code, message);
}

namespace TienLenService.Api;

public sealed record MatchQuickChatEvent(
    Guid EventId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    DateTimeOffset SentAtUtc);

public sealed record MatchThrowEvent(
    Guid EventId,
    Guid SenderUserId,
    string SenderDisplayName,
    Guid TargetUserId,
    string Type,
    string Emoji,
    DateTimeOffset SentAtUtc);

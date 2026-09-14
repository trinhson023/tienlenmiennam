namespace SocialService.Infrastructure.Persistence;

public sealed class SocialMessageRecord
{
    public Guid EventId { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; set; }
}

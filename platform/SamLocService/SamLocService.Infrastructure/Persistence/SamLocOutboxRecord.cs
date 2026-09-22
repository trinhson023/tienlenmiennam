namespace SamLocService.Infrastructure.Persistence;

public sealed class SamLocOutboxRecord
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

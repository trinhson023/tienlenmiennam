namespace SamLocService.Infrastructure.Persistence;

public sealed class SamLocMatchRecord
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public int Status { get; set; }
    public long Version { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTimeOffset? DeclarationDeadlineUtc { get; set; }
    public DateTimeOffset? TurnDeadlineUtc { get; set; }
    public DateTimeOffset? BotActionDueUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

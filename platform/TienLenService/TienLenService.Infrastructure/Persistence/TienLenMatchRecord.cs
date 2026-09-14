namespace TienLenService.Infrastructure.Persistence;

public sealed class TienLenMatchRecord
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public int Status { get; set; }
    public long Version { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTimeOffset? TurnDeadlineUtc { get; set; }
    public DateTimeOffset? BotActionDueUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

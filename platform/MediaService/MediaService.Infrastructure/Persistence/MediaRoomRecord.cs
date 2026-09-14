namespace MediaService.Infrastructure.Persistence;

public sealed class MediaRoomRecord
{
    public Guid RoomId { get; set; }
    public string? CurrentJson { get; set; }
    public string QueueJson { get; set; } = "[]";
    public bool Playing { get; set; }
    public double PositionSeconds { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public long Revision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

namespace StatisticsService.Infrastructure.Persistence;

public sealed class ProcessedMatchRecord
{
    public Guid MatchId { get; set; }
    public Guid EventId { get; set; }
    public string GameSlug { get; set; } = string.Empty;
    public DateTimeOffset CompletedAtUtc { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

namespace StatisticsService.Infrastructure.Persistence;

public sealed class PlayerGameStatisticRecord
{
    public Guid UserId { get; set; }
    public string GameSlug { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Rating { get; set; } = 1000;
    public DateTimeOffset? LastPlayedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

namespace StatisticsService.Domain;

public sealed class PlayerGameStatistic
{
    public PlayerGameStatistic(Guid userId, string gameSlug, string username, string displayName, int rating = 1000)
    {
        UserId = userId;
        GameSlug = gameSlug;
        Username = username;
        DisplayName = displayName;
        Rating = rating;
    }

    public Guid UserId { get; }
    public string GameSlug { get; }
    public string Username { get; private set; }
    public string DisplayName { get; private set; }
    public int GamesPlayed { get; private set; }
    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public int Rating { get; private set; }
    public DateTimeOffset? LastPlayedAtUtc { get; private set; }

    public void ApplyResult(bool won, string username, string displayName, DateTimeOffset completedAtUtc)
    {
        Username = string.IsNullOrWhiteSpace(username) ? Username : username.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? DisplayName : displayName.Trim();
        GamesPlayed++;
        if (won) Wins++; else Losses++;
        LastPlayedAtUtc = completedAtUtc;
    }

    public void Restore(int gamesPlayed, int wins, int losses, int rating, DateTimeOffset? lastPlayedAtUtc)
    {
        GamesPlayed = gamesPlayed;
        Wins = wins;
        Losses = losses;
        Rating = rating;
        LastPlayedAtUtc = lastPlayedAtUtc;
    }
}

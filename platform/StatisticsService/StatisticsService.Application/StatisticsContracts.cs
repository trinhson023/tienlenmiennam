namespace StatisticsService.Application;

public sealed record CompletedMatchPlayer(Guid UserId, string Username, string DisplayName, int SeatNumber, bool IsBot, int FinishPosition);
public sealed record CompletedMatchFact(Guid EventId, Guid MatchId, Guid RoomId, string GameSlug, DateTimeOffset CompletedAtUtc, IReadOnlyList<CompletedMatchPlayer> Players);

public sealed record PlayerStatsView(Guid UserId, string GameSlug, string Username, string DisplayName, int GamesPlayed, int Wins, int Losses, double WinRate, int Rating, DateTimeOffset? LastPlayedAtUtc);
public sealed record LeaderboardEntry(int Rank, Guid UserId, string Username, string DisplayName, int GamesPlayed, int Wins, int Losses, double WinRate, int Rating, DateTimeOffset? LastPlayedAtUtc);

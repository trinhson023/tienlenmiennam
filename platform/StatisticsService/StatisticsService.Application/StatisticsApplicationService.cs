using StatisticsService.Domain;

namespace StatisticsService.Application;

public sealed class StatisticsApplicationService(IStatisticsRepository repository)
{
    public Task<bool> ApplyMatchCompletedAsync(CompletedMatchFact fact, CancellationToken ct)
    {
        if (fact.MatchId == Guid.Empty || string.IsNullOrWhiteSpace(fact.GameSlug) || fact.Players.Count < 2)
            throw new ArgumentException("MatchCompleted payload is invalid.");
        if (fact.Players.Select(x => x.UserId).Distinct().Count() != fact.Players.Count)
            throw new ArgumentException("MatchCompleted contains duplicate players.");
        return repository.ApplyCompletedMatchAsync(fact with { GameSlug = NormalizeGame(fact.GameSlug) }, ct);
    }

    public async Task<PlayerStatsView> GetPlayerAsync(Guid userId, string gameSlug, CancellationToken ct)
    {
        var slug = NormalizeGame(gameSlug);
        var stat = await repository.GetPlayerAsync(userId, slug, ct);
        return stat is null
            ? new PlayerStatsView(userId, slug, string.Empty, string.Empty, 0, 0, 0, 0, 1000, null)
            : Map(stat);
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetLeaderboardAsync(string gameSlug, int limit, CancellationToken ct)
    {
        var slug = NormalizeGame(gameSlug);
        var rows = await repository.GetLeaderboardAsync(slug, Math.Clamp(limit, 1, 100), ct);
        return rows.Select((stat, index) =>
        {
            var view = Map(stat);
            return new LeaderboardEntry(index + 1, view.UserId, view.Username, view.DisplayName, view.GamesPlayed, view.Wins, view.Losses, view.WinRate, view.Rating, view.LastPlayedAtUtc);
        }).ToArray();
    }

    private static PlayerStatsView Map(PlayerGameStatistic stat) => new(
        stat.UserId, stat.GameSlug, stat.Username, stat.DisplayName, stat.GamesPlayed, stat.Wins, stat.Losses,
        stat.GamesPlayed == 0 ? 0 : Math.Round(stat.Wins * 100d / stat.GamesPlayed, 1), stat.Rating, stat.LastPlayedAtUtc);

    private static string NormalizeGame(string gameSlug)
    {
        var slug = (gameSlug ?? string.Empty).Trim().ToLowerInvariant();
        if (slug.Length is < 1 or > 40) throw new ArgumentException("Game slug is invalid.");
        return slug;
    }
}

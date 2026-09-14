using StatisticsService.Domain;

namespace StatisticsService.Application;

public interface IStatisticsRepository
{
    Task<bool> ApplyCompletedMatchAsync(CompletedMatchFact match, CancellationToken ct);
    Task<PlayerGameStatistic?> GetPlayerAsync(Guid userId, string gameSlug, CancellationToken ct);
    Task<IReadOnlyList<PlayerGameStatistic>> GetLeaderboardAsync(string gameSlug, int limit, CancellationToken ct);
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using StatisticsService.Application;
using StatisticsService.Domain;

namespace StatisticsService.Infrastructure.Persistence;

public sealed class StatisticsRepository(IDbContextFactory<StatisticsDbContext> factory) : IStatisticsRepository
{
    public async Task<bool> ApplyCompletedMatchAsync(CompletedMatchFact match, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await db.ProcessedMatches.AsNoTracking().AnyAsync(x => x.MatchId == match.MatchId, ct)) return false;

        foreach (var player in match.Players.Where(x => !x.IsBot))
        {
            var row = await db.PlayerGameStats.SingleOrDefaultAsync(x => x.UserId == player.UserId && x.GameSlug == match.GameSlug, ct);
            if (row is null)
            {
                row = new PlayerGameStatisticRecord { UserId = player.UserId, GameSlug = match.GameSlug, Username = player.Username, DisplayName = player.DisplayName, Rating = 1000 };
                db.PlayerGameStats.Add(row);
            }
            row.Username = string.IsNullOrWhiteSpace(player.Username) ? row.Username : player.Username.Trim();
            row.DisplayName = string.IsNullOrWhiteSpace(player.DisplayName) ? row.DisplayName : player.DisplayName.Trim();
            row.GamesPlayed += 1;
            if (player.FinishPosition == 1) row.Wins += 1; else row.Losses += 1;
            row.LastPlayedAtUtc = match.CompletedAtUtc;
            row.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        db.ProcessedMatches.Add(new ProcessedMatchRecord { MatchId = match.MatchId, EventId = match.EventId, GameSlug = match.GameSlug, CompletedAtUtc = match.CompletedAtUtc, ProcessedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<PlayerGameStatistic?> GetPlayerAsync(Guid userId, string gameSlug, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.PlayerGameStats.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.GameSlug == gameSlug, ct);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<PlayerGameStatistic>> GetLeaderboardAsync(string gameSlug, int limit, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rows = await db.PlayerGameStats.AsNoTracking().Where(x => x.GameSlug == gameSlug && x.GamesPlayed > 0)
            .OrderByDescending(x => x.Rating).ThenByDescending(x => x.Wins).ThenByDescending(x => x.GamesPlayed).ThenBy(x => x.DisplayName)
            .Take(limit).ToListAsync(ct);
        return rows.Select(Map).ToArray();
    }

    private static PlayerGameStatistic Map(PlayerGameStatisticRecord row)
    {
        var stat = new PlayerGameStatistic(row.UserId, row.GameSlug, row.Username, row.DisplayName, row.Rating);
        stat.Restore(row.GamesPlayed, row.Wins, row.Losses, row.Rating, row.LastPlayedAtUtc);
        return stat;
    }
}

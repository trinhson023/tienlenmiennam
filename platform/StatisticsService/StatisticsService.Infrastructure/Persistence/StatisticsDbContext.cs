using Microsoft.EntityFrameworkCore;

namespace StatisticsService.Infrastructure.Persistence;

public sealed class StatisticsDbContext(DbContextOptions<StatisticsDbContext> options) : DbContext(options)
{
    public DbSet<PlayerGameStatisticRecord> PlayerGameStats => Set<PlayerGameStatisticRecord>();
    public DbSet<ProcessedMatchRecord> ProcessedMatches => Set<ProcessedMatchRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("statistics");
        modelBuilder.Entity<PlayerGameStatisticRecord>(entity =>
        {
            entity.ToTable("player_game_stats");
            entity.HasKey(x => new { x.UserId, x.GameSlug });
            entity.Property(x => x.GameSlug).HasMaxLength(40);
            entity.Property(x => x.Username).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => new { x.GameSlug, x.Rating, x.Wins });
            entity.HasIndex(x => new { x.GameSlug, x.LastPlayedAtUtc });
        });
        modelBuilder.Entity<ProcessedMatchRecord>(entity =>
        {
            entity.ToTable("processed_matches");
            entity.HasKey(x => x.MatchId);
            entity.Property(x => x.MatchId).ValueGeneratedNever();
            entity.Property(x => x.EventId).ValueGeneratedNever();
            entity.Property(x => x.GameSlug).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.EventId).IsUnique();
        });
    }
}

using Microsoft.EntityFrameworkCore;

namespace SamLocService.Infrastructure.Persistence;

public sealed class SamLocDbContext(DbContextOptions<SamLocDbContext> options) : DbContext(options)
{
    public DbSet<SamLocMatchRecord> Matches => Set<SamLocMatchRecord>();
    public DbSet<SamLocOutboxRecord> Outbox => Set<SamLocOutboxRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("samloc");

        modelBuilder.Entity<SamLocMatchRecord>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.SnapshotJson).IsRequired();
            entity.HasIndex(x => x.RoomId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.RoomId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<SamLocOutboxRecord>(entity =>
        {
            entity.ToTable("integration_outbox");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(1000);
            entity.HasIndex(x => x.MatchId).IsUnique();
            entity.HasIndex(x => new { x.PublishedAtUtc, x.CreatedAtUtc });
        });
    }
}

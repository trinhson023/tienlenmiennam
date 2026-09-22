using Microsoft.EntityFrameworkCore;

namespace SamLocService.Infrastructure.Persistence;

public sealed class SamLocDbContext(DbContextOptions<SamLocDbContext> options) : DbContext(options)
{
    public DbSet<SamLocMatchRecord> Matches => Set<SamLocMatchRecord>();

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
    }
}

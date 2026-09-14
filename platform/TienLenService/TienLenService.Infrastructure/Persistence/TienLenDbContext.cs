using Microsoft.EntityFrameworkCore;

namespace TienLenService.Infrastructure.Persistence;

public sealed class TienLenDbContext(DbContextOptions<TienLenDbContext> options) : DbContext(options)
{
    public DbSet<TienLenMatchRecord> Matches => Set<TienLenMatchRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tienlen");
        modelBuilder.Entity<TienLenMatchRecord>(entity =>
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

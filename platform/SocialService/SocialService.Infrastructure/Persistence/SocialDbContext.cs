using Microsoft.EntityFrameworkCore;

namespace SocialService.Infrastructure.Persistence;

public sealed class SocialDbContext(DbContextOptions<SocialDbContext> options) : DbContext(options)
{
    public DbSet<SocialMessageRecord> Messages => Set<SocialMessageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("social");
        modelBuilder.Entity<SocialMessageRecord>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
            entity.Property(x => x.SenderDisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Text).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Kind).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.RoomId, x.SentAtUtc });
            entity.HasIndex(x => x.SenderUserId);
        });
    }
}

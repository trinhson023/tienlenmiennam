using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    public DbSet<MediaRoomRecord> Rooms => Set<MediaRoomRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("media");
        modelBuilder.Entity<MediaRoomRecord>(entity =>
        {
            entity.ToTable("room_states"); entity.HasKey(x => x.RoomId); entity.Property(x => x.RoomId).ValueGeneratedNever();
            entity.Property(x => x.CurrentJson); entity.Property(x => x.QueueJson).IsRequired(); entity.HasIndex(x => x.UpdatedAtUtc);
        });
    }
}

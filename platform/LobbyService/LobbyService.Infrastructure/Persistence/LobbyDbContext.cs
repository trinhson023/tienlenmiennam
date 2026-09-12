using LobbyService.Domain.Games;
using LobbyService.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace LobbyService.Infrastructure.Persistence;

public sealed class LobbyDbContext(DbContextOptions<LobbyDbContext> options) : DbContext(options)
{
    public DbSet<GameDefinition> Games => Set<GameDefinition>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("lobby");

        modelBuilder.Entity<GameDefinition>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.Slug).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(16).IsRequired();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasOne(x => x.GameDefinition).WithMany().HasForeignKey(x => x.GameDefinitionId).OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(x => x.Members);
            entity.HasMany<RoomMember>("_members").WithOne(x => x.Room).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.GameDefinitionId);
        });

        modelBuilder.Entity<RoomMember>(entity =>
        {
            entity.ToTable("room_members");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Username).HasMaxLength(32).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.RoomId, x.SeatNumber }).IsUnique();
            entity.HasIndex(x => x.UserId);
        });
    }
}

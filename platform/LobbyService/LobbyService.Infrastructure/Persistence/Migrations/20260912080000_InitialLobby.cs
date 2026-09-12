using LobbyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LobbyService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LobbyDbContext))]
[Migration("20260912080000_InitialLobby")]
public sealed class InitialLobby : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "lobby");
        migrationBuilder.CreateTable(name: "games", schema: "lobby", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false), Type = table.Column<int>(nullable: false), Slug = table.Column<string>(maxLength: 40, nullable: false),
            DisplayName = table.Column<string>(maxLength: 80, nullable: false), Icon = table.Column<string>(maxLength: 16, nullable: false),
            MinPlayers = table.Column<int>(nullable: false), MaxPlayers = table.Column<int>(nullable: false), IsEnabled = table.Column<bool>(nullable: false)
        }, constraints: table => table.PrimaryKey("PK_games", x => x.Id));

        migrationBuilder.CreateTable(name: "rooms", schema: "lobby", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false), GameDefinitionId = table.Column<Guid>(nullable: false), HostUserId = table.Column<Guid>(nullable: false),
            Name = table.Column<string>(maxLength: 60, nullable: false), MaxPlayers = table.Column<int>(nullable: false), Status = table.Column<int>(nullable: false),
            ActiveMatchId = table.Column<Guid>(nullable: true), CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_rooms", x => x.Id);
            table.ForeignKey(name: "FK_rooms_games_GameDefinitionId", column: x => x.GameDefinitionId, principalTable: "games", principalSchema: "lobby", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable(name: "room_members", schema: "lobby", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false), RoomId = table.Column<Guid>(nullable: false), UserId = table.Column<Guid>(nullable: false),
            Username = table.Column<string>(maxLength: 32, nullable: false), DisplayName = table.Column<string>(maxLength: 50, nullable: false),
            SeatNumber = table.Column<int>(nullable: false), JoinedAtUtc = table.Column<DateTimeOffset>(nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_room_members", x => x.Id);
            table.ForeignKey(name: "FK_room_members_rooms_RoomId", column: x => x.RoomId, principalTable: "rooms", principalSchema: "lobby", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("IX_games_Slug", "games", "Slug", schema: "lobby", unique: true);
        migrationBuilder.CreateIndex("IX_rooms_GameDefinitionId", "rooms", "GameDefinitionId", schema: "lobby");
        migrationBuilder.CreateIndex("IX_rooms_Status", "rooms", "Status", schema: "lobby");
        migrationBuilder.CreateIndex("IX_room_members_RoomId_UserId", "room_members", new[] { "RoomId", "UserId" }, schema: "lobby", unique: true);
        migrationBuilder.CreateIndex("IX_room_members_RoomId_SeatNumber", "room_members", new[] { "RoomId", "SeatNumber" }, schema: "lobby", unique: true);
        migrationBuilder.CreateIndex("IX_room_members_UserId", "room_members", "UserId", schema: "lobby");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("room_members", "lobby");
        migrationBuilder.DropTable("rooms", "lobby");
        migrationBuilder.DropTable("games", "lobby");
    }
}

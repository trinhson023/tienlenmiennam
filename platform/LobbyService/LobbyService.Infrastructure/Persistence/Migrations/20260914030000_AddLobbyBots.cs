using LobbyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LobbyService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LobbyDbContext))]
[Migration("20260914030000_AddLobbyBots")]
public sealed class AddLobbyBots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsBot",
            schema: "lobby",
            table: "room_members",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsBot", schema: "lobby", table: "room_members");
    }
}

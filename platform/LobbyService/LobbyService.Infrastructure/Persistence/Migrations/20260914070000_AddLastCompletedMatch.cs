using LobbyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LobbyService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LobbyDbContext))]
[Migration("20260914070000_AddLastCompletedMatch")]
public sealed class AddLastCompletedMatch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "LastCompletedMatchId",
            schema: "lobby",
            table: "rooms",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LastCompletedMatchId", schema: "lobby", table: "rooms");
    }
}

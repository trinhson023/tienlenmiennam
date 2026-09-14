using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MediaService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MediaDbContext))]
[Migration("20260914093000_InitialMediaRoom")]
public sealed class InitialMediaRoom : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "media");
        migrationBuilder.CreateTable(name: "room_states", schema: "media", columns: table => new
        {
            RoomId = table.Column<Guid>(nullable: false), CurrentJson = table.Column<string>(nullable: true), QueueJson = table.Column<string>(nullable: false),
            Playing = table.Column<bool>(nullable: false), PositionSeconds = table.Column<double>(nullable: false), StartedAtUtc = table.Column<DateTimeOffset>(nullable: true),
            Revision = table.Column<long>(nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
        }, constraints: table => table.PrimaryKey("PK_room_states", x => x.RoomId));
        migrationBuilder.CreateIndex(name: "IX_room_states_UpdatedAtUtc", schema: "media", table: "room_states", column: "UpdatedAtUtc");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "room_states", schema: "media");
}

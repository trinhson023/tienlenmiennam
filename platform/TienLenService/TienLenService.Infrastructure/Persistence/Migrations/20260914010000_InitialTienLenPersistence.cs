using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TienLenService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TienLenDbContext))]
[Migration("20260914010000_InitialTienLenPersistence")]
public sealed class InitialTienLenPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "tienlen");
        migrationBuilder.CreateTable(
            name: "matches",
            schema: "tienlen",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                RoomId = table.Column<Guid>(nullable: false),
                Status = table.Column<int>(nullable: false),
                Version = table.Column<long>(nullable: false),
                SnapshotJson = table.Column<string>(nullable: false),
                TurnDeadlineUtc = table.Column<DateTimeOffset>(nullable: true),
                BotActionDueUtc = table.Column<DateTimeOffset>(nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_matches", x => x.Id));

        migrationBuilder.CreateIndex("IX_matches_RoomId", "matches", "RoomId", schema: "tienlen");
        migrationBuilder.CreateIndex("IX_matches_Status", "matches", "Status", schema: "tienlen");
        migrationBuilder.CreateIndex("IX_matches_RoomId_CreatedAtUtc", "matches", new[] { "RoomId", "CreatedAtUtc" }, schema: "tienlen");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("matches", "tienlen");
}

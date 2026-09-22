using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamLocService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SamLocDbContext))]
[Migration("20260922090000_InitialSamLocPersistence")]
public sealed class InitialSamLocPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "samloc");
        migrationBuilder.CreateTable(
            name: "matches",
            schema: "samloc",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                RoomId = table.Column<Guid>(nullable: false),
                Status = table.Column<int>(nullable: false),
                Version = table.Column<long>(nullable: false),
                SnapshotJson = table.Column<string>(nullable: false),
                DeclarationDeadlineUtc = table.Column<DateTimeOffset>(nullable: true),
                TurnDeadlineUtc = table.Column<DateTimeOffset>(nullable: true),
                BotActionDueUtc = table.Column<DateTimeOffset>(nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_matches", x => x.Id));

        migrationBuilder.CreateIndex("IX_matches_RoomId", "matches", "RoomId", schema: "samloc");
        migrationBuilder.CreateIndex("IX_matches_Status", "matches", "Status", schema: "samloc");
        migrationBuilder.CreateIndex("IX_matches_RoomId_CreatedAtUtc", "matches", new[] { "RoomId", "CreatedAtUtc" }, schema: "samloc");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("matches", "samloc");
}

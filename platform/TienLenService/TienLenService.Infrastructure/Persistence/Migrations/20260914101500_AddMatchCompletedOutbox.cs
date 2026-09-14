using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TienLenService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TienLenDbContext))]
[Migration("20260914101500_AddMatchCompletedOutbox")]
public sealed class AddMatchCompletedOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "integration_outbox",
            schema: "tienlen",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                MatchId = table.Column<Guid>(nullable: false),
                EventType = table.Column<string>(maxLength: 120, nullable: false),
                PayloadJson = table.Column<string>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                PublishedAtUtc = table.Column<DateTimeOffset>(nullable: true),
                Attempts = table.Column<int>(nullable: false),
                LastError = table.Column<string>(maxLength: 1000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_integration_outbox", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_integration_outbox_MatchId", schema: "tienlen", table: "integration_outbox", column: "MatchId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_integration_outbox_PublishedAtUtc_CreatedAtUtc", schema: "tienlen", table: "integration_outbox", columns: new[] { "PublishedAtUtc", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "integration_outbox", schema: "tienlen");
}

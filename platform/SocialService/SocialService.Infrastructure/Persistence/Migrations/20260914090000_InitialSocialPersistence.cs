using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SocialService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SocialDbContext))]
[Migration("20260914090000_InitialSocialPersistence")]
public sealed class InitialSocialPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "social");
        migrationBuilder.CreateTable(
            name: "messages",
            schema: "social",
            columns: table => new
            {
                EventId = table.Column<Guid>(nullable: false),
                RoomId = table.Column<Guid>(nullable: false),
                SenderUserId = table.Column<Guid>(nullable: false),
                SenderDisplayName = table.Column<string>(maxLength: 120, nullable: false),
                Text = table.Column<string>(maxLength: 300, nullable: false),
                Kind = table.Column<string>(maxLength: 20, nullable: false),
                SentAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_messages", x => x.EventId));

        migrationBuilder.CreateIndex(name: "IX_messages_RoomId_SentAtUtc", schema: "social", table: "messages", columns: new[] { "RoomId", "SentAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_messages_SenderUserId", schema: "social", table: "messages", column: "SenderUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "messages", schema: "social");
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StatisticsService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(StatisticsDbContext))]
[Migration("20260914102000_InitialStatistics")]
public sealed class InitialStatistics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "statistics");
        migrationBuilder.CreateTable(
            name: "player_game_stats", schema: "statistics",
            columns: table => new
            {
                UserId = table.Column<Guid>(nullable: false), GameSlug = table.Column<string>(maxLength: 40, nullable: false),
                Username = table.Column<string>(maxLength: 80, nullable: false), DisplayName = table.Column<string>(maxLength: 120, nullable: false),
                GamesPlayed = table.Column<int>(nullable: false), Wins = table.Column<int>(nullable: false), Losses = table.Column<int>(nullable: false), Rating = table.Column<int>(nullable: false),
                LastPlayedAtUtc = table.Column<DateTimeOffset>(nullable: true), UpdatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_player_game_stats", x => new { x.UserId, x.GameSlug }));
        migrationBuilder.CreateIndex(name: "IX_player_game_stats_GameSlug_Rating_Wins", schema: "statistics", table: "player_game_stats", columns: new[] { "GameSlug", "Rating", "Wins" });
        migrationBuilder.CreateIndex(name: "IX_player_game_stats_GameSlug_LastPlayedAtUtc", schema: "statistics", table: "player_game_stats", columns: new[] { "GameSlug", "LastPlayedAtUtc" });

        migrationBuilder.CreateTable(
            name: "processed_matches", schema: "statistics",
            columns: table => new
            {
                MatchId = table.Column<Guid>(nullable: false), EventId = table.Column<Guid>(nullable: false), GameSlug = table.Column<string>(maxLength: 40, nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(nullable: false), ProcessedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_processed_matches", x => x.MatchId));
        migrationBuilder.CreateIndex(name: "IX_processed_matches_EventId", schema: "statistics", table: "processed_matches", column: "EventId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "processed_matches", schema: "statistics");
        migrationBuilder.DropTable(name: "player_game_stats", schema: "statistics");
    }
}

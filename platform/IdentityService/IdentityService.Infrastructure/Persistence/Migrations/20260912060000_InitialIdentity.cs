using IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260912060000_InitialIdentity")]
public sealed class InitialIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "identity");

        migrationBuilder.CreateTable(name: "permissions", schema: "identity", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false),
            Name = table.Column<string>(maxLength: 100, nullable: false),
            Description = table.Column<string>(maxLength: 200, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_permissions", x => x.Id));

        migrationBuilder.CreateTable(name: "roles", schema: "identity", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false),
            Name = table.Column<string>(maxLength: 64, nullable: false),
            NormalizedName = table.Column<string>(maxLength: 64, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_roles", x => x.Id));

        migrationBuilder.CreateTable(name: "users", schema: "identity", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false),
            Username = table.Column<string>(maxLength: 32, nullable: false),
            NormalizedUsername = table.Column<string>(maxLength: 32, nullable: false),
            DisplayName = table.Column<string>(maxLength: 50, nullable: false),
            PasswordHash = table.Column<string>(maxLength: 512, nullable: false),
            IsActive = table.Column<bool>(nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
            LastLoginAtUtc = table.Column<DateTimeOffset>(nullable: true)
        }, constraints: table => table.PrimaryKey("PK_users", x => x.Id));

        migrationBuilder.CreateTable(name: "role_permissions", schema: "identity", columns: table => new
        {
            RoleId = table.Column<Guid>(nullable: false),
            PermissionId = table.Column<Guid>(nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.PermissionId });
            table.ForeignKey(
                name: "FK_role_permissions_permissions_PermissionId",
                column: x => x.PermissionId,
                principalTable: "permissions",
                principalSchema: "identity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                name: "FK_role_permissions_roles_RoleId",
                column: x => x.RoleId,
                principalTable: "roles",
                principalSchema: "identity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable(name: "refresh_tokens", schema: "identity", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false),
            UserId = table.Column<Guid>(nullable: false),
            TokenHash = table.Column<string>(maxLength: 128, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
            ExpiresAtUtc = table.Column<DateTimeOffset>(nullable: false),
            RevokedAtUtc = table.Column<DateTimeOffset>(nullable: true),
            ReplacedByTokenHash = table.Column<string>(maxLength: 128, nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_refresh_tokens", x => x.Id);
            table.ForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                column: x => x.UserId,
                principalTable: "users",
                principalSchema: "identity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable(name: "user_roles", schema: "identity", columns: table => new
        {
            UserId = table.Column<Guid>(nullable: false),
            RoleId = table.Column<Guid>(nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
            table.ForeignKey(
                name: "FK_user_roles_roles_RoleId",
                column: x => x.RoleId,
                principalTable: "roles",
                principalSchema: "identity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                name: "FK_user_roles_users_UserId",
                column: x => x.UserId,
                principalTable: "users",
                principalSchema: "identity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("IX_permissions_Name", "permissions", "Name", schema: "identity", unique: true);
        migrationBuilder.CreateIndex("IX_roles_NormalizedName", "roles", "NormalizedName", schema: "identity", unique: true);
        migrationBuilder.CreateIndex("IX_users_NormalizedUsername", "users", "NormalizedUsername", schema: "identity", unique: true);
        migrationBuilder.CreateIndex("IX_refresh_tokens_TokenHash", "refresh_tokens", "TokenHash", schema: "identity", unique: true);
        migrationBuilder.CreateIndex("IX_refresh_tokens_UserId", "refresh_tokens", "UserId", schema: "identity");
        migrationBuilder.CreateIndex("IX_role_permissions_PermissionId", "role_permissions", "PermissionId", schema: "identity");
        migrationBuilder.CreateIndex("IX_user_roles_RoleId", "user_roles", "RoleId", schema: "identity");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("refresh_tokens", "identity");
        migrationBuilder.DropTable("role_permissions", "identity");
        migrationBuilder.DropTable("user_roles", "identity");
        migrationBuilder.DropTable("permissions", "identity");
        migrationBuilder.DropTable("roles", "identity");
        migrationBuilder.DropTable("users", "identity");
    }
}

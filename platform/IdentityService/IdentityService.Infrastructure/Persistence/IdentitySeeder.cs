using IdentityService.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Persistence;

public sealed class IdentitySeeder(IdentityDbContext db)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var definitions = new[]
        {
            (Name: "game.play", Description: "Join and play games"),
            (Name: "room.create", Description: "Create game rooms"),
            (Name: "profile.read", Description: "Read own player profile"),
            (Name: "admin.access", Description: "Access administration functions")
        };

        var permissions = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            var permission = await db.Permissions.SingleOrDefaultAsync(x => x.Name == definition.Name, cancellationToken);
            if (permission is null)
            {
                permission = new Permission(Guid.NewGuid(), definition.Name, definition.Description);
                db.Permissions.Add(permission);
            }
            permissions[definition.Name] = permission;
        }
        await db.SaveChangesAsync(cancellationToken);

        var player = await EnsureRoleAsync("Player", cancellationToken);
        await EnsureGrantAsync(player.Id, permissions["game.play"].Id, cancellationToken);
        await EnsureGrantAsync(player.Id, permissions["room.create"].Id, cancellationToken);
        await EnsureGrantAsync(player.Id, permissions["profile.read"].Id, cancellationToken);

        var admin = await EnsureRoleAsync("Admin", cancellationToken);
        foreach (var permission in permissions.Values)
            await EnsureGrantAsync(admin.Id, permission.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> EnsureRoleAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        var role = await db.Roles.SingleOrDefaultAsync(x => x.NormalizedName == normalized, cancellationToken);
        if (role is not null) return role;
        role = new Role(Guid.NewGuid(), name);
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return role;
    }

    private async Task EnsureGrantAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        if (!await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, cancellationToken))
            db.RolePermissions.Add(new RolePermission(roleId, permissionId));
    }
}

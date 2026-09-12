using IdentityService.Application.Abstractions;
using IdentityService.Domain.Authentication;
using IdentityService.Domain.Authorization;
using IdentityService.Domain.Users;
using IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Repositories;

public sealed class IdentityRepository(IdentityDbContext db) : IIdentityRepository
{
    public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(x => x.NormalizedUsername == normalizedUsername, cancellationToken);

    public Task<User?> GetUserByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(x => x.NormalizedUsername == normalizedUsername, cancellationToken);

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<Role?> GetRoleAsync(string normalizedName, CancellationToken cancellationToken) =>
        db.Roles.SingleOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);

    public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task<(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions)> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await (
            from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select role.Name).Distinct().ToListAsync(cancellationToken);

        var permissions = await (
            from userRole in db.UserRoles
            join rolePermission in db.RolePermissions on userRole.RoleId equals rolePermission.RoleId
            join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId
            select permission.Name).Distinct().ToListAsync(cancellationToken);

        return (roles, permissions);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken) => db.Users.AddAsync(user, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

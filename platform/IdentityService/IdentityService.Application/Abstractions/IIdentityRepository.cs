using IdentityService.Domain.Authentication;
using IdentityService.Domain.Authorization;
using IdentityService.Domain.Users;

namespace IdentityService.Application.Abstractions;

public interface IIdentityRepository
{
    Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken cancellationToken);
    Task<User?> GetUserByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Role?> GetRoleAsync(string normalizedName, CancellationToken cancellationToken);
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions)> GetAuthorizationAsync(Guid userId, CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

using IdentityService.Domain.Authorization;
using IdentityService.Domain.Authentication;

namespace IdentityService.Domain.Users;

public sealed class User
{
    private readonly List<UserRole> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User() { }

    public User(Guid id, string username, string normalizedUsername, string displayName, string passwordHash)
    {
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public IReadOnlyCollection<UserRole> Roles => _roles;
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens;

    public void MarkLoggedIn() => LastLoginAtUtc = DateTimeOffset.UtcNow;

    public void AssignRole(Guid roleId)
    {
        if (_roles.All(x => x.RoleId != roleId)) _roles.Add(new UserRole(Id, roleId));
    }

    public void AddRefreshToken(RefreshToken token) => _refreshTokens.Add(token);
}

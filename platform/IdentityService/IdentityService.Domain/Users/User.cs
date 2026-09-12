namespace IdentityService.Domain.Users;

public sealed class User
{
    private User() { }
    public User(Guid id, string username, string displayName)
    {
        Id = id;
        Username = username;
        DisplayName = displayName;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

namespace IdentityService.Domain.Authorization;

public sealed class Role
{
    private readonly List<RolePermission> _permissions = [];

    private Role() { }
    public Role(Guid id, string name)
    {
        Id = id;
        Name = name.Trim();
        NormalizedName = name.Trim().ToLowerInvariant();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public void Grant(Guid permissionId)
    {
        if (_permissions.All(x => x.PermissionId != permissionId))
            _permissions.Add(new RolePermission(Id, permissionId));
    }
}

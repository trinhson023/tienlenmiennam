namespace IdentityService.Domain.Authorization;

public sealed class Permission
{
    private Permission() { }
    public Permission(Guid id, string name, string description)
    {
        Id = id;
        Name = name.Trim();
        Description = description.Trim();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
}

using LobbyService.Domain.Rooms;

namespace LobbyService.Domain.Games;

public sealed class GameDefinition
{
    private GameDefinition() { }

    public GameDefinition(
        Guid id,
        GameType type,
        string slug,
        string displayName,
        string icon,
        int minPlayers,
        int maxPlayers,
        bool isEnabled)
    {
        Apply(type, slug, displayName, icon, minPlayers, maxPlayers, isEnabled);
        Id = id;
    }

    public Guid Id { get; private set; }
    public GameType Type { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public int MinPlayers { get; private set; }
    public int MaxPlayers { get; private set; }
    public bool IsEnabled { get; private set; }

    public void SyncCatalog(
        GameType type,
        string displayName,
        string icon,
        int minPlayers,
        int maxPlayers,
        bool isEnabled)
    {
        Apply(type, Slug, displayName, icon, minPlayers, maxPlayers, isEnabled);
    }

    private void Apply(
        GameType type,
        string slug,
        string displayName,
        string icon,
        int minPlayers,
        int maxPlayers,
        bool isEnabled)
    {
        if (minPlayers < 1 || maxPlayers < minPlayers)
            throw new ArgumentOutOfRangeException(nameof(maxPlayers));

        Type = type;
        Slug = slug.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        Icon = icon.Trim();
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
        IsEnabled = isEnabled;
    }
}

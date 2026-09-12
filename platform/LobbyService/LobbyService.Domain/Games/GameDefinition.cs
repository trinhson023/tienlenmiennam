using LobbyService.Domain.Rooms;

namespace LobbyService.Domain.Games;

public sealed class GameDefinition
{
    private GameDefinition() { }

    public GameDefinition(Guid id, GameType type, string slug, string displayName, string icon, int minPlayers, int maxPlayers, bool isEnabled)
    {
        if (minPlayers < 1 || maxPlayers < minPlayers) throw new ArgumentOutOfRangeException(nameof(maxPlayers));
        Id = id;
        Type = type;
        Slug = slug.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        Icon = icon.Trim();
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
        IsEnabled = isEnabled;
    }

    public Guid Id { get; private set; }
    public GameType Type { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public int MinPlayers { get; private set; }
    public int MaxPlayers { get; private set; }
    public bool IsEnabled { get; private set; }
}

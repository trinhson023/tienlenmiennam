namespace SocialService.Domain;

public static class ReactionCatalog
{
    public static bool TryResolve(string? value, out string type, out string emoji)
    {
        switch ((value ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "bomb": type = "bomb"; emoji = "💣"; return true;
            case "tomato": type = "tomato"; emoji = "🍅"; return true;
            case "poop": type = "poop"; emoji = "💩"; return true;
            default: type = string.Empty; emoji = string.Empty; return false;
        }
    }
}

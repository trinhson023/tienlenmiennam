using System.Text.RegularExpressions;

namespace SocialService.Domain;

public static class SocialText
{
    public static string Normalize(string? value, int maxLength)
    {
        if (maxLength <= 0) return string.Empty;
        var clean = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
        return clean.Length <= maxLength ? clean : clean[..maxLength];
    }
}

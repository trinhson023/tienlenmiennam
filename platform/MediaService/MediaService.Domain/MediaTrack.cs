namespace MediaService.Domain;

public sealed record MediaTrack(string VideoId, string Title, string ChannelTitle, string Thumbnail)
{
    public static MediaTrack? Create(string? videoId, string? title, string? channelTitle, string? thumbnail)
    {
        var id = (videoId ?? string.Empty).Trim();
        if (id.Length != 11 || id.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '_' or '-'))) return null;
        return new MediaTrack(id, Clean(title, 160, "YouTube video"), Clean(channelTitle, 100, string.Empty), Clean(thumbnail, 500, string.Empty));
    }

    private static string Clean(string? value, int max, string fallback)
    {
        var clean = (value ?? string.Empty).Trim();
        if (clean.Length == 0) clean = fallback;
        return clean.Length <= max ? clean : clean[..max];
    }
}

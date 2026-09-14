using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MediaService.Application;

namespace MediaService.Infrastructure;

public sealed class YouTubeSearchClient(HttpClient client, string apiKey) : IYouTubeSearch
{
    public async Task<IReadOnlyList<MediaSearchResult>> SearchAsync(string query, int maxResults, bool shortOnly, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Chưa cấu hình YOUTUBE_API_KEY.");
        var effectiveQuery = shortOnly ? $"{query} #shorts" : query;
        var url = "https://www.googleapis.com/youtube/v3/search?part=snippet&type=video" +
            $"&maxResults={Math.Clamp(maxResults, 1, 25)}&q={Uri.EscapeDataString(effectiveQuery)}" +
            "&videoEmbeddable=true&videoSyndicated=true&safeSearch=moderate" +
            (shortOnly ? "&videoDuration=short" : string.Empty) + $"&key={Uri.EscapeDataString(apiKey)}";
        var data = await client.GetFromJsonAsync<SearchResponse>(url, ct) ?? new SearchResponse();
        return data.Items.Where(x => !string.IsNullOrWhiteSpace(x.Id?.VideoId)).Select(x => new MediaSearchResult(
            x.Id!.VideoId!, x.Snippet?.Title ?? "YouTube video", x.Snippet?.ChannelTitle ?? string.Empty,
            x.Snippet?.Thumbnails?.Medium?.Url ?? x.Snippet?.Thumbnails?.Default?.Url ?? string.Empty)).ToArray();
    }

    private sealed class SearchResponse { [JsonPropertyName("items")] public List<SearchItem> Items { get; set; } = []; }
    private sealed class SearchItem { [JsonPropertyName("id")] public SearchId? Id { get; set; } [JsonPropertyName("snippet")] public Snippet? Snippet { get; set; } }
    private sealed class SearchId { [JsonPropertyName("videoId")] public string? VideoId { get; set; } }
    private sealed class Snippet { [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("channelTitle")] public string? ChannelTitle { get; set; } [JsonPropertyName("thumbnails")] public Thumbnails? Thumbnails { get; set; } }
    private sealed class Thumbnails { [JsonPropertyName("default")] public Thumb? Default { get; set; } [JsonPropertyName("medium")] public Thumb? Medium { get; set; } }
    private sealed class Thumb { [JsonPropertyName("url")] public string? Url { get; set; } }
}

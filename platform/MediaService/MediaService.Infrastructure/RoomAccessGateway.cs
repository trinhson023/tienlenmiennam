using System.Net;
using System.Net.Http.Json;
using MediaService.Application;

namespace MediaService.Infrastructure;

public sealed class RoomAccessGateway(HttpClient client, string internalApiKey) : IRoomMediaAccessGateway
{
    public async Task<RoomMediaContext?> GetRoomAsync(Guid roomId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/rooms/{roomId}/media-context");
        request.Headers.Add("X-Internal-Key", internalApiKey);
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RoomResponse>(cancellationToken: ct);
        return payload is null ? null : new RoomMediaContext(payload.Id, payload.HostUserId, payload.Status, payload.Members.Select(x => new RoomMediaMember(x.UserId, x.DisplayName, x.IsBot)).ToArray());
    }

    private sealed record RoomResponse(Guid Id, Guid HostUserId, string Status, IReadOnlyList<MemberResponse> Members);
    private sealed record MemberResponse(Guid UserId, string DisplayName, bool IsBot);
}

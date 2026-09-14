using System.Net;
using System.Net.Http.Json;
using SocialService.Application;

namespace SocialService.Infrastructure;

public sealed class LobbyRoomAccessGateway(HttpClient httpClient, string internalApiKey) : IRoomAccessGateway
{
    public async Task<RoomSocialContext?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/rooms/{roomId}/social-context");
        request.Headers.Add("X-Internal-Key", internalApiKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LobbyRoomContextResponse>(cancellationToken: cancellationToken);
        return payload is null
            ? null
            : new RoomSocialContext(payload.Id, payload.Status, payload.Members.Select(x => new RoomSocialMember(x.UserId, x.Username, x.DisplayName, x.IsBot)).ToArray());
    }

    private sealed record LobbyRoomContextResponse(Guid Id, string Status, IReadOnlyList<LobbyRoomMemberResponse> Members);
    private sealed record LobbyRoomMemberResponse(Guid UserId, string Username, string DisplayName, bool IsBot);
}

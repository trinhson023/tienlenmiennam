using System.Net.Http.Json;
using LobbyService.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace LobbyService.Infrastructure.Integration;

public sealed class TienLenMatchLauncher(HttpClient httpClient, IConfiguration configuration) : IMatchLauncher
{
    public async Task<MatchLaunchResult> StartAsync(string gameSlug, Guid roomId, IReadOnlyCollection<MatchLaunchPlayer> players, CancellationToken cancellationToken)
    {
        if (!string.Equals(gameSlug, "tien-len", StringComparison.OrdinalIgnoreCase))
            return MatchLaunchResult.Failure("game_not_supported", "Game service này chưa được kết nối.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/matches");
        request.Headers.Add("X-Internal-Key", configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me");
        request.Content = JsonContent.Create(new
        {
            roomId,
            players = players.Select(x => new { x.UserId, x.SeatNumber, x.Username, x.DisplayName }).ToArray()
        });

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<MatchLaunchResponse>(cancellationToken: cancellationToken);
            if (!response.IsSuccessStatusCode || payload is null || !payload.IsSuccess || payload.MatchId is null)
                return MatchLaunchResult.Failure(payload?.ErrorCode ?? "game_service_error", payload?.ErrorMessage ?? "Tiến Lên Service từ chối tạo match.");
            return MatchLaunchResult.Success(payload.MatchId.Value);
        }
        catch (HttpRequestException ex)
        {
            return MatchLaunchResult.Failure("game_service_unavailable", ex.Message);
        }
    }

    private sealed record MatchLaunchResponse(bool IsSuccess, Guid? MatchId, string? ErrorCode, string? ErrorMessage);
}

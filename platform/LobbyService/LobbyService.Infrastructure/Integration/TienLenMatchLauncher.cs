using System.Net.Http.Json;
using LobbyService.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace LobbyService.Infrastructure.Integration;

public sealed class GameMatchLauncher(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IMatchLauncher
{
    public Task<MatchLaunchResult> StartAsync(
        string gameSlug,
        Guid roomId,
        IReadOnlyCollection<MatchLaunchPlayer> players,
        CancellationToken cancellationToken) =>
        SendAsync(gameSlug, "/api/internal/matches", roomId, players, cancellationToken);

    public Task<MatchLaunchResult> RematchAsync(
        string gameSlug,
        Guid previousMatchId,
        Guid roomId,
        IReadOnlyCollection<MatchLaunchPlayer> players,
        CancellationToken cancellationToken) =>
        SendAsync(gameSlug, $"/api/internal/matches/{previousMatchId}/rematch", roomId, players, cancellationToken);

    public async Task<MatchSummaryResult> GetSummaryAsync(string gameSlug, Guid matchId, CancellationToken cancellationToken)
    {
        var client = ResolveClient(gameSlug);
        if (client is null)
            return MatchSummaryResult.Failure("game_not_supported", "Game service này chưa được kết nối.");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/matches/{matchId}/summary");
        AddInternalKey(request);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return MatchSummaryResult.Failure("match_not_found", "Không tìm thấy ván chơi.");
            if (!response.IsSuccessStatusCode)
                return MatchSummaryResult.Failure("game_service_error", "Không đọc được trạng thái ván chơi.");

            var payload = await response.Content.ReadFromJsonAsync<MatchSummaryResponse>(cancellationToken: cancellationToken);
            return payload is null
                ? MatchSummaryResult.Failure("game_service_error", "Game service trả dữ liệu không hợp lệ.")
                : MatchSummaryResult.Success(payload.MatchId, payload.RoomId, payload.Status);
        }
        catch (HttpRequestException ex)
        {
            return MatchSummaryResult.Failure("game_service_unavailable", ex.Message);
        }
    }

    public async Task<MatchBridgeResult> AbandonAsync(string gameSlug, Guid matchId, Guid userId, CancellationToken cancellationToken)
    {
        var client = ResolveClient(gameSlug);
        if (client is null)
            return MatchBridgeResult.Failure("game_not_supported", "Game service này chưa được kết nối.");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/internal/matches/{matchId}/players/{userId}/abandon");
        AddInternalKey(request);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) return MatchBridgeResult.Success();

            var payload = await response.Content.ReadFromJsonAsync<MatchBridgeResponse>(cancellationToken: cancellationToken);
            return MatchBridgeResult.Failure(
                payload?.ErrorCode ?? "game_service_error",
                payload?.ErrorMessage ?? "Không thể rời ván hiện tại.");
        }
        catch (HttpRequestException ex)
        {
            return MatchBridgeResult.Failure("game_service_unavailable", ex.Message);
        }
    }

    private async Task<MatchLaunchResult> SendAsync(
        string gameSlug,
        string path,
        Guid roomId,
        IReadOnlyCollection<MatchLaunchPlayer> players,
        CancellationToken cancellationToken)
    {
        var client = ResolveClient(gameSlug);
        if (client is null)
            return MatchLaunchResult.Failure("game_not_supported", "Game service này chưa được kết nối.");

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        AddInternalKey(request);
        request.Content = JsonContent.Create(new
        {
            roomId,
            players = players.Select(x => new
            {
                x.UserId,
                x.SeatNumber,
                x.Username,
                x.DisplayName,
                x.IsBot
            }).ToArray()
        });

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<MatchLaunchResponse>(cancellationToken: cancellationToken);
            if (!response.IsSuccessStatusCode || payload is null || !payload.IsSuccess || payload.MatchId is null)
                return MatchLaunchResult.Failure(
                    payload?.ErrorCode ?? "game_service_error",
                    payload?.ErrorMessage ?? "Game service từ chối tạo match.");

            return MatchLaunchResult.Success(payload.MatchId.Value);
        }
        catch (HttpRequestException ex)
        {
            return MatchLaunchResult.Failure("game_service_unavailable", ex.Message);
        }
    }

    private HttpClient? ResolveClient(string gameSlug) => gameSlug.Trim().ToLowerInvariant() switch
    {
        "tien-len" => httpClientFactory.CreateClient("tienlen-match"),
        "sam-loc" => httpClientFactory.CreateClient("samloc-match"),
        _ => null
    };

    private void AddInternalKey(HttpRequestMessage request) =>
        request.Headers.Add("X-Internal-Key", configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me");

    private sealed record MatchLaunchResponse(bool IsSuccess, Guid? MatchId, string? ErrorCode, string? ErrorMessage);
    private sealed record MatchSummaryResponse(Guid MatchId, Guid RoomId, string Status);
    private sealed record MatchBridgeResponse(bool IsSuccess, string? ErrorCode, string? ErrorMessage);
}

using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TienLenService.Application.Matches;

namespace TienLenService.Api.Hubs;

[Authorize]
public sealed class TienLenHub(TienLenMatchApplicationService matches, MatchBroadcaster broadcaster, SocialThrottle socialThrottle) : Hub
{
    public async Task JoinMatch(Guid matchId)
    {
        var userId = GetUserId(); var state = await matches.GetStateAsync(matchId, userId, Context.ConnectionAborted); if (!state.IsSuccess || state.State is null) throw new HubException(state.ErrorMessage ?? "Không vào được ván."); await Groups.AddToGroupAsync(Context.ConnectionId, MatchBroadcaster.UserGroup(matchId, userId), Context.ConnectionAborted); await Clients.Caller.SendAsync("MatchStateUpdated", state.State, Context.ConnectionAborted);
    }
    public Task LeaveMatch(Guid matchId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, MatchBroadcaster.UserGroup(matchId, GetUserId()), Context.ConnectionAborted);
    public async Task PlayCards(Guid matchId, string[] cardCodes, long expectedVersion) { var result = await matches.PlayCardsAsync(matchId, GetUserId(), cardCodes, expectedVersion, Context.ConnectionAborted); if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}"); await broadcaster.BroadcastMatchAsync(matchId, Context.ConnectionAborted); }
    public async Task Pass(Guid matchId, long expectedVersion) { var result = await matches.PassAsync(matchId, GetUserId(), expectedVersion, Context.ConnectionAborted); if (!result.IsSuccess) throw new HubException($"{result.ErrorCode}: {result.ErrorMessage}"); await broadcaster.BroadcastMatchAsync(matchId, Context.ConnectionAborted); }

    public async Task SendQuickChat(Guid matchId, string text)
    {
        var userId = GetUserId(); EnforceSocialThrottle(userId); var state = await RequireStateAsync(matchId, userId); var clean = Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim(); if (clean.Length > 80) clean = clean[..80]; if (string.IsNullOrWhiteSpace(clean)) throw new HubException("Tin nhắn trống."); var sender = state.Players.Single(x => x.UserId == userId); var message = new MatchQuickChatEvent(Guid.NewGuid(), userId, sender.DisplayName, clean, DateTimeOffset.UtcNow); await broadcaster.BroadcastQuickChatAsync(matchId, message, Context.ConnectionAborted);
    }

    public async Task ThrowReaction(Guid matchId, string reactionType, Guid targetUserId)
    {
        var userId = GetUserId(); EnforceSocialThrottle(userId); if (targetUserId == userId) throw new HubException("Không thể tự ném chính mình."); var state = await RequireStateAsync(matchId, userId); if (!state.Players.Any(x => x.UserId == targetUserId)) throw new HubException("Mục tiêu không thuộc ván này."); var (type, emoji) = (reactionType ?? string.Empty).Trim().ToLowerInvariant() switch { "bomb" => ("bomb", "💣"), "tomato" => ("tomato", "🍅"), "poop" => ("poop", "💩"), _ => throw new HubException("Vật phẩm không hợp lệ.") }; var sender = state.Players.Single(x => x.UserId == userId); var reaction = new MatchThrowEvent(Guid.NewGuid(), userId, sender.DisplayName, targetUserId, type, emoji, DateTimeOffset.UtcNow); await broadcaster.BroadcastThrowAsync(matchId, reaction, Context.ConnectionAborted);
    }

    private async Task<MatchStateView> RequireStateAsync(Guid matchId, Guid userId) { var state = await matches.GetStateAsync(matchId, userId, Context.ConnectionAborted); if (!state.IsSuccess || state.State is null) throw new HubException(state.ErrorMessage ?? "Bạn không thuộc ván này."); return state.State; }
    private void EnforceSocialThrottle(Guid userId) { if (!socialThrottle.TryAcquire(userId)) throw new HubException("Thao tác quá nhanh, thử lại sau một chút."); }
    private Guid GetUserId() { var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier); if (!Guid.TryParse(value, out var userId)) throw new HubException("Unauthorized user."); return userId; }
}

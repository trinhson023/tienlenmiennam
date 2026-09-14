namespace RoyalGame.Contracts;

public sealed record MatchCompleted(Guid EventId, Guid MatchId, Guid RoomId, string GameSlug, DateTimeOffset CompletedAtUtc, MatchCompletedPlayer[] Players);
public sealed record MatchCompletedPlayer(Guid UserId, string Username, string DisplayName, int SeatNumber, bool IsBot, int FinishPosition);

using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Matches;

public sealed record SamMatchPlayerSnapshot(Guid UserId, int SeatNumber, Card[] Hand, bool HasPlayedAny);

public sealed record SamMatchSnapshot(
    Guid MatchId,
    SamMatchStatus Status,
    SamMatchPlayerSnapshot[] Players,
    Guid? CurrentPlayerId,
    Card[] CenterCards,
    Guid? LastPlayBy,
    Guid[] ActivePlayerIds,
    Guid? SamDeclarerId,
    bool SamWasBlocked,
    bool SamSucceeded,
    Guid? WinnerId);

using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Matches;

public sealed record PlayerSetup(PlayerId PlayerId, SeatNumber Seat, IReadOnlyCollection<Card> Hand);

using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Matches;

public static class OpeningRules
{
    private static readonly Card ThreeOfSpades = new(Rank.Three, Suit.Spades);

    public static SeatNumber DetermineFirstSeat(IReadOnlyCollection<MatchPlayer> players)
    {
        if (players.Count == 0) throw new ArgumentException("At least one player is required.", nameof(players));

        var holder = players.SingleOrDefault(player => player.Hand.Contains(ThreeOfSpades));
        if (holder is not null) return holder.Seat;

        return players
            .SelectMany(player => player.Hand.Select(card => new { player.Seat, Card = card }))
            .OrderBy(x => x.Card, CardComparer.Instance)
            .First().Seat;
    }
}

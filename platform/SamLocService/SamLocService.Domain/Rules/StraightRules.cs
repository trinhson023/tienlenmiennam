using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Rules;

public static class StraightRules
{
    // Sâm Lốc straight order is special: A-2-3 is the smallest straight,
    // then 2-3-4, 3-4-5 ... and Q-K-A is the largest 3-card straight.
    // K-A-2 is not a legal wrap-around straight.
    private static readonly Rank[] Sequence =
    [
        Rank.Ace, Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six, Rank.Seven,
        Rank.Eight, Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King, Rank.Ace
    ];

    public static bool TryGetStrength(IReadOnlyCollection<Card> cards, out int strength)
    {
        strength = -1;
        if (cards.Count < 3) return false;
        var ranks = cards.Select(x => x.Rank).Distinct().ToArray();
        if (ranks.Length != cards.Count) return false;

        for (var start = 0; start + cards.Count <= Sequence.Length; start++)
        {
            var window = Sequence.Skip(start).Take(cards.Count).ToArray();
            if (window.Distinct().Count() != window.Length) continue;
            if (window.All(ranks.Contains))
            {
                strength = start;
                return true;
            }
        }

        return false;
    }
}

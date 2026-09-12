using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Rules;

public static class CombinationDetector
{
    public static CombinationType Detect(IReadOnlyCollection<Card> cards)
    {
        if (cards.Count == 0) return CombinationType.Invalid;
        if (cards.Count == 1) return CombinationType.Single;

        var ordered = cards.OrderBy(c => (int)c.Rank).ThenBy(c => (int)c.Suit).ToArray();
        var groups = ordered.GroupBy(c => c.Rank).ToArray();

        if (cards.Count == 2 && groups.Length == 1) return CombinationType.Pair;
        if (cards.Count == 3 && groups.Length == 1) return CombinationType.Triple;
        if (cards.Count == 4 && groups.Length == 1) return CombinationType.FourOfAKind;

        if (cards.Count >= 3 && groups.Length == cards.Count && ordered.All(c => c.Rank != Rank.Two))
        {
            var ranks = ordered.Select(c => (int)c.Rank).ToArray();
            if (ranks.Zip(ranks.Skip(1), (a, b) => b - a).All(diff => diff == 1)) return CombinationType.Straight;
        }

        if (cards.Count is 6 or 8 && groups.All(g => g.Count() == 2))
        {
            var ranks = groups.Select(g => (int)g.Key).OrderBy(x => x).ToArray();
            if (ranks.All(x => x != (int)Rank.Two) && ranks.Zip(ranks.Skip(1), (a, b) => b - a).All(diff => diff == 1))
                return cards.Count == 6 ? CombinationType.ThreeConsecutivePairs : CombinationType.FourConsecutivePairs;
        }

        return CombinationType.Invalid;
    }
}

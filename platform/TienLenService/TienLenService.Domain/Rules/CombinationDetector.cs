using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Rules;

public static class CombinationDetector
{
    public static CombinationType Detect(IReadOnlyCollection<Card> cards) => TryCreate(cards)?.Type ?? CombinationType.Invalid;

    public static Combination? TryCreate(IReadOnlyCollection<Card>? cards)
    {
        if (cards is null || cards.Count == 0) return null;

        var ordered = cards.OrderBy(c => c, CardComparer.Instance).ToArray();
        if (ordered.Distinct().Count() != ordered.Length) return null;
        var groups = ordered.GroupBy(c => c.Rank).OrderBy(g => (int)g.Key).ToArray();

        if (ordered.Length == 1) return new Combination(CombinationType.Single, ordered);
        if (ordered.Length == 2 && groups.Length == 1) return new Combination(CombinationType.Pair, ordered);
        if (ordered.Length == 3 && groups.Length == 1) return new Combination(CombinationType.Triple, ordered);

        // Preserve the legacy rules: four 2s are not a legal four-of-a-kind play.
        if (ordered.Length == 4 && groups.Length == 1 && groups[0].Key != Rank.Two)
            return new Combination(CombinationType.FourOfAKind, ordered);

        if (ordered.Length >= 3 && groups.Length == ordered.Length && ordered.All(c => c.Rank != Rank.Two))
        {
            var ranks = ordered.Select(c => (int)c.Rank).ToArray();
            if (IsConsecutive(ranks)) return new Combination(CombinationType.Straight, ordered);
        }

        if (ordered.Length is 6 or 8 && groups.All(g => g.Count() == 2))
        {
            var ranks = groups.Select(g => (int)g.Key).ToArray();
            if (ranks.All(x => x != (int)Rank.Two) && IsConsecutive(ranks))
                return new Combination(ordered.Length == 6 ? CombinationType.ThreeConsecutivePairs : CombinationType.FourConsecutivePairs, ordered);
        }

        return null;
    }

    private static bool IsConsecutive(IReadOnlyList<int> ranks) =>
        ranks.Count > 0 && ranks.Zip(ranks.Skip(1), (a, b) => b - a).All(diff => diff == 1);
}

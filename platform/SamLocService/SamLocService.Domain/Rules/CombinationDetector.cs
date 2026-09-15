using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Rules;

public static class CombinationDetector
{
    public static CombinationType Detect(IReadOnlyCollection<Card> cards) => TryCreate(cards)?.Type ?? CombinationType.Invalid;

    public static Combination? TryCreate(IReadOnlyCollection<Card>? cards)
    {
        if (cards is null || cards.Count == 0) return null;
        var ordered = cards.OrderBy(x => x, CardComparer.Instance).ToArray();
        if (ordered.Distinct().Count() != ordered.Length) return null;
        var groups = ordered.GroupBy(x => x.Rank).OrderBy(x => x.Key).ToArray();

        if (ordered.Length == 1) return new Combination(CombinationType.Single, ordered);
        if (ordered.Length == 2 && groups.Length == 1) return new Combination(CombinationType.Pair, ordered);
        if (ordered.Length == 3 && groups.Length == 1) return new Combination(CombinationType.Triple, ordered);
        if (ordered.Length == 4 && groups.Length == 1) return new Combination(CombinationType.FourOfAKind, ordered);

        if (ordered.Length >= 3 && groups.Length == ordered.Length && ordered.All(x => x.Rank != Rank.Two))
        {
            var ranks = ordered.Select(x => (int)x.Rank).ToArray();
            if (ranks.Zip(ranks.Skip(1), (a, b) => b - a).All(diff => diff == 1))
                return new Combination(CombinationType.Straight, ordered);
        }

        return null;
    }
}

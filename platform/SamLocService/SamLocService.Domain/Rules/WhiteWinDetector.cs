using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Rules;

public enum WhiteWinType
{
    None = 0,
    FivePairs = 1,
    ThreeTriples = 2,
    SameColor = 3,
    FourTwos = 4,
    DragonStraight = 5
}

public static class WhiteWinDetector
{
    public static WhiteWinType Detect(IReadOnlyCollection<Card> cards)
    {
        if (cards.Count != 10 || cards.Distinct().Count() != 10) return WhiteWinType.None;
        var groups = cards.GroupBy(x => x.Rank).ToArray();

        if (IsTenCardStraight(cards)) return WhiteWinType.DragonStraight;
        if (groups.Any(x => x.Key == Rank.Two && x.Count() == 4)) return WhiteWinType.FourTwos;
        if (IsSameColor(cards)) return WhiteWinType.SameColor;
        if (groups.Count(x => x.Count() == 3) >= 3) return WhiteWinType.ThreeTriples;
        if (groups.Length == 5 && groups.All(x => x.Count() == 2)) return WhiteWinType.FivePairs;
        return WhiteWinType.None;
    }

    private static bool IsTenCardStraight(IReadOnlyCollection<Card> cards)
    {
        if (cards.Any(x => x.Rank == Rank.Two)) return false;
        var ranks = cards.Select(x => (int)x.Rank).Distinct().OrderBy(x => x).ToArray();
        return ranks.Length == 10 && ranks.Zip(ranks.Skip(1), (a, b) => b - a).All(diff => diff == 1);
    }

    private static bool IsSameColor(IReadOnlyCollection<Card> cards)
    {
        static bool IsRed(Card card) => card.Suit is Suit.Diamonds or Suit.Hearts;
        var first = IsRed(cards.First());
        return cards.All(x => IsRed(x) == first);
    }
}

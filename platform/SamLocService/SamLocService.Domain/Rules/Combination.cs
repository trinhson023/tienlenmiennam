using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Rules;

public sealed record Combination(CombinationType Type, IReadOnlyList<Card> Cards)
{
    public int Count => Cards.Count;
    public Rank HighestRank => Cards.Max(x => x.Rank);
    public int ComparisonStrength => Type == CombinationType.Straight && StraightRules.TryGetStrength(Cards, out var strength)
        ? strength
        : (int)HighestRank;
}

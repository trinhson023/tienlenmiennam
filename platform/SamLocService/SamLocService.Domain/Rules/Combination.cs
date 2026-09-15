using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Rules;

public sealed record Combination(CombinationType Type, IReadOnlyList<Card> Cards)
{
    public int Count => Cards.Count;
    public Rank HighestRank => Cards.Max(x => x.Rank);
}

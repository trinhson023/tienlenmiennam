using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Rules;

public sealed class Combination
{
    internal Combination(CombinationType type, IEnumerable<Card> cards)
    {
        Type = type;
        Cards = cards.OrderBy(x => x, CardComparer.Instance).ToArray();
        HighestCard = Cards[^1];
    }

    public CombinationType Type { get; }
    public IReadOnlyList<Card> Cards { get; }
    public Card HighestCard { get; }
    public int Count => Cards.Count;
}

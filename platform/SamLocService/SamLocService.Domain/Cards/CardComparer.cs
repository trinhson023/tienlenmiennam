namespace SamLocService.Domain.Cards;

public sealed class CardComparer : IComparer<Card>
{
    public static CardComparer Instance { get; } = new();
    private CardComparer() { }

    public int Compare(Card x, Card y)
    {
        var rank = x.Rank.CompareTo(y.Rank);
        return rank != 0 ? rank : x.Suit.CompareTo(y.Suit);
    }
}

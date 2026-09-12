namespace TienLenService.Domain.Cards;

public sealed class CardComparer : IComparer<Card>
{
    public static CardComparer Instance { get; } = new();
    private CardComparer() { }

    public int Compare(Card x, Card y) => x.Strength.CompareTo(y.Strength);

    public static int CompareHighest(IEnumerable<Card> left, IEnumerable<Card> right)
    {
        var leftCards = left.ToArray();
        var rightCards = right.ToArray();
        if (leftCards.Length == 0 && rightCards.Length == 0) return 0;
        if (leftCards.Length == 0) return -1;
        if (rightCards.Length == 0) return 1;
        return Instance.Compare(leftCards.MaxBy(x => x.Strength), rightCards.MaxBy(x => x.Strength));
    }
}

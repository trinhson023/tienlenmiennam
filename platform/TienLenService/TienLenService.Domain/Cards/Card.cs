namespace TienLenService.Domain.Cards;

public readonly record struct Card(Rank Rank, Suit Suit)
{
    public int Strength => ((int)Rank * 4) + (int)Suit;
    public override string ToString() => $"{Rank}-{Suit}";
}

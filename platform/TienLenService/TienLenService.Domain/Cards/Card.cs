namespace TienLenService.Domain.Cards;

public readonly record struct Card(Rank Rank, Suit Suit)
{
    public int Strength => ((int)Rank * 4) + (int)Suit;
    public string Code => CardCode.Format(this);
    public override string ToString() => Code;
}

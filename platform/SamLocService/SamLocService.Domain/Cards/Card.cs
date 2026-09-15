namespace SamLocService.Domain.Cards;

public readonly record struct Card(Rank Rank, Suit Suit)
{
    public int RankStrength => (int)Rank;
    public string Code => CardCode.Format(this);
    public override string ToString() => Code;
}

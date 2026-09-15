namespace SamLocService.Domain.Cards;

public static class CardCode
{
    public static string Format(Card card) => $"{RankChar(card.Rank)}{SuitChar(card.Suit)}";

    public static Card Parse(string value)
    {
        if (!TryParse(value, out var card)) throw new FormatException($"Invalid card code '{value}'.");
        return card;
    }

    public static bool TryParse(string? value, out Card card)
    {
        card = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var code = value.Trim().ToUpperInvariant();
        if (code.Length != 2) return false;
        if (!TryRank(code[0], out var rank) || !TrySuit(code[1], out var suit)) return false;
        card = new Card(rank, suit);
        return true;
    }

    private static char RankChar(Rank rank) => rank switch
    {
        Rank.Three => '3', Rank.Four => '4', Rank.Five => '5', Rank.Six => '6', Rank.Seven => '7',
        Rank.Eight => '8', Rank.Nine => '9', Rank.Ten => 'T', Rank.Jack => 'J', Rank.Queen => 'Q',
        Rank.King => 'K', Rank.Ace => 'A', Rank.Two => '2', _ => throw new ArgumentOutOfRangeException(nameof(rank))
    };

    private static char SuitChar(Suit suit) => suit switch
    {
        Suit.Spades => 'S', Suit.Clubs => 'C', Suit.Diamonds => 'D', Suit.Hearts => 'H',
        _ => throw new ArgumentOutOfRangeException(nameof(suit))
    };

    private static bool TryRank(char value, out Rank rank)
    {
        rank = value switch
        {
            '3' => Rank.Three, '4' => Rank.Four, '5' => Rank.Five, '6' => Rank.Six, '7' => Rank.Seven,
            '8' => Rank.Eight, '9' => Rank.Nine, 'T' => Rank.Ten, 'J' => Rank.Jack, 'Q' => Rank.Queen,
            'K' => Rank.King, 'A' => Rank.Ace, '2' => Rank.Two, _ => (Rank)(-1)
        };
        return (int)rank >= 0;
    }

    private static bool TrySuit(char value, out Suit suit)
    {
        suit = value switch { 'S' => Suit.Spades, 'C' => Suit.Clubs, 'D' => Suit.Diamonds, 'H' => Suit.Hearts, _ => (Suit)(-1) };
        return (int)suit >= 0;
    }
}

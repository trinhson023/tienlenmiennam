namespace TienLenService.Domain.Cards;

public static class CardCode
{
    public static string Format(Card card) => $"{FormatRank(card.Rank)}{FormatSuit(card.Suit)}";

    public static Card Parse(string code)
    {
        if (!TryParse(code, out var card)) throw new FormatException($"Invalid card code '{code}'.");
        return card;
    }

    public static bool TryParse(string? code, out Card card)
    {
        card = default;
        if (string.IsNullOrWhiteSpace(code)) return false;
        var value = code.Trim().ToUpperInvariant();
        if (value.Length != 2) return false;

        var rank = value[0] switch
        {
            '3' => Rank.Three, '4' => Rank.Four, '5' => Rank.Five, '6' => Rank.Six,
            '7' => Rank.Seven, '8' => Rank.Eight, '9' => Rank.Nine, 'T' => Rank.Ten,
            'J' => Rank.Jack, 'Q' => Rank.Queen, 'K' => Rank.King, 'A' => Rank.Ace,
            '2' => Rank.Two, _ => (Rank?)null
        };
        var suit = value[1] switch
        {
            'S' => Suit.Spades, 'C' => Suit.Clubs, 'D' => Suit.Diamonds, 'H' => Suit.Hearts,
            _ => (Suit?)null
        };
        if (rank is null || suit is null) return false;
        card = new Card(rank.Value, suit.Value);
        return true;
    }

    private static char FormatRank(Rank rank) => rank switch
    {
        Rank.Three => '3', Rank.Four => '4', Rank.Five => '5', Rank.Six => '6',
        Rank.Seven => '7', Rank.Eight => '8', Rank.Nine => '9', Rank.Ten => 'T',
        Rank.Jack => 'J', Rank.Queen => 'Q', Rank.King => 'K', Rank.Ace => 'A', Rank.Two => '2',
        _ => throw new ArgumentOutOfRangeException(nameof(rank))
    };

    private static char FormatSuit(Suit suit) => suit switch
    {
        Suit.Spades => 'S', Suit.Clubs => 'C', Suit.Diamonds => 'D', Suit.Hearts => 'H',
        _ => throw new ArgumentOutOfRangeException(nameof(suit))
    };
}

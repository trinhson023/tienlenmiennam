namespace SamLocService.Domain.Cards;

public static class DeckFactory
{
    public static Card[] CreateStandardDeck() =>
        Enum.GetValues<Rank>().SelectMany(rank => Enum.GetValues<Suit>().Select(suit => new Card(rank, suit))).ToArray();

    public static Card[] Shuffle(Random? random = null)
    {
        random ??= Random.Shared;
        var deck = CreateStandardDeck();
        for (var i = deck.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        return deck;
    }
}

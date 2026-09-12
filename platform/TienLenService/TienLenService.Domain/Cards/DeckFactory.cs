namespace TienLenService.Domain.Cards;

public static class DeckFactory
{
    public static IReadOnlyList<Card> CreateStandardDeck()
    {
        var cards = new List<Card>(52);
        foreach (var rank in Enum.GetValues<Rank>())
        foreach (var suit in Enum.GetValues<Suit>())
            cards.Add(new Card(rank, suit));
        return cards;
    }

    public static IReadOnlyList<IReadOnlyList<Card>> DealChunked(IReadOnlyList<Card> shuffledDeck, int playerCount, int cardsPerPlayer = 13)
    {
        if (playerCount is < 2 or > 4) throw new ArgumentOutOfRangeException(nameof(playerCount));
        if (cardsPerPlayer <= 0) throw new ArgumentOutOfRangeException(nameof(cardsPerPlayer));
        if (shuffledDeck.Count < playerCount * cardsPerPlayer) throw new ArgumentException("Deck does not contain enough cards.", nameof(shuffledDeck));
        if (shuffledDeck.Distinct().Count() != shuffledDeck.Count) throw new ArgumentException("Deck contains duplicate cards.", nameof(shuffledDeck));

        return Enumerable.Range(0, playerCount)
            .Select(player => (IReadOnlyList<Card>)shuffledDeck
                .Skip(player * cardsPerPlayer)
                .Take(cardsPerPlayer)
                .OrderBy(x => x, CardComparer.Instance)
                .ToArray())
            .ToArray();
    }
}

using SamLocService.Domain.Cards;

namespace SamLocService.Domain.Matches;

public sealed class SamMatchPlayer
{
    private readonly List<Card> _hand;

    internal SamMatchPlayer(Guid userId, int seatNumber, IEnumerable<Card> hand, bool hasPlayedAny = false)
    {
        UserId = userId;
        SeatNumber = seatNumber;
        _hand = hand.OrderBy(x => x, CardComparer.Instance).ToList();
        HasPlayedAny = hasPlayedAny;
    }

    public Guid UserId { get; }
    public int SeatNumber { get; }
    public IReadOnlyList<Card> Hand => _hand;
    public bool HasPlayedAny { get; private set; }

    internal bool Owns(IEnumerable<Card> cards) => cards.All(card => _hand.Contains(card));
    internal void Play(IEnumerable<Card> cards)
    {
        foreach (var card in cards) _hand.Remove(card);
        HasPlayedAny = true;
    }
}

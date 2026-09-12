using TienLenService.Domain.Cards;

namespace TienLenService.Domain.Matches;

public sealed class MatchPlayer
{
    private readonly List<Card> _hand;

    internal MatchPlayer(PlayerSetup setup)
    {
        Id = setup.PlayerId;
        Seat = setup.Seat;
        _hand = setup.Hand.OrderBy(x => x, CardComparer.Instance).ToList();
    }

    public PlayerId Id { get; }
    public SeatNumber Seat { get; }
    public IReadOnlyList<Card> Hand => _hand;
    public bool HasFinished => FinishPosition.HasValue;
    public int? FinishPosition { get; private set; }

    internal bool HasCards(IEnumerable<Card> cards)
    {
        var working = _hand.ToList();
        foreach (var card in cards)
        {
            if (!working.Remove(card)) return false;
        }
        return true;
    }

    internal void RemoveCards(IEnumerable<Card> cards)
    {
        foreach (var card in cards)
        {
            if (!_hand.Remove(card)) throw new InvalidOperationException($"Card {card} is not in the player's hand.");
        }
    }

    internal void MarkFinished(int position)
    {
        if (FinishPosition.HasValue) return;
        FinishPosition = position;
    }
}

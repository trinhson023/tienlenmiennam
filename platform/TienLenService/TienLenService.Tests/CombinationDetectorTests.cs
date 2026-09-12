using TienLenService.Domain.Cards;
using TienLenService.Domain.Rules;

namespace TienLenService.Tests;

public class CombinationDetectorTests
{
    [Fact]
    public void DetectsPair()
    {
        var cards = new[] { new Card(Rank.Seven, Suit.Spades), new Card(Rank.Seven, Suit.Hearts) };
        Assert.Equal(CombinationType.Pair, CombinationDetector.Detect(cards));
    }

    [Fact]
    public void StraightCannotContainTwo()
    {
        var cards = new[] { new Card(Rank.King, Suit.Spades), new Card(Rank.Ace, Suit.Spades), new Card(Rank.Two, Suit.Spades) };
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(cards));
    }
}

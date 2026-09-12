using TienLenService.Domain.Cards;
using static TienLenService.Tests.TestCards;

namespace TienLenService.Tests;

public class CardOrderingTests
{
    [Fact]
    public void ThreeOfSpadesIsLowestAndTwoOfHeartsIsHighest()
    {
        var deck = DeckFactory.CreateStandardDeck().OrderBy(x => x, CardComparer.Instance).ToArray();
        Assert.Equal(C("3S"), deck.First());
        Assert.Equal(C("2H"), deck.Last());
        Assert.Equal(52, deck.Distinct().Count());
    }

    [Fact]
    public void SuitsFollowLegacyOrderSpadesClubsDiamondsHearts()
    {
        var sevens = Cs("7H", "7S", "7D", "7C").OrderBy(x => x, CardComparer.Instance).Select(x => x.Code).ToArray();
        Assert.Equal(new[] { "7S", "7C", "7D", "7H" }, sevens);
    }

    [Fact]
    public void CardCodeRoundTripsLegacyIds()
    {
        foreach (var code in new[] { "3S", "TC", "QD", "AH", "2H" })
            Assert.Equal(code, CardCode.Parse(code).Code);
    }
}

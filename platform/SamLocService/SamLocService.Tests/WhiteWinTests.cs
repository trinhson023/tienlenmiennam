using SamLocService.Domain.Cards;
using SamLocService.Domain.Rules;

namespace SamLocService.Tests;

public sealed class WhiteWinTests
{
    private static Card[] Cards(string codes) => codes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(CardCode.Parse).ToArray();

    [Fact]
    public void DetectsTenCardDragonStraight() => Assert.Equal(WhiteWinType.DragonStraight, WhiteWinDetector.Detect(Cards("3S,4C,5D,6H,7S,8C,9D,TH,JS,QC")));

    [Fact]
    public void DetectsFourTwos() => Assert.Equal(WhiteWinType.FourTwos, WhiteWinDetector.Detect(Cards("2S,2C,2D,2H,3S,4C,5D,7H,9S,JC")));

    [Fact]
    public void DetectsSameColor() => Assert.Equal(WhiteWinType.SameColor, WhiteWinDetector.Detect(Cards("3H,3D,5H,6D,8H,9D,JH,QD,KH,AD")));

    [Fact]
    public void DetectsThreeTriples() => Assert.Equal(WhiteWinType.ThreeTriples, WhiteWinDetector.Detect(Cards("3S,3C,3D,5S,5C,5D,7S,7C,7D,AH")));

    [Fact]
    public void DetectsFivePairs() => Assert.Equal(WhiteWinType.FivePairs, WhiteWinDetector.Detect(Cards("3S,3C,5S,5C,7S,7C,9S,9C,JS,JC")));
}

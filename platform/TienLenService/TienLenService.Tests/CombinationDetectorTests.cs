using TienLenService.Domain.Rules;
using static TienLenService.Tests.TestCards;

namespace TienLenService.Tests;

public class CombinationDetectorTests
{
    [Fact] public void DetectsSingle() => Assert.Equal(CombinationType.Single, CombinationDetector.Detect(Cs("3S")));
    [Fact] public void DetectsPair() => Assert.Equal(CombinationType.Pair, CombinationDetector.Detect(Cs("7S", "7H")));
    [Fact] public void DetectsTriple() => Assert.Equal(CombinationType.Triple, CombinationDetector.Detect(Cs("9S", "9C", "9H")));
    [Fact] public void DetectsFourOfAKind() => Assert.Equal(CombinationType.FourOfAKind, CombinationDetector.Detect(Cs("JS", "JC", "JD", "JH")));
    [Fact] public void DetectsStraight() => Assert.Equal(CombinationType.Straight, CombinationDetector.Detect(Cs("3S", "4H", "5C", "6D", "7S")));
    [Fact] public void DetectsThreeConsecutivePairs() => Assert.Equal(CombinationType.ThreeConsecutivePairs, CombinationDetector.Detect(Cs("4S", "4H", "5S", "5D", "6C", "6H")));
    [Fact] public void DetectsFourConsecutivePairs() => Assert.Equal(CombinationType.FourConsecutivePairs, CombinationDetector.Detect(Cs("8S", "8H", "9S", "9D", "TS", "TH", "JS", "JH")));

    [Fact]
    public void StraightCannotContainTwo() =>
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(Cs("KS", "AH", "2S")));

    [Fact]
    public void ConsecutivePairsCannotContainTwo() =>
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(Cs("QS", "QH", "KS", "KH", "AS", "AH", "2S", "2H")));

    [Fact]
    public void FourTwosRemainInvalidForLegacyParity() =>
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(Cs("2S", "2C", "2D", "2H")));

    [Fact]
    public void MalformedStraightWithRepeatedRankIsInvalid() =>
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(Cs("3S", "4S", "4H", "5S")));

    [Fact]
    public void DuplicatePhysicalCardIsInvalid() =>
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(Cs("7S", "7S")));
}

using TienLenService.Domain.Rules;
using static TienLenService.Tests.TestCards;

namespace TienLenService.Tests;

public class ChopRulesTests
{
    private static readonly string[] ThreePairs = ["4S", "4H", "5S", "5D", "6C", "6H"];
    private static readonly string[] FourPairs = ["8S", "8H", "9S", "9D", "TS", "TH", "JS", "JH"];
    private static readonly string[] Quad = ["7S", "7C", "7D", "7H"];

    [Fact] public void ThreePairsChopsSingleTwo() => Assert.True(ChopRules.CanChop(Cs("2H"), Cs(ThreePairs)));
    [Fact] public void ThreePairsDoesNotChopPairOfTwos() => Assert.False(ChopRules.CanChop(Cs("2S", "2H"), Cs(ThreePairs)));
    [Fact] public void FourOfAKindChopsSingleTwo() => Assert.True(ChopRules.CanChop(Cs("2H"), Cs(Quad)));
    [Fact] public void FourOfAKindChopsPairOfTwos() => Assert.True(ChopRules.CanChop(Cs("2S", "2H"), Cs(Quad)));
    [Fact] public void FourOfAKindChopsThreePairs() => Assert.True(ChopRules.CanChop(Cs(ThreePairs), Cs(Quad)));
    [Fact] public void FourPairsChopsSingleTwo() => Assert.True(ChopRules.CanChop(Cs("2H"), Cs(FourPairs)));
    [Fact] public void FourPairsChopsPairOfTwos() => Assert.True(ChopRules.CanChop(Cs("2S", "2H"), Cs(FourPairs)));
    [Fact] public void FourPairsChopsThreePairs() => Assert.True(ChopRules.CanChop(Cs(ThreePairs), Cs(FourPairs)));
    [Fact] public void FourPairsChopsFourOfAKind() => Assert.True(ChopRules.CanChop(Cs(Quad), Cs(FourPairs)));
    [Fact] public void ChopCombinationDoesNotCrossTypeAgainstNormalAce() => Assert.False(ChopRules.CanChop(Cs("AH"), Cs(ThreePairs)));
}

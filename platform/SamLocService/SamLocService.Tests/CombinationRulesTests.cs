using SamLocService.Domain.Cards;
using SamLocService.Domain.Rules;

namespace SamLocService.Tests;

public sealed class CombinationRulesTests
{
    private static Card[] Cards(string codes) => codes.Split(',').Select(CardCode.Parse).ToArray();

    [Fact]
    public void StandardDeck_Has52UniqueCards() => Assert.Equal(52, DeckFactory.CreateStandardDeck().Distinct().Count());

    [Theory]
    [InlineData("3S", CombinationType.Single)]
    [InlineData("7S,7H", CombinationType.Pair)]
    [InlineData("9S,9C,9H", CombinationType.Triple)]
    [InlineData("5S,5C,5D,5H", CombinationType.FourOfAKind)]
    [InlineData("AS,2H,3D", CombinationType.Straight)]
    [InlineData("2S,3H,4D", CombinationType.Straight)]
    [InlineData("3S,4H,5D", CombinationType.Straight)]
    [InlineData("QS,KH,AD", CombinationType.Straight)]
    public void Detects_CoreCombinations(string codes, CombinationType expected) =>
        Assert.Equal(expected, CombinationDetector.Detect(Cards(codes)));

    [Fact]
    public void KingAceTwo_IsNotAStraight() =>
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(Cards("KS,AH,2D")));

    [Fact]
    public void StraightOrder_A23_IsLowerThan234_AndQKAIsHigh()
    {
        var a23 = CombinationDetector.TryCreate(Cards("AS,2H,3D"))!;
        var two34 = CombinationDetector.TryCreate(Cards("2S,3H,4D"))!;
        var qka = CombinationDetector.TryCreate(Cards("QS,KH,AD"))!;
        Assert.True(PlayValidation.Validate(two34, a23, 10).IsValid);
        Assert.True(PlayValidation.Validate(qka, two34, 10).IsValid);
        Assert.False(PlayValidation.Validate(a23, qka, 10).IsValid);
    }

    [Fact]
    public void FourTwos_IsLegalFourOfAKind() =>
        Assert.Equal(CombinationType.FourOfAKind, CombinationDetector.Detect([CardCode.Parse("2S"), CardCode.Parse("2C"), CardCode.Parse("2D"), CardCode.Parse("2H")]));

    [Fact]
    public void SuitDoesNotAffectSamStrength()
    {
        var center = CombinationDetector.TryCreate([CardCode.Parse("7H")])!;
        var candidate = CombinationDetector.TryCreate([CardCode.Parse("8S")])!;
        Assert.True(PlayValidation.Validate(candidate, center, 10).IsValid);
    }

    [Fact]
    public void FourOfAKind_ChopsSingleTwo()
    {
        var center = CombinationDetector.TryCreate([CardCode.Parse("2H")])!;
        var candidate = CombinationDetector.TryCreate(Cards("5S,5C,5D,5H"))!;
        Assert.True(PlayValidation.Validate(candidate, center, 10).IsValid);
    }

    [Fact]
    public void FourOfAKind_DoesNotChopPairOfTwos()
    {
        var center = CombinationDetector.TryCreate(Cards("2S,2H"))!;
        var candidate = CombinationDetector.TryCreate(Cards("5S,5C,5D,5H"))!;
        var result = PlayValidation.Validate(candidate, center, 10);
        Assert.False(result.IsValid);
        Assert.Equal("combination_mismatch", result.ErrorCode);
    }

    [Fact]
    public void CannotFinishWithAnyPlayContainingTwo()
    {
        var candidate = CombinationDetector.TryCreate([CardCode.Parse("2S")])!;
        var result = PlayValidation.Validate(candidate, null, 1);
        Assert.False(result.IsValid);
        Assert.Equal("cannot_finish_with_two", result.ErrorCode);
    }
}

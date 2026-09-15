using SamLocService.Domain.Cards;
using SamLocService.Domain.Rules;

namespace SamLocService.Tests;

public sealed class CombinationRulesTests
{
    [Fact]
    public void StandardDeck_Has52UniqueCards() => Assert.Equal(52, DeckFactory.CreateStandardDeck().Distinct().Count());

    [Theory]
    [InlineData("3S", CombinationType.Single)]
    [InlineData("7S,7H", CombinationType.Pair)]
    [InlineData("9S,9C,9H", CombinationType.Triple)]
    [InlineData("5S,5C,5D,5H", CombinationType.FourOfAKind)]
    [InlineData("3S,4H,5D", CombinationType.Straight)]
    [InlineData("TS,JH,QD,KC,AS", CombinationType.Straight)]
    public void Detects_CoreCombinations(string codes, CombinationType expected)
    {
        var cards = codes.Split(',').Select(CardCode.Parse).ToArray();
        Assert.Equal(expected, CombinationDetector.Detect(cards));
    }

    [Theory]
    [InlineData("AS,2H,3D")]
    [InlineData("KS,AH,2D")]
    [InlineData("2S,3H,4D")]
    public void Straight_CannotContainTwo(string codes) =>
        Assert.Equal(CombinationType.Invalid, CombinationDetector.Detect(codes.Split(',').Select(CardCode.Parse).ToArray()));

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
        var candidate = CombinationDetector.TryCreate([CardCode.Parse("5S"), CardCode.Parse("5C"), CardCode.Parse("5D"), CardCode.Parse("5H")])!;
        Assert.True(PlayValidation.Validate(candidate, center, 10).IsValid);
    }

    [Fact]
    public void FourOfAKind_DoesNotChopPairOfTwos()
    {
        var center = CombinationDetector.TryCreate([CardCode.Parse("2S"), CardCode.Parse("2H")])!;
        var candidate = CombinationDetector.TryCreate([CardCode.Parse("5S"), CardCode.Parse("5C"), CardCode.Parse("5D"), CardCode.Parse("5H")])!;
        var result = PlayValidation.Validate(candidate, center, 10);
        Assert.False(result.IsValid);
        Assert.Equal("combination_mismatch", result.ErrorCode);
    }

    [Fact]
    public void HigherFourOfAKind_BeatsLowerFourOfAKind()
    {
        var center = CombinationDetector.TryCreate("6S,6C,6D,6H".Split(',').Select(CardCode.Parse).ToArray())!;
        var candidate = CombinationDetector.TryCreate("7S,7C,7D,7H".Split(',').Select(CardCode.Parse).ToArray())!;
        Assert.True(PlayValidation.Validate(candidate, center, 10).IsValid);
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

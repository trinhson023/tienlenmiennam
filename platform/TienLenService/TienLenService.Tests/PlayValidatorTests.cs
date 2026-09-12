using TienLenService.Domain.Rules;
using static TienLenService.Tests.TestCards;

namespace TienLenService.Tests;

public class PlayValidatorTests
{
    [Fact]
    public void SameTypeMustBeatCenterByHighestCard()
    {
        Assert.True(PlayValidator.Validate(Cs("8S", "8H"), Cs("7S", "7H"), false, 5).IsValid);
        Assert.Equal(PlayValidationCode.DoesNotBeatCenter, PlayValidator.Validate(Cs("6S", "6H"), Cs("7S", "7H"), false, 5).Code);
    }

    [Fact]
    public void SameRankCanBeatByHigherSuit()
    {
        var result = PlayValidator.Validate(Cs("7C"), Cs("7S"), false, 5);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void DifferentNormalCombinationDoesNotMatchCenter()
    {
        var result = PlayValidator.Validate(Cs("8S", "8H"), Cs("7H"), false, 5);
        Assert.Equal(PlayValidationCode.DoesNotMatchCenter, result.Code);
    }

    [Fact]
    public void CrossTypeChopIsAccepted()
    {
        var result = PlayValidator.Validate(Cs("7S", "7C", "7D", "7H"), Cs("2H"), false, 8);
        Assert.True(result.IsValid);
        Assert.True(result.IsChop);
    }

    [Fact]
    public void OpeningPlayMustContainThreeOfSpadesWhenRequired()
    {
        var result = PlayValidator.Validate(Cs("4S"), Array.Empty<TienLenService.Domain.Cards.Card>(), true, 13);
        Assert.Equal(PlayValidationCode.OpeningThreeOfSpadesRequired, result.Code);
    }

    [Fact]
    public void CannotFinishWithAnyPlayContainingTwo()
    {
        var result = PlayValidator.Validate(Cs("2H"), Array.Empty<TienLenService.Domain.Cards.Card>(), false, 1);
        Assert.Equal(PlayValidationCode.CannotFinishWithTwo, result.Code);
    }
}

using SamLocService.Application.Matches;
using SamLocService.Domain.Cards;
using SamLocService.Domain.Matches;

namespace SamLocService.Tests;

public sealed class LegalMoveGeneratorTests
{
    [Fact]
    public void OpeningBotAvoidsLeavingOnlyTwoWhenAlternativeExists()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var match = SamMatch.Restore(new SamMatchSnapshot(
            Guid.NewGuid(),
            SamMatchStatus.InProgress,
            [
                Player(a, 0, "3S,2H"),
                Player(b, 1, "4S,5S")
            ],
            a,
            [],
            null,
            [a, b],
            null,
            false,
            false,
            null));

        var legal = LegalMoveGenerator.FindLowestLegalPlay(match, match.Players.Single(x => x.UserId == a));

        Assert.NotNull(legal);
        Assert.Single(legal!);
        Assert.Equal(Rank.Two, legal![0].Rank);
    }

    [Fact]
    public void BotCanFindFourOfAKindChopAgainstSingleTwo()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var match = SamMatch.Restore(new SamMatchSnapshot(
            Guid.NewGuid(),
            SamMatchStatus.InProgress,
            [
                Player(a, 0, "6S,6C,6D,6H,9S"),
                Player(b, 1, "3S,4S")
            ],
            a,
            [CardCode.Parse("2S")],
            b,
            [a, b],
            null,
            false,
            false,
            null));

        var legal = LegalMoveGenerator.FindLowestLegalPlay(match, match.Players.Single(x => x.UserId == a));

        Assert.NotNull(legal);
        Assert.Equal(4, legal!.Count);
        Assert.All(legal, card => Assert.Equal(Rank.Six, card.Rank));
    }

    private static SamMatchPlayerSnapshot Player(Guid id, int seat, string cards) =>
        new(id, seat, cards.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(CardCode.Parse).ToArray(), false);
}

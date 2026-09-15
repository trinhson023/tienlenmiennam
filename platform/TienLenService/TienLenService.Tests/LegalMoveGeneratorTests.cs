using TienLenService.Application.Matches;
using TienLenService.Domain.Matches;
using static TienLenService.Tests.TestCards;

namespace TienLenService.Tests;

public sealed class LegalMoveGeneratorTests
{
    [Fact]
    public void FindsFinalTwoWhenItIsTheOnlyLegalCard()
    {
        var bot = PlayerId.New();
        var opponent = PlayerId.New();
        var match = TienLenMatch.Create(MatchId.New(), new[]
        {
            new PlayerSetup(opponent, new SeatNumber(0), Cs("3S", "4S")),
            new PlayerSetup(bot, new SeatNumber(1), Cs("2H"))
        });

        // Opening player sheds 3♠, bot can legally beat it with its final 2.
        Assert.True(match.PlayCards(opponent, Cs("3S")).IsSuccess);
        var botPlayer = match.Players.Single(x => x.Id == bot);

        var legal = LegalMoveGenerator.FindLowestLegalPlay(match, botPlayer);

        Assert.NotNull(legal);
        Assert.Equal(new[] { "2H" }, legal!.Select(x => x.Code));
        var result = match.PlayCards(bot, legal);
        Assert.True(result.IsSuccess);
        Assert.True(result.PlayerFinished);
        Assert.True(result.MatchCompleted);
    }
}

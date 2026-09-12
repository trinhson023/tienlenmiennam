using TienLenService.Domain.Matches;
using TienLenService.Domain.Rules;
using static TienLenService.Tests.TestCards;

namespace TienLenService.Tests;

public class TienLenMatchTests
{
    [Fact]
    public void ThreeOfSpadesHolderStartsAndMustIncludeIt()
    {
        var (match, a, _) = TwoPlayerMatch(Cs("3S", "4S"), Cs("5S", "6S"));
        Assert.Equal(a, match.CurrentPlayerId);

        var invalid = match.PlayCards(a, Cs("4S"));
        Assert.False(invalid.IsSuccess);
        Assert.Equal(PlayValidationCode.OpeningThreeOfSpadesRequired, invalid.ValidationCode);

        Assert.True(match.PlayCards(a, Cs("3S")).IsSuccess);
    }

    [Fact]
    public void LowestDealtCardStartsWhenThreeOfSpadesIsNotDealt()
    {
        var a = PlayerId.New();
        var b = PlayerId.New();
        var match = TienLenMatch.Create(MatchId.New(), new[]
        {
            new PlayerSetup(a, new SeatNumber(0), Cs("7S", "8S")),
            new PlayerSetup(b, new SeatNumber(1), Cs("4C", "9S"))
        });
        Assert.Equal(b, match.CurrentPlayerId);
    }

    [Fact]
    public void PassesResetTrickBackToLastPlayerWhoPlayed()
    {
        var a = PlayerId.New(); var b = PlayerId.New(); var c = PlayerId.New();
        var match = TienLenMatch.Create(MatchId.New(), new[]
        {
            new PlayerSetup(a, new SeatNumber(0), Cs("3S", "9S")),
            new PlayerSetup(b, new SeatNumber(1), Cs("4S", "TS")),
            new PlayerSetup(c, new SeatNumber(2), Cs("5S", "JS"))
        });

        Assert.True(match.PlayCards(a, Cs("3S")).IsSuccess);
        Assert.True(match.Pass(b).IsSuccess);
        var lastPass = match.Pass(c);

        Assert.True(lastPass.TrickReset);
        Assert.Empty(match.Center);
        Assert.Equal(a, match.CurrentPlayerId);
    }

    [Fact]
    public void FinishedTrickWinnerHandsOpeningToNextNonFinishedSeat()
    {
        var a = PlayerId.New(); var b = PlayerId.New(); var c = PlayerId.New();
        var match = TienLenMatch.Create(MatchId.New(), new[]
        {
            new PlayerSetup(a, new SeatNumber(0), Cs("3S")),
            new PlayerSetup(b, new SeatNumber(1), Cs("4S", "8S")),
            new PlayerSetup(c, new SeatNumber(2), Cs("5S", "9S"))
        });

        var finish = match.PlayCards(a, Cs("3S"));
        Assert.True(finish.PlayerFinished);
        Assert.Equal(b, match.CurrentPlayerId);
        Assert.True(match.Pass(b).IsSuccess);
        var reset = match.Pass(c);

        Assert.True(reset.TrickReset);
        Assert.Equal(b, match.CurrentPlayerId); // legacy "hưởng sái" after A has already finished
        Assert.Empty(match.Center);
    }

    [Fact]
    public void CannotFinishWithTwo()
    {
        var (match, a, _) = TwoPlayerMatch(Cs("3S", "2H"), Cs("4S", "5S"));
        Assert.True(match.PlayCards(a, Cs("3S")).IsSuccess);
        // B passes, A opens the new trick and attempts to go out with the 2.
        var b = match.Players.Single(x => x.Id != a).Id;
        Assert.True(match.Pass(b).IsSuccess);
        var result = match.PlayCards(a, Cs("2H"));
        Assert.Equal(PlayValidationCode.CannotFinishWithTwo, result.ValidationCode);
    }

    [Fact]
    public void TwoPlayerMatchCompletesWhenFirstPlayerFinishes()
    {
        var (match, a, b) = TwoPlayerMatch(Cs("3S"), Cs("4S", "5S"));
        var result = match.PlayCards(a, Cs("3S"));
        Assert.True(result.MatchCompleted);
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal(new[] { a, b }, match.WinnerOrder);
    }

    [Fact]
    public void ThreePlayerWinnerOrderAppendsLastRemainingPlayer()
    {
        var a = PlayerId.New(); var b = PlayerId.New(); var c = PlayerId.New();
        var match = TienLenMatch.Create(MatchId.New(), new[]
        {
            new PlayerSetup(a, new SeatNumber(0), Cs("3S")),
            new PlayerSetup(b, new SeatNumber(1), Cs("4S")),
            new PlayerSetup(c, new SeatNumber(2), Cs("5S", "6S"))
        });

        Assert.True(match.PlayCards(a, Cs("3S")).PlayerFinished);
        // A finished. With B/C still active, B can beat 3 with 4 and finish.
        var result = match.PlayCards(b, Cs("4S"));
        Assert.True(result.MatchCompleted);
        Assert.Equal(new[] { a, b, c }, match.WinnerOrder);
    }

    [Fact]
    public void FourPlayerGameDoesNotEndUntilThreeFinishingPositionsExist()
    {
        var ids = Enumerable.Range(0, 4).Select(_ => PlayerId.New()).ToArray();
        var match = TienLenMatch.Create(MatchId.New(), new[]
        {
            new PlayerSetup(ids[0], new SeatNumber(0), Cs("3S")),
            new PlayerSetup(ids[1], new SeatNumber(1), Cs("4S")),
            new PlayerSetup(ids[2], new SeatNumber(2), Cs("5S")),
            new PlayerSetup(ids[3], new SeatNumber(3), Cs("6S", "7S"))
        });

        Assert.True(match.PlayCards(ids[0], Cs("3S")).PlayerFinished);
        Assert.True(match.PlayCards(ids[1], Cs("4S")).PlayerFinished);
        var third = match.PlayCards(ids[2], Cs("5S"));
        Assert.True(third.MatchCompleted);
        Assert.Equal(new[] { ids[0], ids[1], ids[2], ids[3] }, match.WinnerOrder);
    }

    private static (TienLenMatch Match, PlayerId A, PlayerId B) TwoPlayerMatch(TienLenService.Domain.Cards.Card[] handA, TienLenService.Domain.Cards.Card[] handB)
    {
        var a = PlayerId.New();
        var b = PlayerId.New();
        var match = TienLenMatch.Create(MatchId.New(), new[]
        {
            new PlayerSetup(a, new SeatNumber(0), handA),
            new PlayerSetup(b, new SeatNumber(1), handB)
        });
        return (match, a, b);
    }
}

using SamLocService.Domain.Cards;
using SamLocService.Domain.Matches;
using SamLocService.Domain.Rules;

namespace SamLocService.Tests;

public sealed class SamMatchTests
{
    [Theory]
    [InlineData(2, 20)]
    [InlineData(3, 30)]
    [InlineData(4, 40)]
    public void Deal_GivesExactlyTenCardsPerPlayer(int count, int dealt)
    {
        var players = Enumerable.Range(0, count).Select(i => new PlayerSetup(Guid.NewGuid(), i)).ToArray();
        var match = SamMatch.Create(Guid.NewGuid(), players, DeckFactory.CreateStandardDeck());
        Assert.All(match.Players, player => Assert.Equal(10, player.Hand.Count));
        Assert.Equal(dealt, match.Players.Sum(x => x.Hand.Count));
        Assert.Equal(SamMatchStatus.Declaring, match.Status);
    }

    [Fact]
    public void FirstSamDeclaration_ClaimsOpeningTurn()
    {
        var match = NewMatch(3);
        var declarer = match.Players[2].UserId;
        Assert.True(match.DeclareSam(declarer).IsSuccess);
        Assert.Equal(declarer, match.SamDeclarerId);
        Assert.Equal(declarer, match.CurrentPlayerId);
        Assert.Equal(SamDeclarationState.Active, match.DeclarationState);
        Assert.Equal("declaration_closed", match.DeclareSam(match.Players[0].UserId).ErrorCode);
    }

    [Fact]
    public void ClosingDeclaration_UsesProvidedStarter()
    {
        var match = NewMatch(2);
        var starter = match.Players[1].UserId;
        Assert.True(match.CloseDeclaration(starter).IsSuccess);
        Assert.Equal(starter, match.CurrentPlayerId);
        Assert.Null(match.SamDeclarerId);
    }

    [Fact]
    public void Pass_RemovesPlayerForWholeTrick_ThenResetsToLastPlayer()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var snapshot = Snapshot(
            [Player(a,0,"3S,4S,5S"), Player(b,1,"6S,7S,8S"), Player(c,2,"9S,TS,JS")],
            a, [], null, [a,b,c]);
        var match = SamMatch.Restore(snapshot);
        Assert.True(match.Play(a, [CardCode.Parse("3S")]).IsSuccess);
        Assert.Equal(b, match.CurrentPlayerId);
        Assert.True(match.Pass(b).IsSuccess);
        Assert.Equal(c, match.CurrentPlayerId);
        Assert.True(match.Pass(c).IsSuccess);
        Assert.Equal(a, match.CurrentPlayerId);
        Assert.Null(match.Center);
    }

    [Fact]
    public void FirstPlayerToEmptyHand_CompletesMatch()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var snapshot = Snapshot([Player(a,0,"AS"), Player(b,1,"3S,4S")], a, [], null, [a,b]);
        var match = SamMatch.Restore(snapshot);
        Assert.True(match.Play(a, [CardCode.Parse("AS")]).IsSuccess);
        Assert.Equal(SamMatchStatus.Completed, match.Status);
        Assert.Equal(a, match.WinnerId);
    }

    [Fact]
    public void MatchRejectsFinishingWithTwo()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var match = SamMatch.Restore(Snapshot([Player(a,0,"2S"), Player(b,1,"3S,4S")], a, [], null, [a,b]));
        var result = match.Play(a, [CardCode.Parse("2S")]);
        Assert.False(result.IsSuccess);
        Assert.Equal("cannot_finish_with_two", result.ErrorCode);
        Assert.Equal(SamMatchStatus.InProgress, match.Status);
    }

    [Fact]
    public void SamDeclarer_BecomesFailedWhenOpponentBlocksTheirPlay()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var match = SamMatch.Restore(new SamMatchSnapshot(Guid.NewGuid(), SamMatchStatus.InProgress,
            [Player(a,0,"7S,9S"), Player(b,1,"8S,TS")], a, [], null, [a,b], a, false, false, null));
        Assert.True(match.Play(a, [CardCode.Parse("7S")]).IsSuccess);
        Assert.True(match.Play(b, [CardCode.Parse("8S")]).IsSuccess);
        Assert.True(match.SamWasBlocked);
        Assert.Equal(SamDeclarationState.Failed, match.DeclarationState);
    }

    [Fact]
    public void SamDeclarer_SucceedsWhenTheyFinishWithoutBeingBlocked()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var match = SamMatch.Restore(new SamMatchSnapshot(Guid.NewGuid(), SamMatchStatus.InProgress,
            [Player(a,0,"AS"), Player(b,1,"3S,4S")], a, [], null, [a,b], a, false, false, null));
        Assert.True(match.Play(a, [CardCode.Parse("AS")]).IsSuccess);
        Assert.True(match.SamSucceeded);
        Assert.Equal(SamDeclarationState.Succeeded, match.DeclarationState);
    }

    private static SamMatch NewMatch(int count)
    {
        var players = Enumerable.Range(0, count).Select(i => new PlayerSetup(Guid.NewGuid(), i)).ToArray();
        return SamMatch.Create(Guid.NewGuid(), players, DeckFactory.CreateStandardDeck());
    }

    private static SamMatchPlayerSnapshot Player(Guid id, int seat, string cards) => new(id, seat, cards.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(CardCode.Parse).ToArray(), false);

    private static SamMatchSnapshot Snapshot(SamMatchPlayerSnapshot[] players, Guid current, Card[] center, Guid? lastPlayBy, Guid[] active) =>
        new(Guid.NewGuid(), SamMatchStatus.InProgress, players, current, center, lastPlayBy, active, null, false, false, null);
}

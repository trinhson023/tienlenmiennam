using TienLenService.Domain.Matches;
using static TienLenService.Tests.TestCards;

namespace TienLenService.Tests;

public class SnapshotRecoveryTests
{
    [Fact]
    public void SnapshotRestoresHandsCenterTurnAndWinnerState()
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

        var restored = TienLenMatch.Restore(match.CaptureSnapshot());

        Assert.Equal(match.Id, restored.Id);
        Assert.Equal(match.Status, restored.Status);
        Assert.Equal(match.CurrentPlayerId, restored.CurrentPlayerId);
        Assert.Equal(match.Center.Select(x => x.Code), restored.Center.Select(x => x.Code));
        Assert.Equal(match.Players.Select(x => x.Hand.Select(c => c.Code).ToArray()), restored.Players.Select(x => x.Hand.Select(c => c.Code).ToArray()));
    }
}

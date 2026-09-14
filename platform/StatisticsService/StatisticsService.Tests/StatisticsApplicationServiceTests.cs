using StatisticsService.Application;
using StatisticsService.Domain;

namespace StatisticsService.Tests;

public sealed class StatisticsApplicationServiceTests
{
    private static readonly Guid Winner = Guid.NewGuid();
    private static readonly Guid Loser = Guid.NewGuid();
    private static readonly Guid Bot = Guid.NewGuid();

    [Fact]
    public async Task CompletedMatch_CountsHumansAndIgnoresBot()
    {
        var repo = new FakeRepository(); var service = new StatisticsApplicationService(repo);
        await service.ApplyMatchCompletedAsync(Match(Guid.NewGuid()), CancellationToken.None);
        var winner = await service.GetPlayerAsync(Winner, "tien-len", CancellationToken.None);
        var loser = await service.GetPlayerAsync(Loser, "tien-len", CancellationToken.None);
        var bot = await service.GetPlayerAsync(Bot, "tien-len", CancellationToken.None);
        Assert.Equal(1, winner.GamesPlayed); Assert.Equal(1, winner.Wins); Assert.Equal(100, winner.WinRate);
        Assert.Equal(1, loser.GamesPlayed); Assert.Equal(1, loser.Losses); Assert.Equal(0, loser.WinRate);
        Assert.Equal(0, bot.GamesPlayed);
    }

    [Fact]
    public async Task DuplicateMatch_IsIdempotent()
    {
        var repo = new FakeRepository(); var service = new StatisticsApplicationService(repo); var id = Guid.NewGuid();
        Assert.True(await service.ApplyMatchCompletedAsync(Match(id), CancellationToken.None));
        Assert.False(await service.ApplyMatchCompletedAsync(Match(id), CancellationToken.None));
        Assert.Equal(1, (await service.GetPlayerAsync(Winner, "tien-len", CancellationToken.None)).GamesPlayed);
    }

    [Fact]
    public async Task Leaderboard_RanksWinsBeforeGamesAtDefaultRating()
    {
        var repo = new FakeRepository(); var service = new StatisticsApplicationService(repo);
        await service.ApplyMatchCompletedAsync(Match(Guid.NewGuid()), CancellationToken.None);
        var board = await service.GetLeaderboardAsync("tien-len", 10, CancellationToken.None);
        Assert.Equal(Winner, board[0].UserId); Assert.Equal(1, board[0].Rank);
    }

    private static CompletedMatchFact Match(Guid id) => new(Guid.NewGuid(), id, Guid.NewGuid(), "tien-len", DateTimeOffset.UtcNow,
        [new(Winner,"winner","Winner",0,false,1), new(Loser,"loser","Loser",1,false,2), new(Bot,"bot","Bot",2,true,3)]);

    private sealed class FakeRepository : IStatisticsRepository
    {
        private readonly HashSet<Guid> _matches = [];
        private readonly Dictionary<(Guid,string), PlayerGameStatistic> _stats = [];
        public Task<bool> ApplyCompletedMatchAsync(CompletedMatchFact match, CancellationToken ct)
        {
            if (!_matches.Add(match.MatchId)) return Task.FromResult(false);
            foreach (var p in match.Players.Where(x => !x.IsBot))
            {
                var key=(p.UserId,match.GameSlug); if(!_stats.TryGetValue(key,out var stat)){stat=new PlayerGameStatistic(p.UserId,match.GameSlug,p.Username,p.DisplayName);_stats[key]=stat;} stat.ApplyResult(p.FinishPosition==1,p.Username,p.DisplayName,match.CompletedAtUtc);
            }
            return Task.FromResult(true);
        }
        public Task<PlayerGameStatistic?> GetPlayerAsync(Guid userId,string gameSlug,CancellationToken ct)=>Task.FromResult(_stats.GetValueOrDefault((userId,gameSlug)));
        public Task<IReadOnlyList<PlayerGameStatistic>> GetLeaderboardAsync(string gameSlug,int limit,CancellationToken ct)=>Task.FromResult<IReadOnlyList<PlayerGameStatistic>>(_stats.Values.Where(x=>x.GameSlug==gameSlug).OrderByDescending(x=>x.Rating).ThenByDescending(x=>x.Wins).ThenByDescending(x=>x.GamesPlayed).Take(limit).ToArray());
    }
}

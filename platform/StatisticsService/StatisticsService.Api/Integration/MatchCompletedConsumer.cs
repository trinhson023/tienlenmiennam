using MassTransit;
using RoyalGame.Contracts;
using StatisticsService.Application;

namespace StatisticsService.Api.Integration;

public sealed class MatchCompletedConsumer(StatisticsApplicationService statistics, ILogger<MatchCompletedConsumer> logger) : IConsumer<MatchCompleted>
{
    public async Task Consume(ConsumeContext<MatchCompleted> context)
    {
        var message = context.Message;
        var fact = new CompletedMatchFact(
            message.EventId, message.MatchId, message.RoomId, message.GameSlug, message.CompletedAtUtc,
            message.Players.Select(x => new CompletedMatchPlayer(x.UserId, x.Username, x.DisplayName, x.SeatNumber, x.IsBot, x.FinishPosition)).ToArray());
        var applied = await statistics.ApplyMatchCompletedAsync(fact, context.CancellationToken);
        if (applied) logger.LogInformation("Applied MatchCompleted {MatchId} for {GameSlug}.", message.MatchId, message.GameSlug);
        else logger.LogInformation("Ignored duplicate MatchCompleted {MatchId}.", message.MatchId);
    }
}

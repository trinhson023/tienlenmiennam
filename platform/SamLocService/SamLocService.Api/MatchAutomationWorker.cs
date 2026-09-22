using SamLocService.Application.Matches;

namespace SamLocService.Api;

public sealed class MatchAutomationWorker(IServiceScopeFactory scopeFactory, ILogger<MatchAutomationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var matches = scope.ServiceProvider.GetRequiredService<SamMatchApplicationService>();
                var broadcaster = scope.ServiceProvider.GetRequiredService<MatchBroadcaster>();
                var ids = await matches.GetActiveMatchIdsAsync(stoppingToken);
                var now = DateTimeOffset.UtcNow;

                foreach (var matchId in ids)
                {
                    try
                    {
                        if (await matches.ProcessAutomationAsync(matchId, now, stoppingToken))
                            await broadcaster.BroadcastMatchAsync(matchId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Automation failed for Sâm Lốc match {MatchId}", matchId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sâm Lốc automation loop failed");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
        }
    }
}

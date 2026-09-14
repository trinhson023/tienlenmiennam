using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using RoyalGame.Contracts;
using TienLenService.Infrastructure.Persistence;

namespace TienLenService.Api.Integration;

public sealed class MatchOutboxPublisherWorker(IDbContextFactory<TienLenDbContext> dbFactory, IBus bus, ILogger<MatchOutboxPublisherWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PublishBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Tiến Lên outbox publish loop failed."); }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var records = await db.Outbox
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(ct);

        foreach (var record in records)
        {
            try
            {
                var message = JsonSerializer.Deserialize<MatchCompleted>(record.PayloadJson, JsonOptions)
                    ?? throw new InvalidOperationException($"Outbox payload {record.Id} is invalid.");
                await bus.Publish(message, ct);
                record.PublishedAtUtc = DateTimeOffset.UtcNow;
                record.Attempts += 1;
                record.LastError = null;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                record.Attempts += 1;
                record.LastError = ex.Message.Length <= 1000 ? ex.Message : ex.Message[..1000];
                logger.LogWarning(ex, "Failed to publish MatchCompleted for match {MatchId}; attempt {Attempt}.", record.MatchId, record.Attempts);
            }
        }
        if (records.Count > 0) await db.SaveChangesAsync(ct);
    }
}

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SamLocService.Application.Matches;
using SamLocService.Domain.Matches;
using SamLocService.Infrastructure.Persistence;

namespace SamLocService.Infrastructure.Matches;

public sealed class PersistentMatchStore(IDbContextFactory<SamLocDbContext> dbFactory) : IMatchStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<Guid, MatchRuntime> _cache = new();

    public async Task<bool> TryAddAsync(MatchRuntime runtime, CancellationToken ct)
    {
        if (!_cache.TryAdd(runtime.Match.Id, runtime)) return false;

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.Matches.Add(ToRecord(runtime, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch
        {
            _cache.TryRemove(runtime.Match.Id, out _);
            throw;
        }
    }

    public async Task<MatchRuntime?> GetAsync(Guid matchId, CancellationToken ct)
    {
        if (_cache.TryGetValue(matchId, out var cached)) return cached;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var record = await db.Matches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == matchId, ct);
        if (record is null) return null;

        var restored = Restore(record);
        return _cache.GetOrAdd(matchId, restored);
    }

    public async Task<MatchRuntime?> GetLatestByRoomAsync(Guid roomId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = await db.Matches.AsNoTracking()
            .Where(x => x.RoomId == roomId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        return id.HasValue ? await GetAsync(id.Value, ct) : null;
    }

    public async Task<IReadOnlyList<Guid>> GetActiveMatchIdsAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Matches.AsNoTracking()
            .Where(x => x.Status != (int)SamMatchStatus.Completed)
            .Select(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task SaveAsync(MatchRuntime runtime, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var record = await db.Matches.SingleAsync(x => x.Id == runtime.Match.Id, ct);

        var completedNow =
            record.Status != (int)SamMatchStatus.Completed &&
            runtime.Match.Status == SamMatchStatus.Completed;

        var now = DateTimeOffset.UtcNow;
        Apply(record, runtime, now);

        if (completedNow)
            db.Outbox.Add(BuildMatchCompletedOutbox(runtime, now));

        await db.SaveChangesAsync(ct);
        _cache[runtime.Match.Id] = runtime;
    }

    private static SamLocOutboxRecord BuildMatchCompletedOutbox(MatchRuntime runtime, DateTimeOffset now)
    {
        var eventId = Guid.NewGuid();
        var winnerId = runtime.Match.WinnerId;

        var players = runtime.Players.Values
            .OrderBy(x => x.SeatNumber)
            .Select(identity => new OutboxMatchCompletedPlayer(
                identity.UserId,
                identity.Username,
                identity.DisplayName,
                identity.SeatNumber,
                identity.IsBot,
                identity.UserId == winnerId ? 1 : 2))
            .ToArray();

        var payload = new OutboxMatchCompleted(
            eventId,
            runtime.Match.Id,
            runtime.RoomId,
            "sam-loc",
            runtime.CompletedAtUtc ?? now,
            players);

        return new SamLocOutboxRecord
        {
            Id = eventId,
            MatchId = runtime.Match.Id,
            EventType = "MatchCompleted",
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CreatedAtUtc = now
        };
    }

    private static SamLocMatchRecord ToRecord(MatchRuntime runtime, DateTimeOffset now)
    {
        var record = new SamLocMatchRecord
        {
            Id = runtime.Match.Id,
            RoomId = runtime.RoomId,
            CreatedAtUtc = runtime.CreatedAtUtc
        };

        Apply(record, runtime, now);
        return record;
    }

    private static void Apply(SamLocMatchRecord record, MatchRuntime runtime, DateTimeOffset now)
    {
        record.RoomId = runtime.RoomId;
        record.Status = (int)runtime.Match.Status;
        record.Version = runtime.Version;
        record.SnapshotJson = JsonSerializer.Serialize(
            new PersistedEnvelope(
                runtime.Match.CaptureSnapshot(),
                runtime.Players.Values.OrderBy(x => x.SeatNumber).ToArray(),
                runtime.AbandonedUserIds.ToArray(),
                runtime.DefaultStarterUserId),
            JsonOptions);
        record.DeclarationDeadlineUtc = runtime.DeclarationDeadlineUtc;
        record.TurnDeadlineUtc = runtime.TurnDeadlineUtc;
        record.BotActionDueUtc = runtime.BotActionDueUtc;
        record.UpdatedAtUtc = now;
        record.CompletedAtUtc = runtime.CompletedAtUtc;
    }

    private static MatchRuntime Restore(SamLocMatchRecord record)
    {
        var envelope = JsonSerializer.Deserialize<PersistedEnvelope>(record.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException($"Cannot deserialize Sâm Lốc match {record.Id}.");

        return new MatchRuntime(
            record.RoomId,
            SamMatch.Restore(envelope.Match),
            envelope.Players,
            envelope.DefaultStarterUserId,
            record.Version,
            record.DeclarationDeadlineUtc,
            record.TurnDeadlineUtc,
            record.BotActionDueUtc,
            record.CreatedAtUtc,
            record.CompletedAtUtc,
            envelope.AbandonedUserIds ?? Array.Empty<Guid>());
    }

    private sealed record PersistedEnvelope(
        SamMatchSnapshot Match,
        MatchPlayerIdentity[] Players,
        Guid[]? AbandonedUserIds,
        Guid DefaultStarterUserId);

    private sealed record OutboxMatchCompleted(
        Guid EventId,
        Guid MatchId,
        Guid RoomId,
        string GameSlug,
        DateTimeOffset CompletedAtUtc,
        OutboxMatchCompletedPlayer[] Players);

    private sealed record OutboxMatchCompletedPlayer(
        Guid UserId,
        string Username,
        string DisplayName,
        int SeatNumber,
        bool IsBot,
        int FinishPosition);
}

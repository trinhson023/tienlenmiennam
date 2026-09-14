using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TienLenService.Application.Matches;
using TienLenService.Domain.Matches;
using TienLenService.Infrastructure.Persistence;

namespace TienLenService.Infrastructure.Matches;

public sealed class PersistentMatchStore(IDbContextFactory<TienLenDbContext> dbFactory) : IMatchStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<Guid, MatchRuntime> _cache = new();

    public async Task<bool> TryAddAsync(MatchRuntime runtime, CancellationToken ct)
    {
        if (!_cache.TryAdd(runtime.Match.Id.Value, runtime)) return false;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.Matches.Add(ToRecord(runtime, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch
        {
            _cache.TryRemove(runtime.Match.Id.Value, out _);
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
            .Where(x => x.Status == (int)MatchStatus.InProgress)
            .Select(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task SaveAsync(MatchRuntime runtime, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var record = await db.Matches.SingleAsync(x => x.Id == runtime.Match.Id.Value, ct);
        var completedNow = record.Status != (int)MatchStatus.Completed && runtime.Match.Status == MatchStatus.Completed;
        var now = DateTimeOffset.UtcNow;
        Apply(record, runtime, now);
        if (completedNow)
            db.Outbox.Add(BuildMatchCompletedOutbox(runtime, now));
        await db.SaveChangesAsync(ct);
        _cache[runtime.Match.Id.Value] = runtime;
    }

    private static TienLenOutboxRecord BuildMatchCompletedOutbox(MatchRuntime runtime, DateTimeOffset now)
    {
        var eventId = Guid.NewGuid();
        var matchPlayers = runtime.Match.Players.ToDictionary(x => x.Id.Value);
        var players = runtime.Players.Values.OrderBy(x => x.SeatNumber).Select(identity =>
        {
            var player = matchPlayers[identity.UserId];
            return new OutboxMatchCompletedPlayer(identity.UserId, identity.Username, identity.DisplayName, identity.SeatNumber, identity.IsBot, player.FinishPosition ?? 0);
        }).ToArray();
        var payload = new OutboxMatchCompleted(eventId, runtime.Match.Id.Value, runtime.RoomId, "tien-len", runtime.CompletedAtUtc ?? now, players);
        return new TienLenOutboxRecord
        {
            Id = eventId,
            MatchId = runtime.Match.Id.Value,
            EventType = "MatchCompleted",
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CreatedAtUtc = now
        };
    }

    private static TienLenMatchRecord ToRecord(MatchRuntime runtime, DateTimeOffset now)
    {
        var record = new TienLenMatchRecord { Id = runtime.Match.Id.Value, RoomId = runtime.RoomId, CreatedAtUtc = runtime.CreatedAtUtc };
        Apply(record, runtime, now);
        return record;
    }

    private static void Apply(TienLenMatchRecord record, MatchRuntime runtime, DateTimeOffset now)
    {
        record.RoomId = runtime.RoomId;
        record.Status = (int)runtime.Match.Status;
        record.Version = runtime.Version;
        record.SnapshotJson = JsonSerializer.Serialize(new PersistedEnvelope(runtime.Match.CaptureSnapshot(), runtime.Players.Values.OrderBy(x => x.SeatNumber).ToArray()), JsonOptions);
        record.TurnDeadlineUtc = runtime.TurnDeadlineUtc;
        record.BotActionDueUtc = runtime.BotActionDueUtc;
        record.UpdatedAtUtc = now;
        record.CompletedAtUtc = runtime.CompletedAtUtc;
    }

    private static MatchRuntime Restore(TienLenMatchRecord record)
    {
        var envelope = JsonSerializer.Deserialize<PersistedEnvelope>(record.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException($"Cannot deserialize Tiến Lên match {record.Id}.");
        var match = TienLenMatch.Restore(envelope.Match);
        return new MatchRuntime(record.RoomId, match, envelope.Players, record.Version, record.TurnDeadlineUtc, record.BotActionDueUtc, record.CreatedAtUtc, record.CompletedAtUtc);
    }

    private sealed record PersistedEnvelope(TienLenMatchSnapshot Match, MatchPlayerIdentity[] Players);
    private sealed record OutboxMatchCompleted(Guid EventId, Guid MatchId, Guid RoomId, string GameSlug, DateTimeOffset CompletedAtUtc, OutboxMatchCompletedPlayer[] Players);
    private sealed record OutboxMatchCompletedPlayer(Guid UserId, string Username, string DisplayName, int SeatNumber, bool IsBot, int FinishPosition);
}

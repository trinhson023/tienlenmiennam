using SamLocService.Domain.Matches;

namespace SamLocService.Application.Matches;

public sealed record MatchPlayerIdentity(Guid UserId, int SeatNumber, string Username, string DisplayName, bool IsBot);

public sealed class MatchRuntime
{
    private readonly HashSet<Guid> _abandonedUserIds;

    public MatchRuntime(
        Guid roomId,
        SamMatch match,
        IReadOnlyCollection<MatchPlayerIdentity> players,
        Guid defaultStarterUserId,
        long version = 1,
        DateTimeOffset? declarationDeadlineUtc = null,
        DateTimeOffset? turnDeadlineUtc = null,
        DateTimeOffset? botActionDueUtc = null,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? completedAtUtc = null,
        IReadOnlyCollection<Guid>? abandonedUserIds = null)
    {
        RoomId = roomId;
        Match = match;
        Players = players.ToDictionary(x => x.UserId);
        DefaultStarterUserId = defaultStarterUserId;
        Version = version;
        DeclarationDeadlineUtc = declarationDeadlineUtc;
        TurnDeadlineUtc = turnDeadlineUtc;
        BotActionDueUtc = botActionDueUtc;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        CompletedAtUtc = completedAtUtc;
        _abandonedUserIds = abandonedUserIds?.ToHashSet() ?? new HashSet<Guid>();
    }

    public Guid RoomId { get; }
    public SamMatch Match { get; }
    public IReadOnlyDictionary<Guid, MatchPlayerIdentity> Players { get; }
    public IReadOnlyCollection<Guid> AbandonedUserIds => _abandonedUserIds;
    public Guid DefaultStarterUserId { get; }
    public long Version { get; set; }
    public DateTimeOffset? DeclarationDeadlineUtc { get; set; }
    public DateTimeOffset? TurnDeadlineUtc { get; set; }
    public DateTimeOffset? BotActionDueUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public bool IsAbandoned(Guid userId) => _abandonedUserIds.Contains(userId);
    public bool Abandon(Guid userId) => _abandonedUserIds.Add(userId);
}

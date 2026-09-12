using TienLenService.Domain.Matches;

namespace TienLenService.Application.Matches;

public sealed record MatchPlayerIdentity(Guid UserId, int SeatNumber, string Username, string DisplayName);

public sealed class MatchRuntime
{
    public MatchRuntime(Guid roomId, TienLenMatch match, IReadOnlyCollection<MatchPlayerIdentity> players)
    {
        RoomId = roomId;
        Match = match;
        Players = players.ToDictionary(x => x.UserId);
        Version = 1;
    }

    public Guid RoomId { get; }
    public TienLenMatch Match { get; }
    public IReadOnlyDictionary<Guid, MatchPlayerIdentity> Players { get; }
    public long Version { get; set; }
    public object SyncRoot { get; } = new();
}

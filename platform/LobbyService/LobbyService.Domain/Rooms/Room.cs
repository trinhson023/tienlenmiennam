using LobbyService.Domain.Games;

namespace LobbyService.Domain.Rooms;

public sealed class Room
{
    private readonly List<RoomMember> _members = [];
    private Room() { }

    public Room(Guid id, Guid gameDefinitionId, Guid hostUserId, string name, int maxPlayers, string hostUsername, string hostDisplayName)
    {
        if (maxPlayers < 2 || maxPlayers > 8) throw new ArgumentOutOfRangeException(nameof(maxPlayers));
        Id = id;
        GameDefinitionId = gameDefinitionId;
        HostUserId = hostUserId;
        Name = string.IsNullOrWhiteSpace(name) ? $"{hostDisplayName}'s Room" : name.Trim();
        MaxPlayers = maxPlayers;
        Status = RoomStatus.Open;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
        _members.Add(new RoomMember(Guid.NewGuid(), id, hostUserId, hostUsername, hostDisplayName, 0));
    }

    public Guid Id { get; private set; }
    public Guid GameDefinitionId { get; private set; }
    public Guid HostUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int MaxPlayers { get; private set; }
    public RoomStatus Status { get; private set; }
    public Guid? ActiveMatchId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public GameDefinition GameDefinition { get; private set; } = null!;
    public IReadOnlyCollection<RoomMember> Members => _members;

    public RoomMember Join(Guid userId, string username, string displayName)
    {
        if (Status != RoomStatus.Open) throw new InvalidOperationException("Room is not open.");
        if (_members.Any(x => x.UserId == userId)) throw new InvalidOperationException("Player is already in the room.");
        if (_members.Count >= MaxPlayers) throw new InvalidOperationException("Room is full.");

        var occupied = _members.Select(x => x.SeatNumber).ToHashSet();
        var seat = Enumerable.Range(0, MaxPlayers).First(x => !occupied.Contains(x));
        var member = new RoomMember(Guid.NewGuid(), Id, userId, username, displayName, seat);
        _members.Add(member);
        Touch();
        return member;
    }

    public bool Leave(Guid userId)
    {
        var member = _members.SingleOrDefault(x => x.UserId == userId);
        if (member is null) return false;
        _members.Remove(member);
        if (_members.Count > 0 && HostUserId == userId)
            HostUserId = _members.OrderBy(x => x.JoinedAtUtc).ThenBy(x => x.SeatNumber).First().UserId;
        Touch();
        return true;
    }

    public void AttachMatch(Guid matchId)
    {
        if (_members.Count < 2) throw new InvalidOperationException("At least two players are required.");
        ActiveMatchId = matchId;
        Status = RoomStatus.InGame;
        Touch();
    }

    public void ReturnToLobby()
    {
        ActiveMatchId = null;
        Status = RoomStatus.Open;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

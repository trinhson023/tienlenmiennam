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
    public Guid? LastCompletedMatchId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public GameDefinition GameDefinition { get; private set; } = null!;
    public IReadOnlyCollection<RoomMember> Members => _members;

    public RoomMember Join(Guid userId, string username, string displayName)
    {
        EnsureOpen();
        if (_members.Any(x => x.UserId == userId)) throw new InvalidOperationException("Player is already in the room.");
        EnsureCapacity();
        var member = new RoomMember(Guid.NewGuid(), Id, userId, username, displayName, FirstFreeSeat());
        _members.Add(member);
        Touch();
        return member;
    }

    public RoomMember AddBot()
    {
        EnsureOpen();
        EnsureCapacity();
        var seat = FirstFreeSeat();
        var botNumber = Enumerable.Range(1, MaxPlayers).First(number => !_members.Any(x => x.IsBot && x.DisplayName == $"Bot {number}"));
        var botUserId = Guid.NewGuid();
        var member = new RoomMember(Guid.NewGuid(), Id, botUserId, $"bot_{botUserId:N}"[..12], $"Bot {botNumber}", seat, isBot: true);
        _members.Add(member);
        Touch();
        return member;
    }

    public bool RemoveBot(Guid botUserId)
    {
        EnsureOpen();
        var bot = _members.SingleOrDefault(x => x.UserId == botUserId && x.IsBot);
        if (bot is null) return false;
        _members.Remove(bot);
        Touch();
        return true;
    }

    public bool Kick(Guid userId)
    {
        EnsureOpen();
        if (userId == HostUserId) return false;
        var member = _members.SingleOrDefault(x => x.UserId == userId && !x.IsBot);
        if (member is null) return false;
        _members.Remove(member);
        Touch();
        return true;
    }

    public bool Leave(Guid userId)
    {
        var member = _members.SingleOrDefault(x => x.UserId == userId && !x.IsBot);
        if (member is null) return false;
        _members.Remove(member);
        if (HostUserId == userId)
        {
            var nextHumanHost = _members.Where(x => !x.IsBot).OrderBy(x => x.JoinedAtUtc).ThenBy(x => x.SeatNumber).FirstOrDefault();
            if (nextHumanHost is null) _members.Clear();
            else HostUserId = nextHumanHost.UserId;
        }
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

    public void CompleteActiveMatch(Guid matchId)
    {
        if (Status == RoomStatus.Open && ActiveMatchId is null && LastCompletedMatchId == matchId) return;
        if (Status != RoomStatus.InGame || ActiveMatchId != matchId)
            throw new InvalidOperationException("The match is not the room's active match.");
        LastCompletedMatchId = matchId;
        ActiveMatchId = null;
        Status = RoomStatus.Open;
        Touch();
    }

    private int FirstFreeSeat()
    {
        var occupied = _members.Select(x => x.SeatNumber).ToHashSet();
        return Enumerable.Range(0, MaxPlayers).First(x => !occupied.Contains(x));
    }

    private void EnsureOpen()
    {
        if (Status != RoomStatus.Open) throw new InvalidOperationException("Room is not open.");
    }

    private void EnsureCapacity()
    {
        if (_members.Count >= MaxPlayers) throw new InvalidOperationException("Room is full.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

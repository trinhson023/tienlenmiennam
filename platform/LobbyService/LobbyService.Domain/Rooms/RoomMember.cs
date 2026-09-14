namespace LobbyService.Domain.Rooms;

public sealed class RoomMember
{
    private RoomMember() { }

    public RoomMember(Guid id, Guid roomId, Guid userId, string username, string displayName, int seatNumber, bool isBot = false)
    {
        Id = id;
        RoomId = roomId;
        UserId = userId;
        Username = username.Trim();
        DisplayName = displayName.Trim();
        SeatNumber = seatNumber;
        IsBot = isBot;
        JoinedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public int SeatNumber { get; private set; }
    public bool IsBot { get; private set; }
    public DateTimeOffset JoinedAtUtc { get; private set; }
    public Room Room { get; private set; } = null!;
}

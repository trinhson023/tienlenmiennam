namespace SocialService.Application;

public interface IRoomAccessGateway
{
    Task<RoomSocialContext?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken);
}

public interface IRecentSocialHistory
{
    Task AddAsync(SocialChatEvent message, CancellationToken cancellationToken);
    Task<IReadOnlyList<SocialChatEvent>> GetRecentAsync(Guid roomId, int limit, CancellationToken cancellationToken);
}

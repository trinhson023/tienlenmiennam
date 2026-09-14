using SocialService.Application;

namespace SocialService.Tests;

public sealed class SocialApplicationServiceTests
{
    private static readonly Guid RoomId = Guid.NewGuid();
    private static readonly Guid AliceId = Guid.NewGuid();
    private static readonly Guid BobId = Guid.NewGuid();
    private static readonly Guid BotId = Guid.NewGuid();

    [Fact]
    public async Task QuickChat_UsesCanonicalRoomIdentityAndNormalizesText()
    {
        var history = new FakeHistory();
        var service = new SocialApplicationService(new FakeRooms(), history);
        var result = await service.SendQuickChatAsync(RoomId, AliceId, "  hello   bro  ", CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("Alice", result.Value!.SenderDisplayName);
        Assert.Equal("hello bro", result.Value.Text);
        Assert.Equal("taunt", result.Value.Kind);
        Assert.Single(history.Items);
    }

    [Fact]
    public async Task Outsider_CannotReadOrSend()
    {
        var service = new SocialApplicationService(new FakeRooms(), new FakeHistory());
        var outsider = Guid.NewGuid();
        Assert.Equal("not_in_room", (await service.GetRecentAsync(RoomId, outsider, 20, CancellationToken.None)).ErrorCode);
        Assert.Equal("not_in_room", (await service.SendRoomChatAsync(RoomId, outsider, "hi", CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Reaction_RequiresRoomTargetAndWhitelist()
    {
        var service = new SocialApplicationService(new FakeRooms(), new FakeHistory());
        Assert.Equal("target_not_in_room", (await service.CreateReactionAsync(RoomId, AliceId, "bomb", Guid.NewGuid(), CancellationToken.None)).ErrorCode);
        Assert.Equal("invalid_reaction", (await service.CreateReactionAsync(RoomId, AliceId, "rocket", BobId, CancellationToken.None)).ErrorCode);
        Assert.True((await service.CreateReactionAsync(RoomId, AliceId, "bomb", BobId, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Audience_ContainsCurrentHumansOnly()
    {
        var service = new SocialApplicationService(new FakeRooms(), new FakeHistory());
        var result = await service.GetHumanAudienceAsync(RoomId, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { AliceId, BobId }, result.Value);
    }

    private sealed class FakeRooms : IRoomAccessGateway
    {
        public Task<RoomSocialContext?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken) => Task.FromResult<RoomSocialContext?>(
            roomId == RoomId
                ? new RoomSocialContext(RoomId, "InGame", [
                    new RoomSocialMember(AliceId, "alice", "Alice", false),
                    new RoomSocialMember(BobId, "bob", "Bob", false),
                    new RoomSocialMember(BotId, "bot", "Bot", true)
                ])
                : null);
    }

    private sealed class FakeHistory : IRecentSocialHistory
    {
        public List<SocialChatEvent> Items { get; } = [];
        public Task AddAsync(SocialChatEvent message, CancellationToken cancellationToken) { Items.Add(message); return Task.CompletedTask; }
        public Task<IReadOnlyList<SocialChatEvent>> GetRecentAsync(Guid roomId, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SocialChatEvent>>(Items.TakeLast(limit).ToArray());
    }
}

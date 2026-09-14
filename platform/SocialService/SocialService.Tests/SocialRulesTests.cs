using SocialService.Domain;

namespace SocialService.Tests;

public sealed class SocialRulesTests
{
    [Fact]
    public void Normalize_CollapsesWhitespaceAndTruncates()
    {
        Assert.Equal("hello world", SocialText.Normalize("  hello   world  ", 80));
        Assert.Equal("12345", SocialText.Normalize("123456789", 5));
    }

    [Theory]
    [InlineData("bomb", "💣")]
    [InlineData(" TOMATO ", "🍅")]
    [InlineData("poop", "💩")]
    public void ReactionCatalog_ResolvesWhitelist(string value, string expectedEmoji)
    {
        Assert.True(ReactionCatalog.TryResolve(value, out _, out var emoji));
        Assert.Equal(expectedEmoji, emoji);
    }

    [Fact]
    public void ReactionCatalog_RejectsUnknownItems() => Assert.False(ReactionCatalog.TryResolve("rocket", out _, out _));
}

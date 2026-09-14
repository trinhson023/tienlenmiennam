using MediaService.Domain;

namespace MediaService.Tests;

public sealed class MediaRulesTests
{
    [Fact] public void Track_RequiresCanonicalYouTubeId() { Assert.NotNull(MediaTrack.Create("dQw4w9WgXcQ", "x", "y", "")); Assert.Null(MediaTrack.Create("https://youtube.com/watch?v=x", "x", "y", "")); }
    [Fact] public void Track_TruncatesMetadata() { var track=MediaTrack.Create("dQw4w9WgXcQ",new string('x',200),new string('y',120),new string('z',600))!; Assert.Equal(160,track.Title.Length); Assert.Equal(100,track.ChannelTitle.Length); Assert.Equal(500,track.Thumbnail.Length); }
}

using System.Xml.Linq;
using ChartForgeX.Stories;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryChapterClockTests {
    [Theory]
    [InlineData(.25000004, .5, 1)]
    [InlineData(.4, .8, 2)]
    public void CapturedChapterBoundariesSelectTheSameSceneInFramesAndAnimation(double first, double second, int chapter) {
        var story = VisualStory.Create("Chapter clock").WithSize(480, 320);
        foreach (var (id, duration) in new[] { ("first", first), ("middle", second), ("last", .5) })
            story.Scene(id, id, duration).Panel(id, new VisualStoryTextSurface(id));
        story.Outcome("last", "Last", "last");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero));
        var boundary = prepared.Chapters[chapter].Start;
        Assert.Equal("Chapter clock — " + prepared.Chapters[chapter].Title, prepared.PrepareFrame(boundary).Accessibility.Name);
        Assert.Equal("Chapter clock — " + prepared.Chapters[chapter - 1].Title,
            prepared.PrepareFrame(boundary - TimeSpan.FromTicks(1)).Accessibility.Name);
        var svg = XDocument.Parse(prepared.ToAnimatedSvg(new VisualStoryFrameOptions(20)));
        var time = boundary.TotalSeconds.ToString("0.#########", System.Globalization.CultureInfo.InvariantCulture);
        var frame = svg.Root!.Elements().Single(element => (string?)element.Attribute("data-cfx-frame-time") == time);
        Assert.Equal(prepared.Chapters[chapter].Id, (string?)frame.Attribute("data-cfx-scene"));
    }
}

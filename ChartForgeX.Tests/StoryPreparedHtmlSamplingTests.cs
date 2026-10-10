using ChartForgeX.Stories;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryPreparedHtmlSamplingTests {
    [Fact]
    public void LongPreparedHtmlUsesTheCallersFrameBudget() {
        var story = VisualStory.Create("Explicit HTML sampling").WithSize(480, 320);
        for (var index = 0; index < 6; index++)
            story.Scene("scene" + index, "Chapter " + index, 60).Panel("result", new VisualStoryTextSurface("Chapter " + index));
        story.Outcome("ready", "Ready", "result");
        var prepared = story.Prepare();
        Assert.Throws<InvalidOperationException>(() => prepared.ToHtmlPage());
        Assert.Throws<InvalidOperationException>(() => prepared.ToHtmlPage(new VisualStoryFrameOptions(2)));
        var frames = new VisualStoryFrameOptions(2, maximumFrames: 900);
        var svg = prepared.ToAnimatedSvg(frames);
        var html = prepared.ToHtmlPage(frames);
        Assert.Contains(svg, html);
        Assert.Contains("data-cfx-motion-duration=\"361.5\"", html);
        Assert.DoesNotContain("<script", html);
    }
}

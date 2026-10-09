using System.Text;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryPlaybackReviewTests {
    [Fact]
    public void CompletedFrameIncludesAllDeclaredOutcomeLabels() {
        var story = Basic(new VisualStoryTextSurface("Ready"));
        story.Outcome("second", "Second result", "result");
        Assert.Contains("First result · Second result", story.Prepare().ToSvg());
        Assert.DoesNotContain("✓ First result", story.Prepare().ToSvg(TimeSpan.Zero));
    }

    [Fact]
    public void AnimatedSvgKeepsTranscriptOnceAndDecoratesEmbeddedFrames() {
        var story = Basic(new VisualStorySourceSurface(StorySourceText.Create("Ready")));
        story.WithDescription("Unique complete transcript sentinel");
        var animation = XDocument.Parse(story.Prepare().ToAnimatedSvg(new VisualStoryFrameOptions(2)));
        Assert.Contains("Unique complete transcript sentinel", animation.Root!.Elements().Single(element => element.Name.LocalName == "desc").Value);
        foreach (var image in animation.Descendants().Where(element => element.Name.LocalName == "image")) {
            var embedded = Embedded(image);
            Assert.Equal("true", (string?)embedded.Root!.Attribute("aria-hidden"));
            Assert.DoesNotContain(embedded.Descendants(), element => element.Name.LocalName == "desc");
            Assert.DoesNotContain("Unique complete transcript sentinel", embedded.ToString());
        }
    }

    [Fact]
    public void SvgDensityScalesEmbeddedTerminalPixels() {
        var prepared = Basic(new VisualStoryTerminalSurface(TerminalStory.Create().WithWidth(480).WithFinalPrompt(false).Output("Ready"))).Prepare();
        var one = TerminalPixels(prepared.ToAnimatedSvg(new VisualStoryFrameOptions(2, 1)));
        var two = TerminalPixels(prepared.ToAnimatedSvg(new VisualStoryFrameOptions(2, 2)));
        Assert.Equal(one.Width * 2, two.Width);
        Assert.Equal(one.Height * 2, two.Height);
    }

    [Theory]
    [InlineData(0, "First")]
    [InlineData(2, "Second")]
    [InlineData(-1, "Second")]
    public void SingleRowSourceViewportKeepsTheFocusedRow(int highlighted, string expected) {
        var source = StorySourceText.Create("First\nSecond\nThird");
        var options = new VisualStorySourceOptions(fontSize: 32, lineNumbers: false, highlightedLine: Math.Max(0, highlighted));
        var surface = highlighted < 0
            ? new VisualStorySourceSurface(StorySourceTimeline.Create(source).Select(6, 0, TimeSpan.Zero).Pause(TimeSpan.FromSeconds(1)), options: options)
            : new VisualStorySourceSurface(source, options: options);
        var story = VisualStory.Create("One visible row").WithSize(600, 400);
        story.Scene("edit", "Edit", 1, VisualStorySceneLayout.Stacked).Panel("source", surface).Panel("result", new VisualStoryTextSurface("Ready"));
        story.Outcome("result", "Ready", "result");
        var svg = XDocument.Parse(story.Prepare().ToSvg());
        var visible = string.Join("", svg.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
        Assert.Contains(expected, visible);
        Assert.DoesNotContain(expected == "First" ? "Second" : "First", visible);
    }

    [Fact]
    public void DefaultSvgSamplingCoversShortChaptersAndExplicitSamplingStaysExplicit() {
        var story = VisualStory.Create("Short chapters").WithSize(480, 320);
        story.Scene("first", "First", .34).Panel("first", new VisualStoryTextSurface("First"));
        story.Scene("second", "Second", .25).Panel("second", new VisualStoryTextSurface("Second"));
        story.Scene("last", "Last", .25).Panel("last", new VisualStoryTextSurface("Last"));
        story.Outcome("last", "Last", "last");
        Assert.Throws<InvalidOperationException>(() => story.Prepare().ToAnimatedSvg(new VisualStoryFrameOptions(6)));
        Assert.Contains("data-cfx-scene=\"second\"", story.ToSvg());
        Assert.Contains("data-cfx-scene=\"second\"", story.ToHtmlPage());
    }

    [Theory]
    [InlineData(60, 1.5)]
    [InlineData(2, .26)]
    public void GifRejectsViewerClampedDelaysBeforeWriting(int rate, double hold) {
        var story = VisualStory.Create("GIF delays").WithSize(480, 320);
        story.Scene("result", "Result", .25).Panel("result", new VisualStoryTextSurface("Ready"));
        story.Outcome("ready", "Ready", "result");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(hold), TimeSpan.Zero, 1));
        using var stream = new MemoryStream();
        if (rate > 50) Assert.Throws<ArgumentOutOfRangeException>(() => prepared.WriteAnimation(stream, RasterAnimationFormat.Gif, new VisualStoryFrameOptions(rate)));
        else Assert.Throws<InvalidOperationException>(() => prepared.WriteAnimation(stream, RasterAnimationFormat.Gif, new VisualStoryFrameOptions(rate)));
        Assert.Equal(0, stream.Length);
        Assert.True(prepared.ToApng(new VisualStoryFrameOptions(rate)).Length > 64);
    }

    private static VisualStory Basic(VisualStorySurface surface) {
        var story = VisualStory.Create("Review contract").WithSize(480, 320);
        story.Scene("result", "Result", 1).Panel("result", surface);
        return story.Outcome("first", "First result", "result");
    }
    private static XDocument Embedded(XElement image) => XDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(((string)image.Attribute("href")!).Split(',')[1])));
    private static RgbaImage TerminalPixels(string svg) {
        var outerImage = XDocument.Parse(svg).Descendants().First(element => element.Name.LocalName == "image");
        var image = Embedded(outerImage).Descendants().First(element => element.Name.LocalName == "image");
        return RasterImageDecoder.Decode(Convert.FromBase64String(((string)image.Attribute("href")!).Split(',')[1]));
    }
}

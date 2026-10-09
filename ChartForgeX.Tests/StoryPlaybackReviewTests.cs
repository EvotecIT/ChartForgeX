using System.Text;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using ChartForgeX.Svg;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryPlaybackReviewTests {
    [Theory]
    [InlineData(.4, true)]
    [InlineData(.2, false)]
    public void IncomingCrossFadeVisibilityUsesBothOpacityAndDisplayDuration(double transition, bool readable) {
        var story = VisualStory.Create("Incoming chapter").WithSize(480, 320);
        story.Scene("first", "First", .6).Panel("first", new VisualStoryTextSurface("First"));
        story.Scene("middle", "Middle", .35).Panel("middle", new VisualStoryTextSurface("Middle"));
        story.Scene("last", "Last", .5).Panel("last", new VisualStoryTextSurface("Last"));
        story.Outcome("last", "Last", "last");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(transition: TimeSpan.FromSeconds(transition)));
        var sampling = new VisualStoryFrameOptions(2);
        if (!readable) {
            Assert.Throws<InvalidOperationException>(() => prepared.Frames(sampling).First());
            Assert.Throws<InvalidOperationException>(() => prepared.ToAnimatedSvg(sampling));
        } else {
            var frames = prepared.Frames(sampling).ToArray();
            Assert.Equal(prepared.Duration.Ticks, frames.Sum(frame => frame.Duration.Ticks));
            Assert.Equal(prepared.RenderAt(TimeSpan.FromSeconds(.5)).Pixels, frames[1].Image.Pixels);
            var svg = XDocument.Parse(prepared.ToAnimatedSvg(sampling));
            var chapters = svg.Root!.Elements().Where(element => element.Attribute("data-cfx-chapter-id") != null).ToArray();
            Assert.Equal(new[] { "first", "middle", "last" }, chapters.Select(element => (string?)element.Attribute("data-cfx-chapter-id")));
            Assert.Equal("0.5", (string?)chapters[1].Attribute("data-cfx-chapter-frame-time"));
            Assert.Equal("0.6", (string?)chapters[1].Attribute("data-cfx-chapter-start"));
            Assert.DoesNotContain(svg.Root.Elements(), element => (string?)element.Attribute("data-cfx-scene") == "middle");
            var page = new HtmlMotionPlayerRenderer().RenderPage(svg.ToString());
            Assert.Contains("data-cfx-chapter-id=\"middle\"", page);
            chapters[1].SetAttributeValue("data-cfx-chapter-frame-time", "0.6");
            Assert.Throws<ArgumentException>(() => new HtmlMotionPlayerRenderer().RenderPage(svg.ToString()));
        }
        foreach (var format in new[] { RasterAnimationFormat.Gif, RasterAnimationFormat.Apng }) {
            using var stream = new MemoryStream();
            if (readable) {
                prepared.WriteAnimation(stream, format, sampling);
                Assert.True(stream.Length > 64);
            } else {
                Assert.Throws<InvalidOperationException>(() => prepared.WriteAnimation(stream, format, sampling));
                Assert.Equal(0, stream.Length);
            }
        }
    }

    [Fact]
    public void DifferentStoriesWithTheSameTitleKeepIndependentAnimationNames() {
        var first = XDocument.Parse(Basic(new VisualStoryTextSurface("First")).Prepare().ToAnimatedSvg(new VisualStoryFrameOptions(2)));
        var second = XDocument.Parse(Basic(new VisualStoryTextSurface("Second")).Prepare().ToAnimatedSvg(new VisualStoryFrameOptions(2)));
        var firstId = (string)first.Root!.Attribute("id")!;
        var secondId = (string)second.Root!.Attribute("id")!;
        Assert.NotEqual(firstId, secondId);
        foreach (var document in new[] { first, second }) {
            var id = (string)document.Root!.Attribute("id")!;
            var css = document.Root.Elements().Single(element => element.Name.LocalName == "style").Value;
            Assert.Contains("@keyframes " + id + "-motion-frame-0", css);
            Assert.Contains("animation:" + id + "-motion-frame-0", css);
        }
    }

    [Fact]
    public void AOneMillisecondCompletedChapterFailsEverySampledExportBeforeWriting() {
        var story = VisualStory.Create("Readable duration").WithSize(480, 320);
        story.Scene("first", "First", .251).Panel("first", new VisualStoryTextSurface("First"));
        story.Scene("last", "Last", .25).Panel("last", new VisualStoryTextSurface("Last"));
        story.Outcome("last", "Last", "last");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero));
        var sampling = new VisualStoryFrameOptions(2);
        Assert.Throws<InvalidOperationException>(() => prepared.Frames(sampling).First());
        Assert.Throws<InvalidOperationException>(() => prepared.ToAnimatedSvg(sampling));
        foreach (var format in new[] { RasterAnimationFormat.Gif, RasterAnimationFormat.Apng }) {
            using var stream = new MemoryStream();
            Assert.Throws<InvalidOperationException>(() => prepared.WriteAnimation(stream, format, sampling));
            Assert.Equal(0, stream.Length);
        }
        var held = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(.25), TimeSpan.Zero));
        Assert.Equal(2, held.FrameSource(sampling).FrameCount);
    }

    [Fact]
    public void SvgDocumentBudgetIncludesEscapedTextAttributesAndClosingMarkup() {
        const string expected = "<svg title=\"&amp;&quot;&#10;\">&lt;&amp;</svg>";
        var writer = new SvgMarkupWriter(16, expected.Length);
        Assert.Equal(expected, writer.StartElement("svg").Attribute("title", "&\"\n").Text("<&").EndElement().Build());
        var smaller = new SvgMarkupWriter(16, expected.Length - 1);
        Assert.Throws<InvalidOperationException>(() => smaller.StartElement("svg").Attribute("title", "&\"\n").Text("<&").EndElement().Build());
        Assert.Throws<InvalidOperationException>(() => new SvgMarkupWriter(16, 20).StartElement("svg").Text("&&&&"));
        Assert.Throws<InvalidOperationException>(() => new SvgMarkupWriter(16, 20).StartElement("svg").Raw(new string('x', 21)));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(18)]
    [InlineData(32)]
    public void EditorFilenameTypographyFitsAboveTheSeparatorAtEverySupportedZoom(double fontSize) {
        var source = new VisualStorySourceSurface(StorySourceText.Create("Ready"), options: new VisualStorySourceOptions(fileName: "demo.cs", fontSize: fontSize));
        var svg = XDocument.Parse(Basic(source).Prepare().ToSvg());
        var filename = svg.Descendants().Single(element => element.Name.LocalName == "text" && element.Value == "demo.cs");
        Assert.Equal("14", (string?)filename.Attribute("font-size"));
        Assert.Equal("140", (string?)filename.Attribute("y"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetainedTerminalPanelsAndCrossFadesAreRejectedBeforeAllocatingOrWriting(bool crossFade) {
        var terminal = TerminalStory.Create().WithWidth(1800).WithTypography(24, 40).WithFinalPrompt(false);
        for (var i = 0; i < 104; i++) terminal.Output("line " + i);
        var story = VisualStory.Create("Retained terminal images").WithSize(1400, 788);
        var surface = new VisualStoryTerminalSurface(terminal);
        if (crossFade) {
            story.Scene("first", "First", 1).Panel("result", surface);
            story.Scene("last", "Last", 1).Panel("result", surface);
        } else {
            story.Scene("last", "Last", 1, VisualStorySceneLayout.Stacked).Panel("first", surface).Panel("result", surface);
        }
        story.Outcome("ready", "Ready", "result");
        var content = VisualStoryLayout.PanelContent(story.Scenes[0].Panels[0], VisualStoryLayout.Panels(story, story.Scenes[0])[0]);
        var formerPeak = PngTerminalStoryRenderer.EstimateFittedWorkingBytes(terminal, content.Width, content.Height, 2);
        var canvasBytes = AnimatedRasterMemoryBudget.RgbaFramesRetainedBytes(2800, 1576, 5);
        Assert.True(formerPeak + canvasBytes < AnimatedRasterMemoryBudget.MaximumRetainedBytes);
        Assert.True(PngVisualStoryRenderer.MaximumFittedTerminalWorkingBytes(story, 2) + canvasBytes > AnimatedRasterMemoryBudget.MaximumRetainedBytes);
        var prepared = story.Prepare();
        Assert.Throws<InvalidOperationException>(() => prepared.RenderAt(TimeSpan.FromSeconds(.9), 2));
        Assert.Throws<InvalidOperationException>(() => prepared.ToPng(outputScale: 2));
        foreach (var format in new[] { RasterAnimationFormat.Gif, RasterAnimationFormat.Apng }) {
            using var stream = new MemoryStream();
            Assert.Throws<InvalidOperationException>(() => prepared.WriteAnimation(stream, format, new VisualStoryFrameOptions(2, 2)));
            Assert.Equal(0, stream.Length);
        }
    }

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
        story.Scene("first", "First", .501).Panel("first", new VisualStoryTextSurface("First"));
        story.Scene("last", "Last", .25).Panel("last", new VisualStoryTextSurface("Last"));
        story.Outcome("last", "Last", "last");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero));
        Assert.Throws<InvalidOperationException>(() => prepared.ToAnimatedSvg(new VisualStoryFrameOptions(6)));
        var svg = prepared.ToAnimatedSvg();
        Assert.Contains("data-cfx-scene=\"last\"", svg);
        Assert.Contains("data-cfx-chapter-id=\"last\"", new HtmlMotionPlayerRenderer().RenderPage(svg));
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

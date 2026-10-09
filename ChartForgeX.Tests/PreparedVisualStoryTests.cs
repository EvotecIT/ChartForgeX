using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedVisualStoryTests {
    [Fact]
    public void PreparationDetachesMutableInputsAndReturnedFrames() {
        var theme = VisualStoryTheme.GraphiteDark();
        var table = TerminalTable.Create().WithColumns("Result").AddRow("ready");
        var terminal = TerminalStory.Create().Command("Invoke-Demo", 0.5).Table(table);
        var source = StorySourceText.Create("Invoke-Demo", "powershell");
        var pixels = new byte[] { 200, 80, 30, 255 };
        var story = VisualStory.Create("Stable observation").WithSize(600, 400).WithTheme(theme);
        story.Scene("write", "Write", 1).Panel("source", new VisualStorySourceSurface(source));
        story.Scene("run", "Run", 2, VisualStorySceneLayout.Split)
            .Panel("terminal", new VisualStoryTerminalSurface(terminal))
            .Panel("result", new VisualStoryMediaSurface(new RgbaImage(1, 1, pixels), "result"));
        story.Outcome("ready", "Ready", "result");
        var prepared = story.Prepare();
        var before = prepared.ToPng();
        var transcript = prepared.ToTranscript();
        theme.Background = ChartColor.White; table.AddRow("changed"); terminal.Output("changed");
        source.WithLanguage("changed"); pixels[0] = 0;
        Assert.Equal(before, prepared.ToPng());
        Assert.Equal(transcript, prepared.ToTranscript());
        var frame = prepared.RenderAt(prepared.Duration);
        Array.Clear(frame.Pixels);
        Assert.Equal(before, prepared.ToPng());
        Assert.Equal(TimeSpan.FromSeconds(4.5), prepared.Duration);
        Assert.Equal(TimeSpan.FromSeconds(1), prepared.Chapters[1].Start);
    }

    [Fact]
    public void EmbeddedTerminalChangesOnTheSceneClockAndEndpointHoldsCompletedState() {
        var story = VisualStory.Create("Terminal playback").WithSize(600, 400);
        story.Scene("run", "Run", 2).Panel("result", new VisualStoryTerminalSurface(
            TerminalStory.Create().Command("Write-Output 'ready'", 1).Output("ready")));
        story.Outcome("ready", "Ready", "result");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(playCount: 1));
        Assert.NotEqual(prepared.ToPng(TimeSpan.Zero), prepared.ToPng(TimeSpan.FromSeconds(1)));
        Assert.Equal(prepared.ToPng(), prepared.ToPng(prepared.Duration));
        Assert.Equal(prepared.ToPng(), prepared.ToPng(TimeSpan.FromHours(1)));
        var frames = prepared.Frames(new VisualStoryFrameOptions(6)).ToArray();
        Assert.Equal(prepared.Duration.Ticks, frames.Sum(frame => frame.Duration.Ticks));
        Assert.Equal(prepared.RenderAt(prepared.ContentDuration).Pixels, frames[^1].Image.Pixels);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void RepeatableProducerMatchesRetainedEncodingAndReleasesOutputStream(RasterAnimationFormat format) {
        var frames = new[] {
            new RasterAnimationFrame(new RgbaImage(2, 1, new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 }), TimeSpan.FromMilliseconds(170)),
            new RasterAnimationFrame(new RgbaImage(2, 1, new byte[] { 0, 0, 255, 255, 0, 255, 0, 255 }), TimeSpan.FromMilliseconds(330))
        };
        var reads = 0;
        var borrowed = new byte[8];
        var source = new RasterAnimationSource(2, 1, 2, (index, _) => {
            reads++; Array.Copy(frames[index].Image.Pixels, borrowed, borrowed.Length);
            return new RasterAnimationFrame(new RgbaImage(2, 1, borrowed), frames[index].Duration);
        });
        using var stream = new MemoryStream();
        RasterAnimationEncoder.WriteTo(stream, source, format, new RasterAnimationOptions { PlayCount = 3 });
        Assert.Equal(RasterAnimationEncoder.Encode(frames, format, new RasterAnimationOptions { PlayCount = 3 }), stream.ToArray());
        Assert.Equal(format == RasterAnimationFormat.Gif ? 4 : 2, reads);
        Assert.True(stream.CanWrite);
    }

    [Fact]
    public void FrameBudgetAndCancellationFailBeforeProducingPixels() {
        var story = VisualStory.Create("Bounded playback").WithSize(480, 320);
        story.Scene("result", "Result", 10).Panel("result", new VisualStoryTextSurface("ready"));
        story.Outcome("ready", "Ready", "result");
        var prepared = story.Prepare();
        Assert.Throws<InvalidOperationException>(() => prepared.FrameSource(new VisualStoryFrameOptions(30, maximumFrames: 20)));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => prepared.Frames(cancellationToken: cancellation.Token).First());
        Assert.Throws<ArgumentOutOfRangeException>(() => prepared.RenderAt(TimeSpan.FromTicks(-1)));
    }

    [Theory]
    [InlineData(.331, .5, 3)]
    public void GifQuantizationCannotMoveTheCompletedChapterBeforeItsBoundary(double firstSeconds, double lastSeconds, int rate) {
        var story = VisualStory.Create("Quantized boundary").WithSize(480, 320);
        story.Scene("first", "First", firstSeconds).Panel("first", new VisualStoryTextSurface("First"));
        story.Scene("last", "Last", lastSeconds).Panel("last", new VisualStoryTextSurface("Last"));
        story.Outcome("last", "Last", "last");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(1.5), TimeSpan.Zero, 1));
        var sampling = new VisualStoryFrameOptions(rate);
        Assert.True(prepared.FrameSource(sampling).FrameCount > 3);
        using var stream = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() => prepared.WriteAnimation(stream, RasterAnimationFormat.Gif, sampling));
        Assert.Equal(0, stream.Length);
        Assert.Throws<InvalidOperationException>(() => prepared.ToGif(sampling));
        Assert.True(prepared.ToApng(sampling).Length > 64);
    }
}

using ChartForgeX.Raster;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TerminalStoryRasterTimingTests {
    [Theory]
    [InlineData(RasterAnimationFormat.Gif, 2, new[] { 50, 1 })]
    [InlineData(RasterAnimationFormat.Apng, 2, new[] { 50, 1 })]
    [InlineData(RasterAnimationFormat.Gif, 6, new[] { 17, 17, 1 })]
    [InlineData(RasterAnimationFormat.Apng, 6, new[] { 17, 17, 1 })]
    public void ShortStoryKeepsItsCadenceAndACompletedFrameWithoutAnExtraFullHold(RasterAnimationFormat format, int framesPerSecond, int[] delays) {
        var story = ReadyStory();
        Assert.Equal(0.22, TerminalStoryLayout.Build(story).DurationSeconds, 6);
        var options = TerminalStoryAnimationOptions.Create().WithFramesPerSecond(framesPerSecond)
            .WithEndHold(0).WithLoop(false).WithMaximumFrames(delays.Length);
        var controls = ExportContainerInfo.Animation(Encode(story, format, options), format);
        Assert.Equal(delays, controls.DelaysCentiseconds);
        Assert.Equal(1, controls.PlayCount);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void EndHoldStartsAtQuantizedCompletionAndUsesAResidualLastDelay(RasterAnimationFormat format) {
        var options = TerminalStoryAnimationOptions.Create().WithFramesPerSecond(2)
            .WithEndHold(1.23).WithLoop(true).WithMaximumFrames(4);
        var controls = ExportContainerInfo.Animation(Encode(ReadyStory().WithFinalPrompt(true), format, options), format);
        // Logical completion is .22s; the completed sample starts at .50s.
        // Samples at .50s, 1s, and 1.50s keep the cursor alive during the hold.
        Assert.Equal(new[] { 50, 50, 50, 23 }, controls.DelaysCentiseconds);
        Assert.Equal(123, controls.DelaysCentiseconds.Skip(1).Sum());
        Assert.Equal(0, controls.PlayCount);
        Assert.Throws<InvalidOperationException>(() => Encode(ReadyStory(), format, options.WithMaximumFrames(3)));
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void ZeroDurationStoryExportsOneCompletedFrame(RasterAnimationFormat format) {
        var story = TerminalStory.Create().WithWidth(480).WithPngOutputScale(1)
            .WithTiming(0, 42, 0).WithFinalPrompt(false).Blank();
        Assert.Equal(0, TerminalStoryLayout.Build(story).DurationSeconds);
        var options = TerminalStoryAnimationOptions.Create().WithFramesPerSecond(2).WithEndHold(0).WithLoop(false);
        var controls = ExportContainerInfo.Animation(Encode(story, format, options), format);
        Assert.Equal(new[] { 1 }, controls.DelaysCentiseconds);
        Assert.Equal(1, controls.PlayCount);
    }

    private static TerminalStory ReadyStory() => TerminalStory.Create().WithWidth(480).WithPngOutputScale(1)
        .WithTiming(0, 42, 0).WithFinalPrompt(false).Output("ready");

    private static byte[] Encode(TerminalStory story, RasterAnimationFormat format, TerminalStoryAnimationOptions options) =>
        format == RasterAnimationFormat.Gif ? story.ToGif(options) : story.ToApng(options);
}

using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal sealed class TerminalStoryAnimatedRasterRenderer {
    public byte[] Render(TerminalStory story, TerminalStoryAnimationOptions? options, AnimatedRasterFormat format) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var animation = options ?? TerminalStoryAnimationOptions.Create();
        var theme = story.Theme;
        var outlineFont = TrueTypeFont.TryLoadForFamily(theme.FontFamily, out _) ?? TrueTypeFont.TryLoadDefault();
        var tableFont = PngTerminalStoryRenderer.ResolveTableFont(theme, outlineFont);
        string PreserveText(string value) => TerminalPngTextPreserver.Preserve(value, outlineFont);
        string PreserveTableText(string value) => TerminalPngTextPreserver.Preserve(value, tableFont);
        var layout = TerminalStoryLayout.Build(story, PreserveText, outlineFont, PreserveTableText);
        var delayCentiseconds = QuantizedDelayCentiseconds(animation.FramesPerSecond);
        var totalSeconds = layout.DurationSeconds + animation.EndHoldSeconds;
        var frameCount = Math.Max(2, (int)Math.Ceiling(totalSeconds * 100 / delayCentiseconds) + 1);
        if (frameCount > animation.MaximumFrames) {
            throw new InvalidOperationException(
                "Animated terminal story requires " + frameCount +
                " frames. Lower the frame rate or story duration, or increase the maximum frame budget.");
        }
        var outputWidth = checked((long)layout.Width * animation.OutputScale);
        var outputHeight = checked((long)layout.Height * animation.OutputScale);
        var retainedFrameBytes = checked(
            outputWidth * outputHeight * 4 * frameCount +
            AnimatedRasterMemoryBudget.EncoderRetainedBytes(
                outputWidth,
                outputHeight,
                frameCount,
                format));
        if (retainedFrameBytes > AnimatedRasterMemoryBudget.MaximumRetainedBytes) {
            throw new InvalidOperationException(
                "Animated terminal story would retain " + retainedFrameBytes +
                " bytes of sampled frames and encoder buffers. Lower the output scale, frame rate, story size, or duration.");
        }

        var images = new List<RgbaImage>(frameCount);
        for (var index = 0; index < frameCount; index++) {
            var elapsed = index * delayCentiseconds / 100d;
            images.Add(PngTerminalStoryRenderer.RenderImage(story, layout, outlineFont, tableFont, animation.OutputScale, elapsed));
        }

        var frames = AnimatedRasterFrames.Create(images, delayCentiseconds, animation.Loop, format.GetDisplayName());
        return AnimatedRasterEncoder.Encode(format, frames);
    }

    internal static int QuantizedDelayCentiseconds(int framesPerSecond) =>
        Math.Max(1, (int)Math.Ceiling(100d / framesPerSecond));
}

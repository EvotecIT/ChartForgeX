using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal sealed class TerminalStoryAnimatedRasterRenderer {
    public byte[] Render(TerminalStory story, TerminalStoryAnimationOptions? options, AnimatedRasterFormat format) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var animation = options ?? TerminalStoryAnimationOptions.Create();
        var fonts = TerminalTabRasterFonts.Resolve(story);
        var layout = PngTerminalStoryRenderer.BuildLayout(story, fonts);
        var delayCentiseconds = QuantizedDelayCentiseconds(animation.FramesPerSecond);
        // Keep the regular cadence at or below the requested frame rate. Completion
        // can therefore land just after the logical end; measure the explicit hold
        // from that first completed sample, and retain it for at least one centisecond.
        var completionFrame = (int)Math.Ceiling(layout.DurationSeconds * 100 / delayCentiseconds);
        var completionCentiseconds = checked(completionFrame * delayCentiseconds);
        var holdCentiseconds = Math.Max(1, (int)Math.Round(animation.EndHoldSeconds * 100, MidpointRounding.AwayFromZero));
        var totalCentiseconds = checked(completionCentiseconds + holdCentiseconds);
        var frameCount = checked((totalCentiseconds + delayCentiseconds - 1) / delayCentiseconds);
        var finalDelayCentiseconds = totalCentiseconds - checked((frameCount - 1) * delayCentiseconds);
        if (frameCount > animation.MaximumFrames) {
            throw new InvalidOperationException(
                "Animated terminal story requires " + frameCount +
                " frames. Lower the frame rate or story duration, or increase the maximum frame budget.");
        }
        var outputWidth = checked((long)layout.Width * animation.OutputScale);
        var outputHeight = checked((long)layout.Height * animation.OutputScale);
        var renderWorkingBytes = AnimatedRasterMemoryBudget.RenderWorkingBytes(outputWidth, outputHeight, 2);
        var retainedFrameBytes = format == AnimatedRasterFormat.Apng
            ? checked(
                AnimatedRasterMemoryBudget.RgbaFramesRetainedBytes(outputWidth, outputHeight, 2) +
                AnimatedRasterMemoryBudget.ApngWorkingBytes(outputWidth, outputHeight) +
                renderWorkingBytes)
            : checked(
                AnimatedRasterMemoryBudget.RgbaFramesRetainedBytes(outputWidth, outputHeight, frameCount) +
                Math.Max(renderWorkingBytes, AnimatedRasterMemoryBudget.EncoderRetainedBytes(
                    outputWidth,
                    outputHeight,
                    frameCount,
                    format)));
        if (retainedFrameBytes > AnimatedRasterMemoryBudget.MaximumRetainedBytes) {
            throw new InvalidOperationException(
                "Animated terminal story would retain " + retainedFrameBytes +
                " bytes of sampled frames and encoder buffers. Lower the output scale, frame rate, story size, or duration.");
        }
        var maximumEncodedBytes = format == AnimatedRasterFormat.Apng
            ? AnimatedRasterMemoryBudget.MaximumStreamedApngBytes(retainedFrameBytes)
            : AnimatedRasterMemoryBudget.MaximumStreamedGifBytes(retainedFrameBytes);
        if (maximumEncodedBytes <= 0) {
            throw new InvalidOperationException(
                "Animated terminal story has no remaining bounded memory for encoded " +
                format.GetDisplayName() +
                " output. Lower the output scale or story size.");
        }
        if (format == AnimatedRasterFormat.Apng) {
            return AnimatedRasterEncoder.EncodeStreamedApng(
                checked((int)outputWidth),
                checked((int)outputHeight),
                frameCount,
                delayCentiseconds,
                finalDelayCentiseconds,
                animation.Loop,
                maximumEncodedBytes,
                index => PngTerminalStoryRenderer.RenderImage(
                    story,
                    layout,
                    fonts,
                    animation.OutputScale,
                    index * delayCentiseconds / 100d));
        }

        var images = new List<RgbaImage>(frameCount);
        for (var index = 0; index < frameCount; index++) {
            var elapsed = index * delayCentiseconds / 100d;
            images.Add(PngTerminalStoryRenderer.RenderImage(story, layout, fonts, animation.OutputScale, elapsed));
        }

        var frames = AnimatedRasterFrames.Create(
            images,
            delayCentiseconds,
            finalDelayCentiseconds,
            animation.Loop,
            format.GetDisplayName());
        return AnimatedRasterEncoder.EncodeBoundedGif(frames, maximumEncodedBytes);
    }

    internal static int QuantizedDelayCentiseconds(int framesPerSecond) =>
        Math.Max(1, (int)Math.Ceiling(100d / framesPerSecond));
}

using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Stories;

internal sealed class VisualStoryAnimatedRasterRenderer {
    public byte[] Render(VisualStory story, VisualStoryAnimationOptions? options, AnimatedRasterFormat format) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        var animation = options ?? VisualStoryAnimationOptions.Create();
        var totalSeconds = story.DurationSeconds + animation.EndHoldSeconds;
        var totalCentiseconds = Math.Max(
            2,
            (int)Math.Round(totalSeconds * 100, MidpointRounding.AwayFromZero));
        var delay = Math.Max(1, (int)Math.Ceiling(100d / animation.FramesPerSecond));
        var frameCount = Math.Max(2, (int)Math.Ceiling((double)totalCentiseconds / delay));
        if (checked((frameCount - 1) * delay) >= totalCentiseconds) {
            delay = Math.Max(1, (totalCentiseconds - 1) / (frameCount - 1));
        }
        var finalDelay = totalCentiseconds - checked((frameCount - 1) * delay);
        if (frameCount > animation.MaximumFrames) {
            throw new InvalidOperationException("Animated visual story requires " + frameCount + " frames. Lower the frame rate or duration, or increase the frame budget.");
        }
        EnsureEverySceneIsVisible(story, frameCount, delay, finalDelay, animation.TransitionSeconds);
        var outputWidth = checked((long)story.Width * animation.OutputScale);
        var outputHeight = checked((long)story.Height * animation.OutputScale);
        var frameBytes = checked(outputWidth * outputHeight * 4);
        var retainedScenes = checked(frameBytes * story.Scenes.Count);
        var fittedTerminalWorkingBytes = PngVisualStoryRenderer.MaximumFittedTerminalWorkingBytes(story, animation.OutputScale);
        var retained = format == AnimatedRasterFormat.Apng
            ? checked(
                retainedScenes +
                frameBytes * 2 +
                AnimatedRasterMemoryBudget.ApngWorkingBytes(outputWidth, outputHeight) +
                fittedTerminalWorkingBytes)
            : checked(
                frameBytes * frameCount +
                retainedScenes +
                AnimatedRasterMemoryBudget.EncoderRetainedBytes(
                    outputWidth,
                    outputHeight,
                    frameCount,
                    format) +
                fittedTerminalWorkingBytes);
        if (retained > AnimatedRasterMemoryBudget.MaximumRetainedBytes) {
            throw new InvalidOperationException("Animated visual story would retain " + retained + " bytes of sampled frames, cached scenes, fitted terminal buffers, and encoder buffers. Lower the size, scale, frame rate, duration, or scene count.");
        }
        var maximumEncodedBytes = format == AnimatedRasterFormat.Apng
            ? AnimatedRasterMemoryBudget.MaximumStreamedApngBytes(retained)
            : AnimatedRasterMemoryBudget.MaximumStreamedGifBytes(retained);
        if (maximumEncodedBytes <= 0) {
            throw new InvalidOperationException(
                "Animated visual story has no remaining bounded memory for encoded " +
                format.GetDisplayName() +
                " output. Lower the size, scale, or scene count.");
        }

        var scenes = new List<RgbaImage>(story.Scenes.Count);
        for (var index = 0; index < story.Scenes.Count; index++) {
            scenes.Add(PngVisualStoryRenderer.RenderScene(story, index, animation.OutputScale));
        }
        if (format == AnimatedRasterFormat.Apng) {
            return AnimatedRasterEncoder.EncodeStreamedApng(
                checked((int)outputWidth),
                checked((int)outputHeight),
                frameCount,
                delay,
                finalDelay,
                animation.Loop,
                maximumEncodedBytes,
                index => {
                    var elapsed = SampleElapsed(story, index, frameCount, delay);
                    return RenderFrame(story, scenes, elapsed, animation);
                });
        }
        var frames = new List<RgbaImage>(frameCount);
        for (var index = 0; index < frameCount; index++) {
            var elapsed = SampleElapsed(story, index, frameCount, delay);
            frames.Add(RenderFrame(story, scenes, elapsed, animation));
        }
        var retainedFrames = AnimatedRasterFrames.Create(
            frames,
            delay,
            finalDelay,
            animation.Loop,
            format.GetDisplayName());
        return AnimatedRasterEncoder.EncodeBoundedGif(retainedFrames, maximumEncodedBytes);
    }

    private static RgbaImage RenderFrame(VisualStory story, IReadOnlyList<RgbaImage> scenes, double elapsed, VisualStoryAnimationOptions options) {
        var sceneIndex = VisualStoryTimeline.FindScene(story, elapsed, out var timing);
        var current = scenes[sceneIndex];
        var remaining = timing.End - elapsed;
        var transitionSeconds = Math.Min(options.TransitionSeconds, story.Scenes[sceneIndex].DurationSeconds);
        if (sceneIndex + 1 < scenes.Count && transitionSeconds > 0 && remaining < transitionSeconds) {
            var progress = Math.Max(0, Math.Min(1, 1 - remaining / transitionSeconds));
            return CrossFade(
                current,
                scenes[sceneIndex + 1],
                progress);
        }
        return current;
    }

    internal static RgbaImage CrossFade(
        RgbaImage current,
        RgbaImage next,
        double progress) {
        if (current.Width != next.Width || current.Height != next.Height) {
            throw new ArgumentException("Cross-faded story scenes must have matching dimensions.", nameof(next));
        }
        progress = Math.Max(0, Math.Min(1, progress));
        var inverseProgress = 1 - progress;
        var pixels = new byte[current.Pixels.Length];
        for (var index = 0; index < pixels.Length; index += 4) {
            var currentAlpha = current.Pixels[index + 3];
            var nextAlpha = next.Pixels[index + 3];
            var alpha = Math.Round(currentAlpha * inverseProgress + nextAlpha * progress);
            pixels[index + 3] = (byte)alpha;
            if (alpha <= 0) continue;
            for (var channel = 0; channel < 3; channel++) {
                var currentPremultiplied = current.Pixels[index + channel] * currentAlpha / 255d;
                var nextPremultiplied = next.Pixels[index + channel] * nextAlpha / 255d;
                var premultiplied = currentPremultiplied * inverseProgress + nextPremultiplied * progress;
                pixels[index + channel] = (byte)Math.Max(
                    0,
                    Math.Min(255, Math.Round(premultiplied * 255d / alpha)));
            }
        }
        return new RgbaImage(current.Width, current.Height, pixels);
    }

    private static double SampleElapsed(VisualStory story, int index, int frameCount, int delay) {
        if (index == frameCount - 1) return story.DurationSeconds;
        return Math.Min(story.DurationSeconds, index * delay / 100d);
    }

    private static void EnsureEverySceneIsVisible(VisualStory story, int frameCount, int delay, int finalDelay, double transitionSeconds) {
        const double minimumVisibleOpacity = 0.5;
        var visibleOpacity = new double[story.Scenes.Count];
        var visibleCentiseconds = new double[story.Scenes.Count];
        for (var index = 0; index < frameCount; index++) {
            var frameDelay = index == frameCount - 1 ? finalDelay : delay;
            var elapsed = SampleElapsed(story, index, frameCount, delay);
            var sceneIndex = VisualStoryTimeline.FindScene(story, elapsed, out var timing);
            var remaining = timing.End - elapsed;
            var transition = Math.Min(transitionSeconds, story.Scenes[sceneIndex].DurationSeconds);
            if (sceneIndex + 1 < story.Scenes.Count && transition > 0 && remaining < transition) {
                var progress = Math.Max(0, Math.Min(1, 1 - remaining / transition));
                visibleOpacity[sceneIndex] = Math.Max(visibleOpacity[sceneIndex], 1 - progress);
                visibleOpacity[sceneIndex + 1] = Math.Max(visibleOpacity[sceneIndex + 1], progress);
                visibleCentiseconds[sceneIndex] += frameDelay * (1 - progress);
                visibleCentiseconds[sceneIndex + 1] += frameDelay * progress;
            } else {
                visibleOpacity[sceneIndex] = 1;
                visibleCentiseconds[sceneIndex] += frameDelay;
            }
        }
        for (var index = 0; index < visibleOpacity.Length; index++) {
            var requiredCentiseconds = Math.Max(
                1,
                Math.Min(delay, (int)Math.Round(story.Scenes[index].DurationSeconds * 100, MidpointRounding.AwayFromZero)));
            if (visibleOpacity[index] >= minimumVisibleOpacity &&
                visibleCentiseconds[index] >= requiredCentiseconds) {
                continue;
            }
            throw new InvalidOperationException(
                "Animated visual story frame timing cannot sample scene '" + story.Scenes[index].Id +
                "' for a meaningful visible duration. Increase the frame rate, shorten the transition, or lengthen the scene.");
        }
    }

}

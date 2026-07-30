using System;
using System.Collections.Generic;
using ChartForgeX.Composition;
using ChartForgeX.Raster;
using ChartForgeX.Primitives;

namespace ChartForgeX.Stories;

internal sealed class VisualStoryAnimatedRasterRenderer {
    private const long MaximumRetainedFrameBytes = 256L * 1024 * 1024;

    public byte[] Render(VisualStory story, VisualStoryAnimationOptions? options, AnimatedRasterFormat format) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        var animation = options ?? VisualStoryAnimationOptions.Create();
        var delay = Math.Max(1, (int)Math.Ceiling(100d / animation.FramesPerSecond));
        var totalSeconds = story.DurationSeconds + animation.EndHoldSeconds;
        var frameCount = Math.Max(2, (int)Math.Ceiling(totalSeconds * 100 / delay) + 1);
        if (frameCount > animation.MaximumFrames) {
            throw new InvalidOperationException("Animated visual story requires " + frameCount + " frames. Lower the frame rate or duration, or increase the frame budget.");
        }
        EnsureEverySceneIsVisible(story, frameCount, delay, animation.TransitionSeconds);
        var outputWidth = checked((long)story.Width * animation.OutputScale);
        var outputHeight = checked((long)story.Height * animation.OutputScale);
        var retainedFrames = checked(outputWidth * outputHeight * 4 * frameCount);
        var retainedScenes = checked(outputWidth * outputHeight * 4 * story.Scenes.Count);
        var retained = checked(retainedFrames + retainedScenes);
        if (retained > MaximumRetainedFrameBytes) {
            throw new InvalidOperationException("Animated visual story would retain " + retained + " bytes of sampled frames and cached scenes. Lower the size, scale, frame rate, duration, or scene count.");
        }

        var scenes = new List<RgbaImage>(story.Scenes.Count);
        for (var index = 0; index < story.Scenes.Count; index++) {
            scenes.Add(PngVisualStoryRenderer.RenderScene(story, index, animation.OutputScale));
        }
        var frames = new List<RgbaImage>(frameCount);
        for (var index = 0; index < frameCount; index++) {
            var elapsed = Math.Min(story.DurationSeconds, index * delay / 100d);
            frames.Add(RenderFrame(story, scenes, elapsed, animation));
        }
        return AnimatedRasterEncoder.Encode(format, AnimatedRasterFrames.Create(frames, delay, animation.Loop, format.GetDisplayName()));
    }

    private static RgbaImage RenderFrame(VisualStory story, IReadOnlyList<RgbaImage> scenes, double elapsed, VisualStoryAnimationOptions options) {
        var sceneIndex = FindScene(story, elapsed, out var sceneStart);
        var current = scenes[sceneIndex];
        var remaining = sceneStart + story.Scenes[sceneIndex].DurationSeconds - elapsed;
        var transitionSeconds = Math.Min(options.TransitionSeconds, story.Scenes[sceneIndex].DurationSeconds);
        if (sceneIndex + 1 < scenes.Count && transitionSeconds > 0 && remaining < transitionSeconds) {
            var progress = Math.Max(0, Math.Min(1, 1 - remaining / transitionSeconds));
            var blend = ImageComposition.CreateScaled(story.Width, story.Height, story.Theme.Background, options.OutputScale);
            blend.DrawImage(current, 0, 0, story.Width, story.Height, VisualCanvasImageFit.Stretch);
            blend.DrawImage(scenes[sceneIndex + 1], 0, 0, story.Width, story.Height, VisualCanvasImageFit.Stretch, progress);
            return blend.ToImage();
        }
        return current;
    }

    private static void EnsureEverySceneIsVisible(VisualStory story, int frameCount, int delay, double transitionSeconds) {
        const double minimumVisibleOpacity = 0.5;
        var visibleOpacity = new double[story.Scenes.Count];
        for (var index = 0; index < frameCount; index++) {
            var elapsed = Math.Min(story.DurationSeconds, index * delay / 100d);
            var sceneIndex = FindScene(story, elapsed, out var sceneStart);
            var remaining = sceneStart + story.Scenes[sceneIndex].DurationSeconds - elapsed;
            var transition = Math.Min(transitionSeconds, story.Scenes[sceneIndex].DurationSeconds);
            if (sceneIndex + 1 < story.Scenes.Count && transition > 0 && remaining < transition) {
                var progress = Math.Max(0, Math.Min(1, 1 - remaining / transition));
                visibleOpacity[sceneIndex] = Math.Max(visibleOpacity[sceneIndex], 1 - progress);
                visibleOpacity[sceneIndex + 1] = Math.Max(visibleOpacity[sceneIndex + 1], progress);
            } else {
                visibleOpacity[sceneIndex] = 1;
            }
        }
        for (var index = 0; index < visibleOpacity.Length; index++) {
            if (visibleOpacity[index] >= minimumVisibleOpacity) continue;
            throw new InvalidOperationException(
                "Animated visual story frame timing cannot sample scene '" + story.Scenes[index].Id +
                "' at a visible opacity. Increase the frame rate, shorten the transition, or lengthen the scene.");
        }
    }

    private static int FindScene(VisualStory story, double elapsed, out double sceneStart) {
        sceneStart = 0d;
        for (var index = 0; index < story.Scenes.Count; index++) {
            var sceneEnd = sceneStart + story.Scenes[index].DurationSeconds;
            if (elapsed < sceneEnd || index == story.Scenes.Count - 1) return index;
            sceneStart = sceneEnd;
        }
        return story.Scenes.Count - 1;
    }
}

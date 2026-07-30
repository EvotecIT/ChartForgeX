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
        var outputWidth = checked((long)story.Width * animation.OutputScale);
        var outputHeight = checked((long)story.Height * animation.OutputScale);
        var retained = checked(outputWidth * outputHeight * 4 * frameCount);
        if (retained > MaximumRetainedFrameBytes) {
            throw new InvalidOperationException("Animated visual story would retain " + retained + " bytes of sampled frames. Lower the size, scale, frame rate, or duration.");
        }

        var scenes = new List<RgbaImage>(story.Scenes.Count);
        for (var index = 0; index < story.Scenes.Count; index++) scenes.Add(PngVisualStoryRenderer.RenderScene(story, index));
        var frames = new List<RgbaImage>(frameCount);
        for (var index = 0; index < frameCount; index++) {
            var elapsed = Math.Min(story.DurationSeconds, index * delay / 100d);
            frames.Add(RenderFrame(story, scenes, elapsed, animation));
        }
        return AnimatedRasterEncoder.Encode(format, AnimatedRasterFrames.Create(frames, delay, animation.Loop, format.GetDisplayName()));
    }

    private static RgbaImage RenderFrame(VisualStory story, IReadOnlyList<RgbaImage> scenes, double elapsed, VisualStoryAnimationOptions options) {
        var sceneIndex = story.Scenes.Count - 1;
        var sceneStart = 0d;
        for (var index = 0; index < story.Scenes.Count; index++) {
            var sceneEnd = sceneStart + story.Scenes[index].DurationSeconds;
            if (elapsed < sceneEnd || index == story.Scenes.Count - 1) {
                sceneIndex = index;
                break;
            }
            sceneStart = sceneEnd;
        }
        var current = scenes[sceneIndex];
        RgbaImage logical;
        var remaining = sceneStart + story.Scenes[sceneIndex].DurationSeconds - elapsed;
        if (sceneIndex + 1 < scenes.Count && options.TransitionSeconds > 0 && remaining < options.TransitionSeconds) {
            var progress = Math.Max(0, Math.Min(1, 1 - remaining / options.TransitionSeconds));
            var blend = ImageComposition.Create(story.Width, story.Height, story.Theme.Background);
            blend.DrawImage(current, 0, 0, story.Width, story.Height, VisualCanvasImageFit.Stretch);
            blend.DrawImage(scenes[sceneIndex + 1], 0, 0, story.Width, story.Height, VisualCanvasImageFit.Stretch, progress);
            logical = blend.ToImage();
        } else {
            logical = current;
        }
        if (options.OutputScale == 1) return logical;
        var scaled = ImageComposition.Create(story.Width * options.OutputScale, story.Height * options.OutputScale, ChartColor.Transparent);
        scaled.DrawImage(logical, 0, 0, story.Width * options.OutputScale, story.Height * options.OutputScale, VisualCanvasImageFit.Stretch);
        return scaled.ToImage();
    }
}

using System;
using ChartForgeX.Raster;

namespace ChartForgeX.Stories;

internal sealed class VisualStoryAnimatedRasterRenderer {
    public byte[] Render(VisualStory story, VisualStoryAnimationOptions? options, AnimatedRasterFormat format) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var animation = options ?? VisualStoryAnimationOptions.Create();
        var prepared = story.Prepare(animation.Playback());
        return format == AnimatedRasterFormat.Gif ? prepared.ToGif(animation.Sampling()) : prepared.ToApng(animation.Sampling());
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

}

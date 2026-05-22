using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

internal sealed class AnimatedRasterFrames {
    private AnimatedRasterFrames(IReadOnlyList<RgbaImage> frames, int width, int height, int delayCentiseconds, bool loop) {
        Frames = frames;
        Width = width;
        Height = height;
        DelayCentiseconds = delayCentiseconds;
        Loop = loop;
    }

    public IReadOnlyList<RgbaImage> Frames { get; }
    public int Width { get; }
    public int Height { get; }
    public int DelayCentiseconds { get; }
    public bool Loop { get; }

    public static AnimatedRasterFrames Create(IReadOnlyList<RgbaImage> frames, int delayCentiseconds, bool loop, string formatName) {
        if (frames == null) throw new ArgumentNullException(nameof(frames));
        if (frames.Count == 0) throw new ArgumentException("Animated " + formatName + " export requires at least one frame.", nameof(frames));
        var width = frames[0].Width;
        var height = frames[0].Height;
        foreach (var frame in frames) {
            if (frame.Width != width || frame.Height != height) throw new ArgumentException("Animated " + formatName + " frames must have matching dimensions.", nameof(frames));
        }

        return new AnimatedRasterFrames(frames, width, height, ClampDelay(delayCentiseconds), loop);
    }

    private static int ClampDelay(int delayCentiseconds) =>
        Math.Max(1, Math.Min(65535, delayCentiseconds));
}

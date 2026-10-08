using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

internal readonly struct GifIndexedFrame {
    public GifIndexedFrame(int left, int top, int width, int height, byte[] pixels) {
        if (left < 0) throw new ArgumentOutOfRangeException(nameof(left), left, "GIF frame left offset must not be negative.");
        if (top < 0) throw new ArgumentOutOfRangeException(nameof(top), top, "GIF frame top offset must not be negative.");
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "GIF frame width must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "GIF frame height must be positive.");
        if (pixels == null) throw new ArgumentNullException(nameof(pixels));
        if (pixels.Length < checked(width * height)) throw new ArgumentException("Indexed GIF frame pixels are smaller than the requested frame dimensions.", nameof(pixels));
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Left { get; }
    public int Top { get; }
    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }
}

internal static class GifFrameOptimizer {
    public static IReadOnlyList<GifIndexedFrame> BuildFrames(IReadOnlyList<RgbaImage> frames, GifPalette palette) {
        if (frames == null) throw new ArgumentNullException(nameof(frames));
        if (palette == null) throw new ArgumentNullException(nameof(palette));
        var indexed = new List<GifIndexedFrame>(frames.Count);
        byte[]? previous = null;
        for (var i = 0; i < frames.Count; i++) {
            var current = GifPaletteQuantizer.Quantize(frames[i], palette);
            indexed.Add(palette.HasTransparency || i == 0 || previous == null ? FullFrame(frames[i], current) : DeltaFrame(frames[i], current, previous));
            previous = current;
        }

        return indexed;
    }

    private static GifIndexedFrame FullFrame(RgbaImage frame, byte[] pixels) =>
        new(0, 0, frame.Width, frame.Height, pixels);

    private static GifIndexedFrame DeltaFrame(RgbaImage frame, byte[] current, byte[] previous) {
        var left = frame.Width;
        var top = frame.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < frame.Height; y++) {
            var row = y * frame.Width;
            for (var x = 0; x < frame.Width; x++) {
                var index = row + x;
                if (current[index] == previous[index]) continue;
                if (x < left) left = x;
                if (x > right) right = x;
                if (y < top) top = y;
                if (y > bottom) bottom = y;
            }
        }

        if (right < left || bottom < top) return new GifIndexedFrame(0, 0, 1, 1, new[] { current[0] });
        var width = right - left + 1;
        var height = bottom - top + 1;
        var cropped = new byte[width * height];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(current, (top + y) * frame.Width + left, cropped, y * width, width);
        }

        return new GifIndexedFrame(left, top, width, height, cropped);
    }
}

using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

internal readonly struct RgbaFrameRect {
    public RgbaFrameRect(int left, int top, int width, int height, byte[] pixels) {
        if (left < 0) throw new ArgumentOutOfRangeException(nameof(left), left, "RGBA frame left offset must not be negative.");
        if (top < 0) throw new ArgumentOutOfRangeException(nameof(top), top, "RGBA frame top offset must not be negative.");
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "RGBA frame width must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "RGBA frame height must be positive.");
        if (pixels == null) throw new ArgumentNullException(nameof(pixels));
        if (pixels.Length < checked(width * height * 4)) throw new ArgumentException("RGBA frame pixels are smaller than the requested frame dimensions.", nameof(pixels));
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

internal static class RgbaFrameOptimizer {
    public static IReadOnlyList<RgbaFrameRect> BuildFrames(IReadOnlyList<RgbaImage> frames) {
        if (frames == null) throw new ArgumentNullException(nameof(frames));
        var optimized = new List<RgbaFrameRect>(frames.Count);
        RgbaImage? previous = null;
        for (var i = 0; i < frames.Count; i++) {
            optimized.Add(i == 0 || previous == null ? FullFrame(frames[i]) : DeltaFrame(frames[i], previous.Value));
            previous = frames[i];
        }

        return optimized;
    }

    private static RgbaFrameRect FullFrame(RgbaImage frame) =>
        new(0, 0, frame.Width, frame.Height, frame.Pixels);

    private static RgbaFrameRect DeltaFrame(RgbaImage current, RgbaImage previous) {
        var left = current.Width;
        var top = current.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < current.Height; y++) {
            var row = y * current.Width * 4;
            for (var x = 0; x < current.Width; x++) {
                var offset = row + x * 4;
                if (current.Pixels[offset] == previous.Pixels[offset] &&
                    current.Pixels[offset + 1] == previous.Pixels[offset + 1] &&
                    current.Pixels[offset + 2] == previous.Pixels[offset + 2] &&
                    current.Pixels[offset + 3] == previous.Pixels[offset + 3]) continue;
                if (x < left) left = x;
                if (x > right) right = x;
                if (y < top) top = y;
                if (y > bottom) bottom = y;
            }
        }

        if (right < left || bottom < top) return new RgbaFrameRect(0, 0, 1, 1, new[] { current.Pixels[0], current.Pixels[1], current.Pixels[2], current.Pixels[3] });
        var width = right - left + 1;
        var height = bottom - top + 1;
        var cropped = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(current.Pixels, ((top + y) * current.Width + left) * 4, cropped, y * width * 4, width * 4);
        }

        return new RgbaFrameRect(left, top, width, height, cropped);
    }
}

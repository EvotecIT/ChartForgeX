using System;

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
    internal static bool SameColor(byte current, byte previous, GifPalette palette, GifPalette previousPalette) {
        if (ReferenceEquals(palette, previousPalette)) return current == previous;
        var transparent = palette.HasTransparency && current == palette.TransparentIndex;
        var wasTransparent = previousPalette.HasTransparency && previous == previousPalette.TransparentIndex;
        if (transparent || wasTransparent) return transparent == wasTransparent;
        for (var channel = 0; channel < 3; channel++) {
            if (palette.Colors[current * 3 + channel] != previousPalette.Colors[previous * 3 + channel]) return false;
        }
        return true;
    }

    internal static GifIndexedFrame FullFrame(RgbaImage frame, byte[] pixels) =>
        new(0, 0, frame.Width, frame.Height, pixels);

    internal static GifIndexedFrame DeltaFrame(RgbaImage frame, byte[] current, byte[] previous, GifPalette? palette = null, GifPalette? previousPalette = null) {
        var left = frame.Width;
        var top = frame.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < frame.Height; y++) {
            var row = y * frame.Width;
            for (var x = 0; x < frame.Width; x++) {
                var index = row + x;
                if (palette == null ? current[index] == previous[index] : SameColor(current[index], previous[index], palette, previousPalette!)) continue;
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

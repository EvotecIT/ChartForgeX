using System;

namespace ChartForgeX.Raster;

internal readonly struct RgbaImage {
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Pixels;

    public RgbaImage(int width, int height, byte[] pixels) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Image width must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Image height must be positive.");
        if (pixels == null) throw new ArgumentNullException(nameof(pixels));
        var required = checked(width * height * 4);
        if (pixels.Length < required) throw new ArgumentException("RGBA pixel buffer is smaller than the requested image dimensions.", nameof(pixels));
        Width = width;
        Height = height;
        Pixels = pixels;
    }
}

internal static class RgbaCanvasImageExtensions {
    public static RgbaImage ToImage(this RgbaCanvas canvas) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        return new RgbaImage(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());
    }
}

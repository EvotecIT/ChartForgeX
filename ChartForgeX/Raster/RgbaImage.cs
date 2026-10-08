using System;

namespace ChartForgeX.Raster;

/// <summary>Represents a dependency-free 32-bit RGBA raster image.</summary>
/// <remarks>This value borrows the supplied mutable pixel array without copying it. Keep the array alive and avoid changing it while a renderer or encoder uses the image. Composition and typed visual-canvas image entrypoints document when they create independent snapshots.</remarks>
public readonly struct RgbaImage {
    /// <summary>Gets the image width in pixels.</summary>
    public readonly int Width;
    /// <summary>Gets the image height in pixels.</summary>
    public readonly int Height;
    /// <summary>Gets the image pixels as RGBA bytes, row-major from top-left.</summary>
    public readonly byte[] Pixels;

    /// <summary>Initializes an RGBA raster image.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixels">Borrowed mutable RGBA pixels, row-major from top-left. The constructor does not copy the array.</param>
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

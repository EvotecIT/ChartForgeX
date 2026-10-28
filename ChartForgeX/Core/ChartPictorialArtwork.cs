using System;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Core;

/// <summary>One immutable captured pictorial image, reused by native SVG and PNG symbol placements.</summary>
/// <remarks>Artwork keeps its own colors. It is fitted centrally within each symbol slot; partial values clip the fitted image rectangle.
/// SVG input is rasterized once at the caller's dimensions, not retained as vector markup. No URL or file is resolved.</remarks>
public sealed class ChartPictorialArtwork {
    private const int MaximumDimension = 8192;
    private const long MaximumPixels = 16_777_216;
    private ChartPictorialArtwork(RgbaImage image) { Resource = new VisualSceneImageData(image); }
    internal VisualSceneImageData Resource { get; }
    /// <summary>Gets the captured source width in pixels.</summary>
    public int Width => Resource.Image.Width;
    /// <summary>Gets the captured source height in pixels.</summary>
    public int Height => Resource.Image.Height;

    /// <summary>Captures caller-owned RGBA pixels without retaining their mutable array.</summary>
    /// <param name="image">The complete source image, read only during this call.</param>
    /// <returns>Independent immutable artwork.</returns>
    /// <remarks>Dimensions are limited to 8192 per side and 16,777,216 pixels. The existing raster allocation guard also applies.</remarks>
    public static ChartPictorialArtwork FromImage(RgbaImage image) {
        ValidateDimensions(image.Width, image.Height);
        return new ChartPictorialArtwork(image);
    }

    /// <summary>Captures the strict supported SVG subset as pixels at explicit dimensions.</summary>
    /// <param name="svg">A complete SVG document. The shared parser prohibits DTDs and bounds document size/depth.</param>
    /// <param name="width">The captured raster width, independent of the SVG's declared viewport.</param>
    /// <param name="height">The captured raster height.</param>
    /// <returns>Independent image artwork. Original SVG markup is never embedded in the exported document.</returns>
    /// <remarks>The same image dimension limits as FromImage apply before parsing or raster allocation. Unsupported visible SVG content is rejected by strict rasterization.</remarks>
    public static ChartPictorialArtwork FromSvg(string svg, int width, int height) {
        ValidateDimensions(width, height);
        return new ChartPictorialArtwork(SvgRasterizer.Rasterize(svg, width, height, strict: true).Image);
    }

    private static void ValidateDimensions(int width, int height) {
        if (width <= 0 || width > MaximumDimension) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0 || height > MaximumDimension) throw new ArgumentOutOfRangeException(nameof(height));
        if ((long)width * height > MaximumPixels) throw new ArgumentOutOfRangeException(nameof(width), "Pictorial artwork exceeds 16,777,216 source pixels.");
        _ = RasterAllocationGuard.Calculate(width, height, 1, 1);
    }
}

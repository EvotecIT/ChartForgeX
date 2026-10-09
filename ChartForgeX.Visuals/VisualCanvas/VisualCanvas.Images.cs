using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvas {
    /// <summary>Adds a bitmap layer from an independent snapshot of the supplied RGBA image.</summary>
    /// <remarks>The canvas copies the source pixels during this call. Later changes to the caller's buffer do not change either output. SVG embeds a PNG encoded from the same pixels used by PNG output; no image resource is fetched.</remarks>
    public VisualCanvas AddImage(RgbaImage image, double x, double y, double width, double height, double opacity = 1, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch) {
        ValidateEnum(fit, nameof(fit));
        var layer = new VisualCanvasImageLayer(x, y, width, height) { Opacity = opacity, Fit = fit };
        var snapshot = VisualCanvasImagePixels.Snapshot(image);
        layer.Rgba = snapshot.Pixels;
        layer.SourceWidth = snapshot.Width;
        layer.SourceHeight = snapshot.Height;
        return AddLayer(layer);
    }

    /// <summary>Adds a bitmap snapshot using anchor-based placement.</summary>
    /// <remarks>The canvas copies the source pixels during this call and uses that snapshot for both SVG and PNG output.</remarks>
    public VisualCanvas AddImage(RgbaImage image, VisualCanvasPlacement placement, double width, double height, double opacity = 1, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch) {
        var bounds = ResolvePlacement(placement, width, height);
        return AddImage(image, bounds.X, bounds.Y, bounds.Width, bounds.Height, opacity, fit);
    }

    /// <summary>Adds a central badge whose bitmap content is an independent RGBA snapshot.</summary>
    /// <remarks>The canvas copies the source pixels during this call and uses that snapshot for both SVG and PNG output.</remarks>
    public VisualCanvas AddHeroBadge(RgbaImage image, double x, double y, double width, double height, string symbol = "", ChartColor? accent = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Contain, double padding = 10, double opacity = 1) {
        ValidateEnum(fit, nameof(fit));
        var layer = new VisualCanvasHeroBadgeLayer(x, y, width, height, symbol) { AccentOverride = accent, ImageFit = fit, ImagePadding = padding, ImageOpacity = opacity };
        var snapshot = VisualCanvasImagePixels.Snapshot(image);
        layer.ImageRgba = snapshot.Pixels;
        layer.ImageSourceWidth = snapshot.Width;
        layer.ImageSourceHeight = snapshot.Height;
        return AddLayer(layer);
    }

    /// <summary>Adds a central badge with bitmap snapshot content using anchor-based placement.</summary>
    /// <remarks>The canvas copies the source pixels during this call and uses that snapshot for both SVG and PNG output.</remarks>
    public VisualCanvas AddHeroBadge(RgbaImage image, VisualCanvasPlacement placement, double width, double height, string symbol = "", ChartColor? accent = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Contain, double padding = 10, double opacity = 1) {
        var bounds = ResolvePlacement(placement, width, height);
        return AddHeroBadge(image, bounds.X, bounds.Y, bounds.Width, bounds.Height, symbol, accent, fit, padding, opacity);
    }
}

internal static class VisualCanvasImagePixels {
    internal static RgbaImage Snapshot(RgbaImage image) {
        var bytes = Validate(image.Width, image.Height, image.Pixels);
        var copy = new byte[bytes];
        Buffer.BlockCopy(image.Pixels, 0, copy, 0, bytes);
        return new RgbaImage(image.Width, image.Height, copy);
    }

    internal static string EmbeddedPng(int width, int height, byte[] pixels) {
        Validate(width, height, pixels);
        var encoded = RasterImageEncoder.Encode(new RgbaImage(width, height, pixels), RasterImageFormat.Png);
        return "data:image/png;base64," + Convert.ToBase64String(encoded);
    }

    private static int Validate(int width, int height, byte[] pixels) {
        var allocation = RasterAllocationGuard.Calculate(width, height, 1, 1);
        if (pixels == null) throw new ArgumentNullException(nameof(pixels));
        if (pixels.Length < allocation.ByteCount) throw new ArgumentException("RGBA pixel buffer is smaller than the requested image dimensions.", nameof(pixels));
        return allocation.ByteCount;
    }
}

using System;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Composition;

/// <summary>
/// Dependency-free RGBA image composition surface for wallpapers, previews, reports, and reusable renderer hosts.
/// </summary>
public sealed class ImageComposition {
    private readonly RgbaCanvas _canvas;

    private ImageComposition(RgbaCanvas canvas) {
        _canvas = canvas;
    }

    /// <summary>Gets the composition width in pixels.</summary>
    public int Width => _canvas.Width;

    /// <summary>Gets the composition height in pixels.</summary>
    public int Height => _canvas.Height;

    /// <summary>Creates a new composition with the requested background.</summary>
    public static ImageComposition Create(int width, int height, ChartColor background) {
        var canvas = new RgbaCanvas(width, height, 1, null, 1);
        canvas.Clear(background);
        return new ImageComposition(canvas);
    }

    /// <summary>Creates a transparent composition.</summary>
    public static ImageComposition CreateTransparent(int width, int height) => Create(width, height, ChartColor.Transparent);

    /// <summary>Creates a composition initialized with decoded image pixels.</summary>
    public static ImageComposition FromImage(RgbaImage image) {
        var composition = CreateTransparent(image.Width, image.Height);
        composition.DrawImage(image, 0, 0, image.Width, image.Height);
        return composition;
    }

    /// <summary>Loads image bytes and creates a composition with the decoded pixels.</summary>
    public static ImageComposition FromBytes(byte[] data) => FromImage(RasterImageDecoder.Decode(data));

    /// <summary>Loads an image file and creates a composition with the decoded pixels.</summary>
    public static ImageComposition FromFile(string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        return FromBytes(File.ReadAllBytes(path));
    }

    /// <summary>Fills the whole composition with a color.</summary>
    public ImageComposition Clear(ChartColor color) {
        _canvas.Clear(color);
        return this;
    }

    /// <summary>Draws a filled rectangle.</summary>
    public ImageComposition FillRectangle(double x, double y, double width, double height, ChartColor color) {
        ValidateRect(x, y, width, height);
        _canvas.FillRect(x, y, width, height, color);
        return this;
    }

    /// <summary>Draws a filled rounded rectangle.</summary>
    public ImageComposition FillRoundedRectangle(double x, double y, double width, double height, double radius, ChartColor color) {
        ValidateRect(x, y, width, height);
        ValidateFinite(radius, nameof(radius));
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius cannot be negative.");
        _canvas.FillRoundedRect(x, y, width, height, radius, color);
        return this;
    }

    /// <summary>Draws text inside a fixed-width region.</summary>
    public ImageComposition DrawText(double x, double y, double width, string text, double fontSize, ChartColor color, VisualCanvasTextAlignment alignment = VisualCanvasTextAlignment.Left, bool emphasized = false) {
        ValidateFinite(x, nameof(x));
        ValidateFinite(y, nameof(y));
        ValidatePositive(width, nameof(width));
        ValidatePositive(fontSize, nameof(fontSize));
        VisualCanvas.ValidateEnum(alignment, nameof(alignment));
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (text.Length == 0 || color.A == 0) return this;
        var textWidth = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        var drawX = ResolveAlignedX(x, width, textWidth, alignment);
        if (emphasized) _canvas.DrawTextEmphasized(drawX, y, text, color, fontSize);
        else _canvas.DrawText(drawX, y, text, color, fontSize);
        return this;
    }

    /// <summary>Draws an image into an explicit destination rectangle.</summary>
    public ImageComposition DrawImage(RgbaImage image, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        ValidateRect(x, y, width, height);
        VisualCanvas.ValidateEnum(fit, nameof(fit));
        var pixels = ApplyOpacity(image.Pixels, opacity);
        DrawFittedImage(image.Width, image.Height, pixels, (int)Math.Round(x), (int)Math.Round(y), Math.Max(1, (int)Math.Round(width)), Math.Max(1, (int)Math.Round(height)), fit);
        return this;
    }

    /// <summary>Draws an image using anchor-based placement.</summary>
    public ImageComposition DrawImage(RgbaImage image, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        var bounds = placement.Resolve(Width, Height, width, height);
        return DrawImage(image, bounds.X, bounds.Y, bounds.Width, bounds.Height, fit, opacity);
    }

    /// <summary>Decodes and draws image bytes into an explicit destination rectangle.</summary>
    public ImageComposition DrawImageBytes(byte[] data, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return DrawImage(RasterImageDecoder.Decode(data), x, y, width, height, fit, opacity);
    }

    /// <summary>Decodes and draws image bytes using anchor-based placement.</summary>
    public ImageComposition DrawImageBytes(byte[] data, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return DrawImage(RasterImageDecoder.Decode(data), placement, width, height, fit, opacity);
    }

    /// <summary>Decodes and draws an image file into an explicit destination rectangle.</summary>
    public ImageComposition DrawImageFile(string path, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        return DrawImageBytes(File.ReadAllBytes(path), x, y, width, height, fit, opacity);
    }

    /// <summary>Decodes and draws an image file using anchor-based placement.</summary>
    public ImageComposition DrawImageFile(string path, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        return DrawImageBytes(File.ReadAllBytes(path), placement, width, height, fit, opacity);
    }

    /// <summary>Draws a chart layer.</summary>
    public ImageComposition DrawChart(Chart chart, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return DrawImage(RasterRenderer.RenderImage(chart), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a chart layer using anchor-based placement.</summary>
    public ImageComposition DrawChart(Chart chart, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return DrawImage(RasterRenderer.RenderImage(chart), placement, width, height, fit, opacity);
    }

    /// <summary>Draws a chart grid layer.</summary>
    public ImageComposition DrawChartGrid(ChartGrid grid, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return DrawImage(RasterRenderer.RenderImage(grid), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a chart grid layer using anchor-based placement.</summary>
    public ImageComposition DrawChartGrid(ChartGrid grid, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return DrawImage(RasterRenderer.RenderImage(grid), placement, width, height, fit, opacity);
    }

    /// <summary>Draws a visual block layer.</summary>
    public ImageComposition DrawVisualBlock(IVisualBlock block, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        return DrawImage(RasterRenderer.RenderImage(block), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a visual block layer using anchor-based placement.</summary>
    public ImageComposition DrawVisualBlock(IVisualBlock block, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        return DrawImage(RasterRenderer.RenderImage(block), placement, width, height, fit, opacity);
    }

    /// <summary>Draws a visual grid layer.</summary>
    public ImageComposition DrawVisualGrid(VisualGrid grid, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return DrawImage(RasterRenderer.RenderImage(grid), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a visual grid layer using anchor-based placement.</summary>
    public ImageComposition DrawVisualGrid(VisualGrid grid, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return DrawImage(RasterRenderer.RenderImage(grid), placement, width, height, fit, opacity);
    }

    /// <summary>Draws a visual canvas layer.</summary>
    public ImageComposition DrawVisualCanvas(VisualCanvas canvas, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        return DrawImage(new PngVisualCanvasRenderer().RenderImage(canvas), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a visual canvas layer using anchor-based placement.</summary>
    public ImageComposition DrawVisualCanvas(VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        return DrawImage(new PngVisualCanvasRenderer().RenderImage(canvas), placement, width, height, fit, opacity);
    }

    /// <summary>Draws a topology chart layer.</summary>
    public ImageComposition DrawTopology(TopologyChart topology, double x, double y, double width, double height, TopologyRenderOptions? options = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (topology == null) throw new ArgumentNullException(nameof(topology));
        return DrawImage(TopologyRasterRenderer.RenderImage(topology, options), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a topology chart layer using anchor-based placement.</summary>
    public ImageComposition DrawTopology(TopologyChart topology, VisualCanvasPlacement placement, double width, double height, TopologyRenderOptions? options = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (topology == null) throw new ArgumentNullException(nameof(topology));
        return DrawImage(TopologyRasterRenderer.RenderImage(topology, options), placement, width, height, fit, opacity);
    }

    /// <summary>Returns a copy of the current pixels.</summary>
    public RgbaImage ToImage() => _canvas.ToImage();

    /// <summary>Encodes the composition to PNG bytes.</summary>
    public byte[] ToPng() => PngWriter.WriteRgba(ToImage());

    /// <summary>Encodes the composition to JPEG bytes.</summary>
    public byte[] ToJpeg(RasterImageOptions? options = null) => RasterImageEncoder.Encode(ToImage(), RasterImageFormat.Jpeg, options);

    /// <summary>Encodes the composition to a dependency-free raster format.</summary>
    public byte[] ToRasterImage(RasterImageFormat format, RasterImageOptions? options = null) => RasterImageEncoder.Encode(ToImage(), format, options);

    /// <summary>Saves the composition to a raster file using the output extension to choose the format.</summary>
    public void Save(string path, RasterImageOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, ToRasterImage(RasterImageFormatExtensions.FromFileExtension(path), options));
    }

    private void DrawFittedImage(int sourceWidth, int sourceHeight, byte[] rgba, int x, int y, int width, int height, VisualCanvasImageFit fit) {
        switch (fit) {
            case VisualCanvasImageFit.Contain:
                DrawContained(sourceWidth, sourceHeight, rgba, x, y, width, height);
                return;
            case VisualCanvasImageFit.Cover:
                DrawCovered(sourceWidth, sourceHeight, rgba, x, y, width, height);
                return;
            case VisualCanvasImageFit.Center:
                DrawCentered(sourceWidth, sourceHeight, rgba, x, y, width, height);
                return;
            case VisualCanvasImageFit.Tile:
                DrawTiled(sourceWidth, sourceHeight, rgba, x, y, width, height);
                return;
            default:
                _canvas.DrawImageScaled(x, y, width, height, sourceWidth, sourceHeight, rgba);
                return;
        }
    }

    private void DrawContained(int sourceWidth, int sourceHeight, byte[] rgba, int x, int y, int width, int height) {
        var scale = Math.Min(width / (double)sourceWidth, height / (double)sourceHeight);
        var drawWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        var drawHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
        _canvas.DrawImageScaled(x + (width - drawWidth) / 2, y + (height - drawHeight) / 2, drawWidth, drawHeight, sourceWidth, sourceHeight, rgba);
    }

    private void DrawCovered(int sourceWidth, int sourceHeight, byte[] rgba, int x, int y, int width, int height) {
        var scale = Math.Max(width / (double)sourceWidth, height / (double)sourceHeight);
        var cropWidth = Math.Min(sourceWidth, width / scale);
        var cropHeight = Math.Min(sourceHeight, height / scale);
        _canvas.DrawImageScaled(x, y, width, height, sourceWidth, sourceHeight, rgba, (sourceWidth - cropWidth) / 2, (sourceHeight - cropHeight) / 2, cropWidth, cropHeight);
    }

    private void DrawCentered(int sourceWidth, int sourceHeight, byte[] rgba, int x, int y, int width, int height) {
        var centeredX = x + (width - sourceWidth) / 2;
        var centeredY = y + (height - sourceHeight) / 2;
        var drawX = Math.Max(x, centeredX);
        var drawY = Math.Max(y, centeredY);
        var drawRight = Math.Min(x + width, centeredX + sourceWidth);
        var drawBottom = Math.Min(y + height, centeredY + sourceHeight);
        var drawWidth = drawRight - drawX;
        var drawHeight = drawBottom - drawY;
        if (drawWidth <= 0 || drawHeight <= 0) return;
        _canvas.DrawImageScaled(drawX, drawY, drawWidth, drawHeight, sourceWidth, sourceHeight, rgba, drawX - centeredX, drawY - centeredY, drawWidth, drawHeight);
    }

    private void DrawTiled(int sourceWidth, int sourceHeight, byte[] rgba, int x, int y, int width, int height) {
        for (var tileY = y; tileY < y + height; tileY += sourceHeight) {
            var drawHeight = Math.Min(sourceHeight, y + height - tileY);
            for (var tileX = x; tileX < x + width; tileX += sourceWidth) {
                var drawWidth = Math.Min(sourceWidth, x + width - tileX);
                _canvas.DrawImageScaled(tileX, tileY, drawWidth, drawHeight, sourceWidth, sourceHeight, rgba, 0, 0, drawWidth, drawHeight);
            }
        }
    }

    private static byte[] ApplyOpacity(byte[] pixels, double opacity) {
        ValidateFinite(opacity, nameof(opacity));
        if (opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity), opacity, "Opacity must be between 0 and 1.");
        if (opacity >= 0.999) return pixels;
        var copy = new byte[pixels.Length];
        Buffer.BlockCopy(pixels, 0, copy, 0, pixels.Length);
        for (var i = 3; i < copy.Length; i += 4) copy[i] = (byte)Math.Round(copy[i] * opacity);
        return copy;
    }

    private static double ResolveAlignedX(double x, double width, double textWidth, VisualCanvasTextAlignment alignment) {
        switch (alignment) {
            case VisualCanvasTextAlignment.Center:
                return x + (width - textWidth) / 2;
            case VisualCanvasTextAlignment.Right:
                return x + width - textWidth;
            default:
                return x;
        }
    }

    private static void ValidateRect(double x, double y, double width, double height) {
        ValidateFinite(x, nameof(x));
        ValidateFinite(y, nameof(y));
        ValidatePositive(width, nameof(width));
        ValidatePositive(height, nameof(height));
    }

    private static void ValidatePositive(double value, string parameterName) {
        ValidateFinite(value, parameterName);
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
    }

    private static void ValidateFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }
}

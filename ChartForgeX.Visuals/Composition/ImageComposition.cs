using System;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Composition;

/// <summary>
/// Dependency-free RGBA image composition surface for wallpapers, previews, reports, and reusable renderer hosts.
/// </summary>
public sealed partial class ImageComposition {
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

    internal static ImageComposition CreateScaled(int width, int height, ChartColor background, int outputScale) {
        var canvas = new RgbaCanvas(width, height, 1, null, outputScale);
        canvas.Clear(background);
        return new ImageComposition(canvas);
    }

    /// <summary>Creates a transparent composition.</summary>
    public static ImageComposition CreateTransparent(int width, int height) => Create(width, height, ChartColor.Transparent);

    /// <summary>Creates a composition initialized with an independent copy of the supplied image pixels.</summary>
    public static ImageComposition FromImage(RgbaImage image) {
        var composition = CreateTransparent(image.Width, image.Height);
        // The canvas starts transparent, so the pixels can be copied instead of blended one by one.
        Buffer.BlockCopy(image.Pixels, 0, composition._canvas.Pixels, 0, image.Width * image.Height * 4);
        return composition;
    }

    /// <summary>Loads image bytes within the requested decode limits and creates a composition with the decoded pixels.</summary>
    public static ImageComposition FromBytes(byte[] data, RasterDecodeOptions? options = null) => FromImage(RasterImageDecoder.Decode(data, options));

    /// <summary>Attempts to load image bytes and create a composition with the decoded pixels.</summary>
    public static bool TryFromBytes(byte[]? data, out ImageComposition? composition, RasterDecodeOptions? options = null) {
        composition = null;
        if (!RasterImageDecoder.TryDecode(data, options, out var image)) return false;
        composition = FromImage(image);
        return true;
    }

    /// <summary>Reads an image file within the requested encoded-byte and pixel limits and creates a composition with the decoded pixels.</summary>
    public static ImageComposition FromFile(string path, RasterDecodeOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        return FromImage(RasterImageDecoder.Read(path, options));
    }

    /// <summary>Attempts to load an image file and create a composition with the decoded pixels.</summary>
    public static bool TryFromFile(string? path, out ImageComposition? composition, RasterDecodeOptions? options = null) {
        composition = null;
        if (!RasterImageDecoder.TryRead(path, options, out var image)) return false;
        composition = FromImage(image);
        return true;
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

    /// <summary>Draws a rectangle outline.</summary>
    public ImageComposition StrokeRectangle(double x, double y, double width, double height, ChartColor color, double thickness = 1) {
        ValidateRect(x, y, width, height);
        ValidatePositive(thickness, nameof(thickness));
        _canvas.StrokeRect(x, y, width, height, color, thickness);
        return this;
    }

    /// <summary>Draws a rounded rectangle outline.</summary>
    public ImageComposition StrokeRoundedRectangle(double x, double y, double width, double height, double radius, ChartColor color, double thickness = 1) {
        ValidateRect(x, y, width, height);
        ValidateFinite(radius, nameof(radius));
        ValidatePositive(thickness, nameof(thickness));
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius cannot be negative.");
        _canvas.StrokeRoundedRect(x, y, width, height, radius, color, thickness);
        return this;
    }

    /// <summary>Draws a line segment.</summary>
    public ImageComposition DrawLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness = 1) {
        ValidateFinite(x0, nameof(x0));
        ValidateFinite(y0, nameof(y0));
        ValidateFinite(x1, nameof(x1));
        ValidateFinite(y1, nameof(y1));
        ValidatePositive(thickness, nameof(thickness));
        _canvas.DrawLine(x0, y0, x1, y1, color, thickness);
        return this;
    }

    /// <summary>Draws a dashed line segment.</summary>
    public ImageComposition DrawDashedLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness = 1, double dash = 6, double gap = 5) {
        ValidateFinite(x0, nameof(x0));
        ValidateFinite(y0, nameof(y0));
        ValidateFinite(x1, nameof(x1));
        ValidateFinite(y1, nameof(y1));
        ValidatePositive(thickness, nameof(thickness));
        ValidatePositive(dash, nameof(dash));
        ValidatePositive(gap, nameof(gap));
        _canvas.DrawDashedLine(x0, y0, x1, y1, color, thickness, dash, gap);
        return this;
    }

    /// <summary>Draws a leader line and rounded text box callout for reusable image annotations.</summary>
    public ImageComposition DrawCallout(double anchorX, double anchorY, double boxX, double boxY, double boxWidth, double boxHeight, string text, double fontSize, ChartColor lineColor, ChartColor fillColor, ChartColor textColor, double radius = 4, double thickness = 1, double padding = 4) {
        ValidateFinite(anchorX, nameof(anchorX));
        ValidateFinite(anchorY, nameof(anchorY));
        ValidateRect(boxX, boxY, boxWidth, boxHeight);
        ValidatePositive(fontSize, nameof(fontSize));
        ValidateFinite(radius, nameof(radius));
        ValidatePositive(thickness, nameof(thickness));
        ValidateNonNegative(padding, nameof(padding));
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius cannot be negative.");
        if (text == null) throw new ArgumentNullException(nameof(text));

        var leader = NearestPointOnRectangle(anchorX, anchorY, boxX, boxY, boxWidth, boxHeight);
        _canvas.DrawLine(anchorX, anchorY, leader.X, leader.Y, lineColor, thickness);
        _canvas.FillRoundedRect(boxX, boxY, boxWidth, boxHeight, radius, fillColor);
        _canvas.StrokeRoundedRect(boxX, boxY, boxWidth, boxHeight, radius, lineColor, thickness);
        return DrawText(boxX + padding, boxY + padding, Math.Max(1, boxWidth - padding * 2), text, fontSize, textColor);
    }

    /// <summary>Draws text inside a fixed-width region.</summary>
    public ImageComposition DrawText(double x, double y, double width, string text, double fontSize, ChartColor color, TextAlignment alignment = TextAlignment.Left, bool emphasized = false) {
        VisualCanvas.ValidateEnum(alignment, nameof(alignment));
        var style = TextStyle.Create(fontSize, color);
        style.Alignment = alignment;
        style.Font.Weight = emphasized ? 700 : 400;
        return DrawText(x, y, width, text, style, TextWrapMode.NoWrap, null, TextTrimming.None);
    }

    /// <summary>Draws measured and optionally wrapped text with the shared dependency-free typography engine.</summary>
    public ImageComposition DrawText(double x, double y, double width, string text, TextStyle style, TextWrapMode wrapMode = TextWrapMode.Word, int? maximumLines = null, TextTrimming trimming = TextTrimming.Ellipsis) {
        ValidateFinite(x, nameof(x));
        ValidateFinite(y, nameof(y));
        ValidatePositive(width, nameof(width));
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (style == null) throw new ArgumentNullException(nameof(style));
        if (text.Length == 0 || style.Color.A == 0) return this;

        _canvas.DrawStyledText(x, y, width, text, style, wrapMode, maximumLines, trimming);
        return this;
    }

    /// <summary>Draws an image into an explicit destination rectangle.</summary>
    public ImageComposition DrawImage(RgbaImage image, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        ValidateRect(x, y, width, height);
        VisualCanvas.ValidateEnum(fit, nameof(fit));
        var pixels = ApplyOpacity(image.Pixels, opacity);
        RasterImageFitting.Draw(_canvas, image.Width, image.Height, pixels, (int)Math.Round(x), (int)Math.Round(y), Math.Max(1, (int)Math.Round(width)), Math.Max(1, (int)Math.Round(height)), VisualCanvasImageFitMapping.Resolve(fit));
        return this;
    }

    /// <summary>Draws an image using anchor-based placement.</summary>
    public ImageComposition DrawImage(RgbaImage image, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        var bounds = placement.Resolve(Width, Height, width, height);
        return DrawImage(image, bounds.X, bounds.Y, bounds.Width, bounds.Height, fit, opacity);
    }

    /// <summary>Decodes image bytes within the requested limits and draws them into an explicit destination rectangle.</summary>
    public ImageComposition DrawImageBytes(byte[] data, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return DrawImage(RasterImageDecoder.Decode(data, options), x, y, width, height, fit, opacity);
    }

    /// <summary>Decodes image bytes within the requested limits and draws them using anchor-based placement.</summary>
    public ImageComposition DrawImageBytes(byte[] data, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return DrawImage(RasterImageDecoder.Decode(data, options), placement, width, height, fit, opacity);
    }

    /// <summary>Reads and decodes an image file within the requested limits and draws it into an explicit destination rectangle.</summary>
    public ImageComposition DrawImageFile(string path, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        return DrawImage(RasterImageDecoder.Read(path, options), x, y, width, height, fit, opacity);
    }

    /// <summary>Reads and decodes an image file within the requested limits and draws it using anchor-based placement.</summary>
    public ImageComposition DrawImageFile(string path, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        return DrawImage(RasterImageDecoder.Read(path, options), placement, width, height, fit, opacity);
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
        return DrawImage(block.RenderRgba(), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a visual block layer using anchor-based placement.</summary>
    public ImageComposition DrawVisualBlock(IVisualBlock block, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        return DrawImage(block.RenderRgba(), placement, width, height, fit, opacity);
    }

    /// <summary>Draws a visual grid layer.</summary>
    public ImageComposition DrawVisualGrid(VisualGrid grid, double x, double y, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return DrawImage(grid.RenderRgba(), x, y, width, height, fit, opacity);
    }

    /// <summary>Draws a visual grid layer using anchor-based placement.</summary>
    public ImageComposition DrawVisualGrid(VisualGrid grid, VisualCanvasPlacement placement, double width, double height, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return DrawImage(grid.RenderRgba(), placement, width, height, fit, opacity);
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

    /// <summary>Encodes the composition to PNG bytes using dependency-free raster options.</summary>
    public byte[] ToPng(RasterImageOptions? options) => RasterImageEncoder.Encode(ToImage(), RasterImageFormat.Png, options);

    /// <summary>Encodes the composition to JPEG bytes.</summary>
    public byte[] ToJpeg(RasterImageOptions? options = null) => RasterImageEncoder.Encode(ToImage(), RasterImageFormat.Jpeg, options);

    /// <summary>Encodes the composition to a static dependency-free raster format.</summary>
    /// <remarks>GIF output is provided by ChartForgeX.Stories over <see cref="ToImage"/>.</remarks>
    public byte[] ToRasterImage(RasterImageFormat format, RasterImageOptions? options = null) => RasterImageEncoder.Encode(ToImage(), format, options);

    /// <summary>Writes the composition to a raster stream.</summary>
    public void Write(Stream stream, RasterImageFormat format, RasterImageOptions? options = null) => RasterImageEncoder.WriteTo(stream, ToImage(), format, options);

    /// <summary>Saves the composition to a raster file using an explicit format.</summary>
    public void Save(string path, RasterImageFormat format, RasterImageOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, ToRasterImage(format, options));
    }

    /// <summary>Saves the composition to a raster file using the output extension to choose the format.</summary>
    public void Save(string path, RasterImageOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        Save(path, RasterImageFormatExtensions.FromFileExtension(path), options);
    }

    private static ChartPoint NearestPointOnRectangle(double x, double y, double rectX, double rectY, double rectWidth, double rectHeight) {
        var left = Math.Abs(x - rectX);
        var right = Math.Abs(x - (rectX + rectWidth));
        var top = Math.Abs(y - rectY);
        var bottom = Math.Abs(y - (rectY + rectHeight));
        var min = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
        if (min == left) return new ChartPoint(rectX, Math.Max(rectY, Math.Min(rectY + rectHeight, y)));
        if (min == right) return new ChartPoint(rectX + rectWidth, Math.Max(rectY, Math.Min(rectY + rectHeight, y)));
        if (min == top) return new ChartPoint(Math.Max(rectX, Math.Min(rectX + rectWidth, x)), rectY);
        return new ChartPoint(Math.Max(rectX, Math.Min(rectX + rectWidth, x)), rectY + rectHeight);
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

    private static void ValidateNonNegative(double value, string parameterName) {
        ValidateFinite(value, parameterName);
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
    }

    private static void ValidateFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }
}

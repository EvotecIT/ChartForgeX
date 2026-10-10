using System;
using System.IO;
using System.Net;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Composition;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

/// <summary>
/// Provides static canvas and composition-grid export and embedding methods.
/// </summary>
public static partial class VisualsExtensions {
    /// <summary>
    /// Configures the current visual grid theme in place, creating a light theme when the grid is currently automatic.
    /// </summary>
    /// <param name="grid">The visual grid to configure.</param>
    /// <param name="configure">The theme customization callback.</param>
    /// <returns>The current visual grid.</returns>
    public static VisualGrid ConfigureTheme(this VisualGrid grid, Action<ChartTheme> configure) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var theme = grid.Theme ?? ChartTheme.Light();
        configure(theme);
        grid.Theme = theme;
        return grid;
    }

    /// <summary>
    /// Sets the output pixel multiplier used by visual grid PNG exports using a named density preset.
    /// </summary>
    /// <param name="grid">The visual grid to configure.</param>
    /// <param name="scale">The output density preset.</param>
    /// <returns>The current visual grid.</returns>
    public static VisualGrid WithPngOutputScale(this VisualGrid grid, ChartPngOutputScale scale) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return grid.WithPngOutputScale((int)scale);
    }

    /// <summary>
    /// Renders a visual grid to SVG markup.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this VisualGrid grid) => new SvgVisualGridRenderer().Render(grid);

    /// <summary>
    /// Renders a visual grid to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVG grids in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this VisualGrid grid, string idScope) => new SvgVisualGridRenderer().Render(grid, idScope);

    /// <summary>
    /// Renders a visual grid to a standalone HTML fragment containing inline SVG charts and visual blocks.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this VisualGrid grid) => new HtmlVisualGridRenderer().RenderFragment(grid);

    /// <summary>
    /// Renders a visual grid to an HTML fragment with an additional deterministic ID scope.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep IDs unique when embedding equivalent fragments together.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this VisualGrid grid, string idScope) => new HtmlVisualGridRenderer().RenderFragment(grid, idScope);

    /// <summary>
    /// Renders a visual grid to a complete HTML document.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <returns>An HTML page.</returns>
    public static string ToHtmlPage(this VisualGrid grid) => new HtmlVisualGridRenderer().RenderPage(grid);

    /// <summary>
    /// Renders a visual grid to PNG bytes.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this VisualGrid grid) => new PngVisualGridRenderer().Render(grid);

    /// <summary>
    /// Saves a visual grid as an SVG file.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveSvg(this VisualGrid grid, string path) => File.WriteAllText(path, grid.ToSvg(), Encoding.UTF8);

    /// <summary>
    /// Saves a visual grid as a complete HTML file.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveHtml(this VisualGrid grid, string path) => File.WriteAllText(path, grid.ToHtmlPage(), Encoding.UTF8);

    /// <summary>
    /// Saves a visual grid as a PNG file.
    /// </summary>
    /// <param name="grid">The visual grid to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SavePng(this VisualGrid grid, string path) => File.WriteAllBytes(path, grid.ToPng());

    /// <summary>
    /// Renders a visual canvas to SVG markup.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this VisualCanvas canvas) => new SvgVisualCanvasRenderer().Render(canvas);

    /// <summary>
    /// Renders a visual canvas to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVGs in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this VisualCanvas canvas, string idScope) => new SvgVisualCanvasRenderer().Render(canvas, idScope);

    /// <summary>
    /// Renders a visual canvas to PNG bytes.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this VisualCanvas canvas) => new PngVisualCanvasRenderer().Render(canvas);

    /// <summary>
    /// Renders a visual canvas to a complete HTML document.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <returns>An HTML page.</returns>
    public static string ToHtmlPage(this VisualCanvas canvas) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var title = WebUtility.HtmlEncode(canvas.Title.Length == 0 ? "ChartForgeX visual canvas" : canvas.Title);
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + title + "</title><style>html,body{margin:0;min-height:100%;background:#020617}body{display:grid;place-items:center;padding:24px;box-sizing:border-box;background:linear-gradient(180deg,#020617,#0b1120);font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-visual-canvas{max-width:100%;height:auto}.chartforgex-visual-canvas svg{display:block;max-width:100%;height:auto;overflow:visible}@media print{html,body{background:transparent}body{padding:0}.chartforgex-visual-canvas{max-width:none}}</style></head><body><div class=\"chartforgex-visual-canvas\">" + canvas.ToSvg("html-page") + "</div></body></html>";
    }

    /// <summary>
    /// Saves a visual canvas as an SVG file.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveSvg(this VisualCanvas canvas, string path) => File.WriteAllText(path, canvas.ToSvg(), Encoding.UTF8);

    /// <summary>
    /// Saves a visual canvas as a complete HTML file.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveHtml(this VisualCanvas canvas, string path) => File.WriteAllText(path, canvas.ToHtmlPage(), Encoding.UTF8);

    /// <summary>
    /// Saves a visual canvas as a PNG file.
    /// </summary>
    /// <param name="canvas">The visual canvas to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SavePng(this VisualCanvas canvas, string path) => File.WriteAllBytes(path, canvas.ToPng());

    /// <summary>
    /// Creates a visual canvas from decoded RGBA pixels with the image as the first layer.
    /// </summary>
    public static VisualCanvas ToVisualCanvas(this RgbaImage image, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch) {
        VisualCanvas.ValidateEnum(fit, nameof(fit));
        return VisualCanvas.Create(image.Width, image.Height)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddRasterImage(0, 0, image.Width, image.Height, image, fit);
    }

    /// <summary>
    /// Adds an independent snapshot of decoded RGBA pixels as an image layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddRasterImage(this VisualCanvas canvas, double x, double y, double width, double height, RgbaImage image, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        return canvas.AddImage(image, x, y, width, height, opacity, fit);
    }

    /// <summary>
    /// Adds decoded RGBA pixels as an image layer using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddRasterImage(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, RgbaImage image, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddRasterImage(bounds.X, bounds.Y, bounds.Width, bounds.Height, image, fit, opacity);
    }

    /// <summary>
    /// Decodes image bytes and adds them as an image layer to a visual canvas.
    /// </summary>
    /// <remarks>SVG preserves original static PNG and JPEG containers. Animated PNG and other decoded formats are embedded as a static PNG. PNG output uses the decoded pixels; both representations are captured during this call.</remarks>
    public static VisualCanvas AddImageBytes(this VisualCanvas canvas, double x, double y, double width, double height, byte[] data, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (data == null) throw new ArgumentNullException(nameof(data));
        var image = RasterImageDecoder.Decode(data, options);
        return AddRenderedImage(canvas, x, y, width, height, image, EmbeddedRasterDataUri(data, image), fit, opacity);
    }

    /// <summary>
    /// Decodes image bytes and adds them as an image layer using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddImageBytes(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, byte[] data, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddImageBytes(bounds.X, bounds.Y, bounds.Width, bounds.Height, data, fit, opacity, options);
    }

    /// <summary>
    /// Decodes an image file and adds it as an image layer to a visual canvas.
    /// </summary>
    /// <remarks>The file is read within the requested decode limits. SVG preserves original static PNG and JPEG containers; other inputs are embedded as a static PNG. PNG output uses the decoded pixels.</remarks>
    public static VisualCanvas AddImageFile(this VisualCanvas canvas, double x, double y, double width, double height, string path, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (path == null) throw new ArgumentNullException(nameof(path));
        var image = RasterImageDecoder.Read(path, options, out var data);
        return AddRenderedImage(canvas, x, y, width, height, image, EmbeddedRasterDataUri(data, image), fit, opacity);
    }

    /// <summary>
    /// Decodes an image file and adds it as an image layer using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddImageFile(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, string path, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1, RasterDecodeOptions? options = null) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddImageFile(bounds.X, bounds.Y, bounds.Width, bounds.Height, path, fit, opacity, options);
    }

    /// <summary>
    /// Decodes an image file and adds it as the content of a hero badge.
    /// </summary>
    /// <remarks>The file is read within the requested decode limits. SVG preserves original static PNG and JPEG containers; other inputs are embedded as a static PNG. PNG output uses the decoded pixels.</remarks>
    public static VisualCanvas AddHeroBadgeImageFile(this VisualCanvas canvas, double x, double y, double width, double height, string path, string symbol = "", ChartColor? accent = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Contain, double padding = 10, double opacity = 1, RasterDecodeOptions? options = null) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (path == null) throw new ArgumentNullException(nameof(path));
        var image = RasterImageDecoder.Read(path, options, out var data);
        return canvas.AddHeroBadge(x, y, width, height, symbol, accent, EmbeddedRasterDataUri(data, image), image.Pixels, image.Width, image.Height, fit, padding, opacity);
    }

    /// <summary>
    /// Decodes an image file and adds it as the content of a hero badge using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddHeroBadgeImageFile(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, string path, string symbol = "", ChartColor? accent = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Contain, double padding = 10, double opacity = 1, RasterDecodeOptions? options = null) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddHeroBadgeImageFile(bounds.X, bounds.Y, bounds.Width, bounds.Height, path, symbol, accent, fit, padding, opacity, options);
    }

    /// <summary>
    /// Adds a rendered chart layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddChart(this VisualCanvas canvas, double x, double y, double width, double height, Chart chart, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return AddRenderedImage(canvas, x, y, width, height, RasterRenderer.RenderImage(chart), SvgDataUri(chart.ToSvg("visual-canvas-chart")), fit, opacity);
    }

    /// <summary>
    /// Adds a rendered chart layer to a visual canvas using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddChart(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, Chart chart, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddChart(bounds.X, bounds.Y, bounds.Width, bounds.Height, chart, fit, opacity);
    }

    /// <summary>
    /// Adds a rendered chart grid layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddChartGrid(this VisualCanvas canvas, double x, double y, double width, double height, ChartGrid grid, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return AddRenderedImage(canvas, x, y, width, height, grid.ToRgbaImage(), SvgDataUri(grid.ToSvg()), fit, opacity);
    }

    /// <summary>
    /// Adds a rendered chart grid layer to a visual canvas using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddChartGrid(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, ChartGrid grid, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddChartGrid(bounds.X, bounds.Y, bounds.Width, bounds.Height, grid, fit, opacity);
    }

    /// <summary>
    /// Adds a rendered visual block layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddVisualBlock(this VisualCanvas canvas, double x, double y, double width, double height, IVisualBlock block, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        return AddRenderedImage(canvas, x, y, width, height, RasterRenderer.RenderImage(block), SvgDataUri(block.ToSvg()), fit, opacity);
    }

    /// <summary>
    /// Adds a rendered visual block layer to a visual canvas using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddVisualBlock(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, IVisualBlock block, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddVisualBlock(bounds.X, bounds.Y, bounds.Width, bounds.Height, block, fit, opacity);
    }

    /// <summary>
    /// Adds a rendered visual grid layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddVisualGrid(this VisualCanvas canvas, double x, double y, double width, double height, VisualGrid grid, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        return AddRenderedImage(canvas, x, y, width, height, grid.ToRgbaImage(), SvgDataUri(grid.ToSvg()), fit, opacity);
    }

    /// <summary>
    /// Adds a rendered visual grid layer to a visual canvas using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddVisualGrid(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, VisualGrid grid, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddVisualGrid(bounds.X, bounds.Y, bounds.Width, bounds.Height, grid, fit, opacity);
    }

    /// <summary>
    /// Adds a rendered topology chart layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddTopology(this VisualCanvas canvas, double x, double y, double width, double height, TopologyChart topology, TopologyRenderOptions? options = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (topology == null) throw new ArgumentNullException(nameof(topology));
        return AddRenderedImage(canvas, x, y, width, height, TopologyRasterRenderer.RenderImage(topology, options), SvgDataUri(topology.ToSvg(options)), fit, opacity);
    }

    /// <summary>
    /// Adds a rendered topology chart layer to a visual canvas using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddTopology(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, TopologyChart topology, TopologyRenderOptions? options = null, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddTopology(bounds.X, bounds.Y, bounds.Width, bounds.Height, topology, options, fit, opacity);
    }

    private static VisualCanvas AddRenderedImage(VisualCanvas canvas, double x, double y, double width, double height, RgbaImage image, string href, VisualCanvasImageFit fit, double opacity) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        VisualCanvas.ValidateEnum(fit, nameof(fit));
        return canvas.AddImage(x, y, width, height, href, image.Pixels, image.Width, image.Height, opacity, fit);
    }

    private static string SvgDataUri(string svg) {
        if (svg == null) throw new ArgumentNullException(nameof(svg));
        return "data:image/svg+xml;charset=utf-8," + Uri.EscapeDataString(svg);
    }

    private static string EmbeddedRasterDataUri(byte[] data, RgbaImage image) {
        var mimeType = RasterImageDecoder.MimeTypeFor(data);
        if (mimeType == "image/jpeg" || (mimeType == "image/png" && !PngReader.IsAnimatedPng(data))) {
            return "data:" + mimeType + ";base64," + Convert.ToBase64String(data);
        }
        return VisualCanvasImagePixels.EmbeddedPng(image.Width, image.Height, image.Pixels);
    }

}

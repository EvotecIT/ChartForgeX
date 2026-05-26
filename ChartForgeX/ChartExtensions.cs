using System;
using System.IO;
using System.Net;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.Composition;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX;

/// <summary>
/// Provides convenience rendering and file export methods for charts.
/// </summary>
public static partial class ChartExtensions {
    /// <summary>
    /// Configures the current chart theme in place.
    /// </summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="configure">The theme customization callback.</param>
    /// <returns>The current chart.</returns>
    public static Chart WithTheme(this Chart chart, Action<ChartTheme> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(chart.Options.Theme);
        return chart;
    }

    /// <summary>
    /// Sets the default series palette on the current chart theme.
    /// </summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="colors">The palette colors.</param>
    /// <returns>The current chart.</returns>
    public static Chart WithPalette(this Chart chart, params ChartColor[] colors) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Options.Theme.Palette = colors;
        return chart;
    }

    /// <summary>
    /// Sets the default series palette on the current chart theme from hexadecimal color strings.
    /// </summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="colors">The color strings in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current chart.</returns>
    public static Chart WithPalette(this Chart chart, params string[] colors) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Options.Theme.Palette = ChartPalettes.FromHex(colors);
        return chart;
    }

    /// <summary>
    /// Applies a reusable brand kit to the current chart theme.
    /// </summary>
    /// <param name="chart">The chart to configure.</param>
    /// <param name="brandKit">The brand kit to apply.</param>
    /// <returns>The current chart.</returns>
    public static Chart WithBrandKit(this Chart chart, ChartBrandKit brandKit) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (brandKit == null) throw new ArgumentNullException(nameof(brandKit));
        brandKit.ApplyTo(chart.Options.Theme);
        return chart;
    }

    /// <summary>
    /// Configures the current chart grid theme in place, creating a light grid theme when the grid is currently automatic.
    /// </summary>
    /// <param name="grid">The chart grid to configure.</param>
    /// <param name="configure">The theme customization callback.</param>
    /// <returns>The current chart grid.</returns>
    public static ChartGrid WithTheme(this ChartGrid grid, Action<ChartTheme> configure) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var theme = grid.Theme ?? ChartTheme.Light();
        configure(theme);
        grid.Theme = theme;
        return grid;
    }

    /// <summary>
    /// Applies a reusable brand kit to the current chart grid theme, creating a light grid theme when the grid is currently automatic.
    /// </summary>
    /// <param name="grid">The chart grid to configure.</param>
    /// <param name="brandKit">The brand kit to apply.</param>
    /// <returns>The current chart grid.</returns>
    public static ChartGrid WithBrandKit(this ChartGrid grid, ChartBrandKit brandKit) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (brandKit == null) throw new ArgumentNullException(nameof(brandKit));
        var theme = grid.Theme ?? ChartTheme.Light();
        brandKit.ApplyTo(theme);
        grid.Theme = theme;
        return grid;
    }

    /// <summary>
    /// Renders a chart to SVG markup.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this Chart chart) => new SvgChartRenderer().Render(chart);

    /// <summary>
    /// Renders a chart to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVGs in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this Chart chart, string idScope) => new SvgChartRenderer().Render(chart, idScope);

    /// <summary>
    /// Renders a chart to a standalone HTML fragment containing inline SVG.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this Chart chart) => new HtmlChartRenderer().RenderFragment(chart);

    /// <summary>
    /// Renders a chart to a complete HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>An HTML page.</returns>
    public static string ToHtmlPage(this Chart chart) => new HtmlChartRenderer().RenderPage(chart);

    /// <summary>
    /// Renders a chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this Chart chart) => new PngChartRenderer().Render(chart);

    /// <summary>
    /// Resolves the font that would be used when rendering the chart to PNG.
    /// </summary>
    /// <param name="chart">The chart to inspect.</param>
    /// <returns>The PNG font resolution details.</returns>
    public static PngFontInfo GetPngFontInfo(this Chart chart) => PngChartRenderer.GetFontInfo(chart);

    /// <summary>
    /// Saves a chart as an SVG file.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveSvg(this Chart chart, string path) => File.WriteAllText(path, chart.ToSvg(), Encoding.UTF8);

    /// <summary>
    /// Saves a chart as a complete HTML file.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveHtml(this Chart chart, string path) => File.WriteAllText(path, chart.ToHtmlPage(), Encoding.UTF8);

    /// <summary>
    /// Saves a chart as a PNG file.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SavePng(this Chart chart, string path) => File.WriteAllBytes(path, chart.ToPng());

    /// <summary>
    /// Renders a chart grid to SVG markup.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this ChartGrid grid) => new SvgChartGridRenderer().Render(grid);

    /// <summary>
    /// Renders a chart grid to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVG grids in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this ChartGrid grid, string idScope) => new SvgChartGridRenderer().Render(grid, idScope);

    /// <summary>
    /// Renders a chart grid to a standalone HTML fragment containing inline SVG charts.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this ChartGrid grid) => new HtmlChartGridRenderer().RenderFragment(grid);

    /// <summary>
    /// Renders a chart grid to a complete HTML document.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>An HTML page.</returns>
    public static string ToHtmlPage(this ChartGrid grid) => new HtmlChartGridRenderer().RenderPage(grid);

    /// <summary>
    /// Renders a chart grid to PNG bytes.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this ChartGrid grid) => new PngChartGridRenderer().Render(grid);

    /// <summary>
    /// Saves a chart grid as an SVG file.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveSvg(this ChartGrid grid, string path) => File.WriteAllText(path, grid.ToSvg(), Encoding.UTF8);

    /// <summary>
    /// Saves a chart grid as a complete HTML file.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveHtml(this ChartGrid grid, string path) => File.WriteAllText(path, grid.ToHtmlPage(), Encoding.UTF8);

    /// <summary>
    /// Saves a chart grid as a PNG file.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SavePng(this ChartGrid grid, string path) => File.WriteAllBytes(path, grid.ToPng());

    /// <summary>
    /// Sets the default palette on the current visual block theme.
    /// </summary>
    /// <typeparam name="TBlock">The visual block type.</typeparam>
    /// <param name="block">The visual block to configure.</param>
    /// <param name="colors">The palette colors.</param>
    /// <returns>The current visual block.</returns>
    public static TBlock WithPalette<TBlock>(this TBlock block, params ChartColor[] colors) where TBlock : IVisualBlock {
        if (block == null) throw new ArgumentNullException(nameof(block));
        block.Options.Theme.Palette = colors;
        return block;
    }

    /// <summary>
    /// Sets the default palette on the current visual block theme from hexadecimal color strings.
    /// </summary>
    /// <typeparam name="TBlock">The visual block type.</typeparam>
    /// <param name="block">The visual block to configure.</param>
    /// <param name="colors">The color strings in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current visual block.</returns>
    public static TBlock WithPalette<TBlock>(this TBlock block, params string[] colors) where TBlock : IVisualBlock {
        if (block == null) throw new ArgumentNullException(nameof(block));
        block.Options.Theme.Palette = ChartPalettes.FromHex(colors);
        return block;
    }

    /// <summary>
    /// Applies a reusable brand kit to the current visual block theme.
    /// </summary>
    /// <typeparam name="TBlock">The visual block type.</typeparam>
    /// <param name="block">The visual block to configure.</param>
    /// <param name="brandKit">The brand kit to apply.</param>
    /// <returns>The current visual block.</returns>
    public static TBlock WithBrandKit<TBlock>(this TBlock block, ChartBrandKit brandKit) where TBlock : IVisualBlock {
        if (block == null) throw new ArgumentNullException(nameof(block));
        if (brandKit == null) throw new ArgumentNullException(nameof(brandKit));
        brandKit.ApplyTo(block.Options.Theme);
        return block;
    }

    /// <summary>
    /// Sets the output pixel multiplier used by visual block PNG exports using a named density preset.
    /// </summary>
    /// <typeparam name="TBlock">The visual block type.</typeparam>
    /// <param name="block">The visual block to configure.</param>
    /// <param name="scale">The output density preset.</param>
    /// <returns>The current visual block.</returns>
    public static TBlock WithPngOutputScale<TBlock>(this TBlock block, ChartPngOutputScale scale) where TBlock : IVisualBlock {
        if (block == null) throw new ArgumentNullException(nameof(block));
        block.Options.PngOutputScale = (int)scale;
        return block;
    }

    /// <summary>
    /// Configures the current visual grid theme in place, creating a light theme when the grid is currently automatic.
    /// </summary>
    /// <param name="grid">The visual grid to configure.</param>
    /// <param name="configure">The theme customization callback.</param>
    /// <returns>The current visual grid.</returns>
    public static VisualGrid WithTheme(this VisualGrid grid, Action<ChartTheme> configure) {
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
    /// Renders a visual block to SVG markup.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this IVisualBlock block) => new SvgVisualBlockRenderer().Render(block);

    /// <summary>
    /// Renders a visual block to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVGs in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this IVisualBlock block, string idScope) => new SvgVisualBlockRenderer().Render(block, idScope);

    /// <summary>
    /// Renders a visual block to a standalone HTML fragment containing inline SVG.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this IVisualBlock block) => new HtmlVisualBlockRenderer().RenderFragment(block);

    /// <summary>
    /// Renders a visual block to a complete HTML document.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <returns>An HTML page.</returns>
    public static string ToHtmlPage(this IVisualBlock block) => new HtmlVisualBlockRenderer().RenderPage(block);

    /// <summary>
    /// Renders a visual block to PNG bytes.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this IVisualBlock block) => new PngVisualBlockRenderer().Render(block);

    /// <summary>
    /// Saves a visual block as an SVG file.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveSvg(this IVisualBlock block, string path) => File.WriteAllText(path, block.ToSvg(), Encoding.UTF8);

    /// <summary>
    /// Saves a visual block as a complete HTML file.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SaveHtml(this IVisualBlock block, string path) => File.WriteAllText(path, block.ToHtmlPage(), Encoding.UTF8);

    /// <summary>
    /// Saves a visual block as a PNG file.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="path">The output file path.</param>
    public static void SavePng(this IVisualBlock block, string path) => File.WriteAllBytes(path, block.ToPng());

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
    /// Adds decoded RGBA pixels as an image layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddRasterImage(this VisualCanvas canvas, double x, double y, double width, double height, RgbaImage image, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        return AddRenderedImage(canvas, x, y, width, height, image, RasterDataUri(image), fit, opacity);
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
    public static VisualCanvas AddImageBytes(this VisualCanvas canvas, double x, double y, double width, double height, byte[] data, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        var image = RasterImageDecoder.Decode(data);
        return AddRenderedImage(canvas, x, y, width, height, image, EmbeddedRasterDataUri(data, image, null), fit, opacity);
    }

    /// <summary>
    /// Decodes image bytes and adds them as an image layer using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddImageBytes(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, byte[] data, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddImageBytes(bounds.X, bounds.Y, bounds.Width, bounds.Height, data, fit, opacity);
    }

    /// <summary>
    /// Decodes an image file and adds it as an image layer to a visual canvas.
    /// </summary>
    public static VisualCanvas AddImageFile(this VisualCanvas canvas, double x, double y, double width, double height, string path, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        var data = File.ReadAllBytes(path);
        var image = RasterImageDecoder.Decode(data);
        return AddRenderedImage(canvas, x, y, width, height, image, EmbeddedRasterDataUri(data, image, path), fit, opacity);
    }

    /// <summary>
    /// Decodes an image file and adds it as an image layer using anchor-based placement.
    /// </summary>
    public static VisualCanvas AddImageFile(this VisualCanvas canvas, VisualCanvasPlacement placement, double width, double height, string path, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch, double opacity = 1) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var bounds = canvas.ResolvePlacement(placement, width, height);
        return canvas.AddImageFile(bounds.X, bounds.Y, bounds.Width, bounds.Height, path, fit, opacity);
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
        return AddRenderedImage(canvas, x, y, width, height, RasterRenderer.RenderImage(grid), SvgDataUri(grid.ToSvg()), fit, opacity);
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
        return AddRenderedImage(canvas, x, y, width, height, RasterRenderer.RenderImage(grid), SvgDataUri(grid.ToSvg()), fit, opacity);
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

    private static string RasterDataUri(RgbaImage image) => BinaryDataUri(PngWriter.WriteRgba(image), "image/png");

    private static string EmbeddedRasterDataUri(byte[] data, RgbaImage image, string? path) {
        var mimeType = RasterImageDecoder.MimeTypeFor(data, path);
        if (mimeType == "image/png" || mimeType == "image/jpeg") return BinaryDataUri(data, mimeType);
        return RasterDataUri(image);
    }

    private static string BinaryDataUri(byte[] data, string mimeType) => "data:" + mimeType + ";base64," + Convert.ToBase64String(data);
}

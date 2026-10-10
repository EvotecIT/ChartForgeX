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
    public static Chart ConfigureTheme(this Chart chart, Action<ChartTheme> configure) {
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
    public static ChartGrid ConfigureTheme(this ChartGrid grid, Action<ChartTheme> configure) {
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
    public static string ToSvg(this Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg();

    /// <summary>
    /// Renders a chart to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVGs in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this Chart chart, string idScope) => chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg(new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(idScope ?? throw new ArgumentNullException(nameof(idScope))), chart.Options.SvgColorVariables));

    /// <summary>
    /// Renders a chart to a standalone HTML fragment containing inline SVG.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this Chart chart) => new HtmlChartRenderer().RenderFragment(chart);

    /// <summary>
    /// Renders a chart to an HTML fragment with an additional deterministic ID scope.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep IDs unique when embedding equivalent fragments together.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this Chart chart, string idScope) => new HtmlChartRenderer().RenderFragment(chart, idScope);

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
    public static byte[] ToPng(this Chart chart) { var request = VisualExportRequest.ForChart(chart); return chart.Prepare(request.Context).ToPng(request.RasterOptions); }

    /// <summary>
    /// Resolves the font that would be used when rendering the chart to PNG.
    /// </summary>
    /// <param name="chart">The chart to inspect.</param>
    /// <returns>The PNG font resolution details.</returns>
    public static PngFontInfo GetPngFontInfo(this Chart chart) { var font = VisualExportRequest.ForChart(chart).Context.Font; return TrueTypeFont.ResolveInfo(font.FilePath, font.CollectionIndex, font.FaceName, font.Family); }

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
    public static string ToSvg(this ChartGrid grid) => grid.Prepare(VisualExportRequest.ForGrid(grid).Context).ToSvg();

    /// <summary>
    /// Renders a chart grid to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVG grids in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this ChartGrid grid, string idScope) => grid.Prepare(VisualExportRequest.ForGrid(grid).Context).ToSvg(new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(idScope ?? throw new ArgumentNullException(nameof(idScope))), grid.SvgColorVariables));

    /// <summary>
    /// Renders a chart grid to a standalone HTML fragment containing inline SVG charts.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this ChartGrid grid) => new HtmlChartGridRenderer().RenderFragment(grid);

    /// <summary>
    /// Renders a chart grid to an HTML fragment with an additional deterministic ID scope.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep IDs unique when embedding equivalent fragments together.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this ChartGrid grid, string idScope) => new HtmlChartGridRenderer().RenderFragment(grid, idScope);

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
    public static byte[] ToPng(this ChartGrid grid) { var request = VisualExportRequest.ForGrid(grid); return grid.Prepare(request.Context).ToPng(request.RasterOptions); }

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
    /// Renders a visual block to SVG markup.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this IVisualBlock block) => block.RenderSvg(string.Empty);

    /// <summary>
    /// Renders a visual block to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVGs in one document.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this IVisualBlock block, string idScope) => block.RenderSvg(idScope);

    /// <summary>
    /// Renders a visual block to a standalone HTML fragment containing inline SVG.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this IVisualBlock block) => new HtmlVisualBlockRenderer().RenderFragment(block);

    /// <summary>
    /// Renders a visual block to an HTML fragment with an additional deterministic ID scope.
    /// </summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep IDs unique when embedding equivalent fragments together.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToHtmlFragment(this IVisualBlock block, string idScope) => new HtmlVisualBlockRenderer().RenderFragment(block, idScope);

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
    public static byte[] ToPng(this IVisualBlock block) => PngWriter.WriteRgba(block.RenderRgba());

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

}

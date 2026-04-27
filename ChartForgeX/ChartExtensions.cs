using System.IO;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX;

/// <summary>
/// Provides convenience rendering and file export methods for charts.
/// </summary>
public static class ChartExtensions {
    /// <summary>
    /// Renders a chart to SVG markup.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this Chart chart) => new SvgChartRenderer().Render(chart);

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
}

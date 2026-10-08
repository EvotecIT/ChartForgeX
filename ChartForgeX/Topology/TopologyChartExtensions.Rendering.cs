using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Renders the topology chart to SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>Complete SVG markup.</returns>
    public static string ToSvg(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologySvgRenderer().Render(chart, options);

    /// <summary>
    /// Renders the topology chart to SVG with every SVG id scoped, so several renders of the same chart can be embedded
    /// in one document (see <see cref="TopologyRenderOptions.IdScope"/>).
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="idScope">A caller-provided scope put in front of every SVG id.</param>
    /// <param name="options">Optional render options; null uses the options carried by the chart. They are not modified.</param>
    /// <returns>Complete SVG markup.</returns>
    public static string ToSvg(this TopologyChart chart, string idScope, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (string.IsNullOrWhiteSpace(idScope)) throw new ArgumentException("The id scope must not be empty.", nameof(idScope));
        var scoped = chart.ResolveRenderOptions(options).Clone();
        scoped.IdScope = idScope;
        return new TopologySvgRenderer().Render(chart, scoped);
    }

    /// <summary>
    /// Renders the topology chart to an HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An embeddable HTML fragment.</returns>
    public static string ToHtmlFragment(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderFragment(chart, options);

    /// <summary>
    /// Renders the topology chart to an HTML fragment without renderer-owned stylesheet or script assets.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An embeddable HTML fragment that expects the host document to register topology HTML assets.</returns>
    public static string ToHtmlFragmentWithoutAssets(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderFragmentWithoutAssets(chart, options);

    /// <summary>
    /// Renders the topology chart to a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A complete HTML page.</returns>
    public static string ToHtmlPage(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderPage(chart, options);

    /// <summary>
    /// Renders the topology chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyPngRenderer().Render(chart, options);

    /// <summary>
    /// Saves the topology chart as SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveSvg(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToSvg(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToHtmlPage(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as PNG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SavePng(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllBytes(path, chart.ToPng(options));

}

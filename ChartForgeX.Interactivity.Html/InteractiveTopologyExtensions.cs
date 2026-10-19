using System;
using System.IO;
using System.Text;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Provides adapter-owned interactive HTML output for topology charts.</summary>
public static class InteractiveTopologyExtensions {
    /// <summary>Renders a self-contained interactive topology HTML fragment.</summary>
    public static string ToInteractiveHtmlFragment(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return new HtmlInteractiveTopologyRenderer().RenderFragment(chart, options);
    }

    /// <summary>Renders interactive topology markup without renderer-owned assets.</summary>
    public static string ToInteractiveHtmlFragmentWithoutAssets(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return new HtmlInteractiveTopologyRenderer().RenderFragmentWithoutAssets(chart, options);
    }

    /// <summary>Renders a complete self-contained interactive topology HTML page.</summary>
    public static string ToInteractiveHtmlPage(this TopologyChart chart, TopologyRenderOptions? options = null) {
        return new HtmlInteractiveTopologyRenderer().RenderPage(chart, options);
    }

    /// <summary>Renders controls around trusted SVG created from the adapter's detached interactive topology.</summary>
    public static string ToInteractiveHtmlPage(this TopologyChart chart, TopologyRenderOptions? options,
        Func<PreparedTopology, string> svgPresentation) {
        if (svgPresentation == null) throw new ArgumentNullException(nameof(svgPresentation));
        return new HtmlInteractiveTopologyRenderer().RenderPresentationPage(chart, svgPresentation, options);
    }

    /// <summary>Saves a complete self-contained interactive topology HTML page.</summary>
    public static void SaveInteractiveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, chart.ToInteractiveHtmlPage(options), Encoding.UTF8);
    }

    /// <summary>Saves interactive controls around a trusted detached SVG presentation of the same topology.</summary>
    public static void SaveInteractiveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options,
        Func<PreparedTopology, string> svgPresentation) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, chart.ToInteractiveHtmlPage(options, svgPresentation), Encoding.UTF8);
    }
    /// <summary>Renders a localized interactive page, optionally around a trusted detached SVG presentation.</summary>
    /// <param name="chart">The source topology.</param>
    /// <param name="options">Native render options, or null for the chart defaults.</param>
    /// <param name="svgPresentation">Optional producer for the adapter's detached prepared topology.</param>
    /// <param name="configureLocalization">Optional control-message configuration; authored facts remain unchanged.</param>
    public static string ToInteractiveHtmlPage(this TopologyChart chart, TopologyRenderOptions? options,
        Func<PreparedTopology, string>? svgPresentation = null, Action<HtmlChartLocalization>? configureLocalization = null) {
        var renderer = LocalizedRenderer(configureLocalization);
        return svgPresentation == null ? renderer.RenderPage(chart, options) : renderer.RenderPresentationPage(chart, svgPresentation, options);
    }

    /// <summary>Renders an interactive fragment with localized control messages.</summary>
    public static string ToInteractiveHtmlFragment(this TopologyChart chart, TopologyRenderOptions? options,
        Action<HtmlChartLocalization> configureLocalization) => LocalizedRenderer(configureLocalization).RenderFragment(chart, options);

    /// <summary>Renders localized controls without renderer-owned stylesheet or script assets.</summary>
    public static string ToInteractiveHtmlFragmentWithoutAssets(this TopologyChart chart, TopologyRenderOptions? options,
        Action<HtmlChartLocalization> configureLocalization) => LocalizedRenderer(configureLocalization).RenderFragmentWithoutAssets(chart, options);

    /// <summary>Saves a localized interactive page, optionally around a trusted detached SVG presentation.</summary>
    public static void SaveInteractiveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options,
        Func<PreparedTopology, string>? svgPresentation = null, Action<HtmlChartLocalization>? configureLocalization = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, chart.ToInteractiveHtmlPage(options, svgPresentation, configureLocalization), Encoding.UTF8);
    }

    private static HtmlInteractiveTopologyRenderer LocalizedRenderer(Action<HtmlChartLocalization>? configure) {
        var renderer = new HtmlInteractiveTopologyRenderer();
        configure?.Invoke(renderer.Localization);
        return renderer;
    }

}

using System;
using ChartForgeX.Html;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders topology charts with the adapter-owned browser interaction runtime.
/// </summary>
public sealed class HtmlInteractiveTopologyRenderer {
    private readonly TopologyHtmlRenderer _staticRenderer = new();

    /// <summary>Renders a self-contained interactive topology HTML fragment.</summary>
    public string RenderFragment(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options = Prepare(options ?? chart.DefaultRenderOptions);
        return _staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: true) + InteractionScriptTag(options);
    }

    /// <summary>Renders interactive topology markup without CSS or JavaScript assets.</summary>
    public string RenderFragmentWithoutAssets(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options = Prepare(options ?? chart.DefaultRenderOptions);
        return _staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: false);
    }

    /// <summary>Renders a complete self-contained interactive topology HTML page.</summary>
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options = null) => RenderPage(chart, options, null);

    /// <summary>
    /// Renders a complete interactive topology HTML page. When <paramref name="externalAssets"/> is set, the interaction
    /// runtime is linked from its base path instead of inlined; write it once per bundle with
    /// <see cref="HtmlInteractiveAssetFiles.TopologyScript"/> using the same options. Page styles stay inline because they
    /// depend on the render options.
    /// </summary>
    /// <param name="chart">The topology to render.</param>
    /// <param name="options">Optional render options.</param>
    /// <param name="externalAssets">Optional shared asset references; null inlines the runtime.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options, HtmlAssetReferences? externalAssets) {
        return RenderPageCore(chart, options, externalAssets, null);
    }

    /// <summary>Renders a complete interactive page with producer-owned SVG over the adapter's prepared topology.</summary>
    /// <param name="chart">The topology whose metadata, scenarios and HTML controls are rendered.</param>
    /// <param name="renderSvg">Exports trusted SVG from the supplied detached topology, for example a script-free motion presentation.
    /// The callback runs once after interactive scenario and graph-control policies are applied. Retain the supplied geometry,
    /// topology metadata and SVG identity scope so browser controls target the same entities.</param>
    /// <param name="options">Optional topology and HTML render options; caller-owned options are not modified.</param>
    /// <param name="externalAssets">Optional shared interaction runtime references; null inlines the runtime.</param>
    /// <returns>A complete HTML document whose viewport uses the prepared presentation's logical width.</returns>
    public string RenderPresentationPage(TopologyChart chart, Func<PreparedTopology, string> renderSvg,
        TopologyRenderOptions? options = null, HtmlAssetReferences? externalAssets = null) {
        if (renderSvg == null) throw new ArgumentNullException(nameof(renderSvg));
        return RenderPageCore(chart, options, externalAssets, renderSvg);
    }

    /// <summary>Renders a self-contained interactive fragment with producer-owned SVG over the adapter's prepared topology.</summary>
    /// <param name="chart">The topology whose metadata, scenarios and HTML controls are rendered.</param>
    /// <param name="renderSvg">Exports trusted SVG from the supplied detached topology. Preserve its geometry, topology metadata
    /// and identity scope; the callback runs once after interactive scenario and graph-control policies are applied.</param>
    /// <param name="options">Optional topology and HTML render options; caller-owned options are not modified.</param>
    /// <returns>Interactive markup with inline CSS and JavaScript assets.</returns>
    public string RenderPresentationFragment(TopologyChart chart, Func<PreparedTopology, string> renderSvg,
        TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (renderSvg == null) throw new ArgumentNullException(nameof(renderSvg));
        options = Prepare(options ?? chart.DefaultRenderOptions);
        return _staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: true, renderSvg: renderSvg) + InteractionScriptTag(options);
    }

    /// <summary>Renders producer-owned topology SVG and interactive controls without CSS or JavaScript assets.</summary>
    /// <param name="chart">The topology whose metadata, scenarios and HTML controls are rendered.</param>
    /// <param name="renderSvg">Exports trusted SVG from the supplied detached topology. Preserve its geometry, topology metadata
    /// and identity scope; the callback runs once after interactive scenario and graph-control policies are applied.</param>
    /// <param name="options">Optional topology and HTML render options; caller-owned options are not modified.</param>
    /// <returns>Interactive markup that expects the embedding host to register topology HTML assets.</returns>
    public string RenderPresentationFragmentWithoutAssets(TopologyChart chart, Func<PreparedTopology, string> renderSvg,
        TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (renderSvg == null) throw new ArgumentNullException(nameof(renderSvg));
        options = Prepare(options ?? chart.DefaultRenderOptions);
        return _staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: false, renderSvg: renderSvg);
    }

    private string RenderPageCore(TopologyChart chart, TopologyRenderOptions? options, HtmlAssetReferences? externalAssets,
        Func<PreparedTopology, string>? renderSvg) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options = Prepare(options ?? chart.DefaultRenderOptions);
        var theme = chart.Theme ?? TopologyTheme.Light();
        var title = string.IsNullOrWhiteSpace(chart.Title) ? chart.Labels.UntitledTopology : chart.Title!;
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, TopologyHtmlRenderer.BuildPageStyle(options, theme));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(_staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: false, assetSource: "document", renderSvg: renderSvg)).Line()
            .RawTrusted(externalAssets == null ? InteractionScriptTag(options) : string.Empty);
        if (externalAssets != null) HtmlInteractiveAssetFiles.WriteScript(writer, externalAssets, HtmlInteractiveAssetFiles.TopologyScript(options), null);
        writer.Line()
            .EndElement().Line()
            .EndElement().Line();
        return writer.Build();
    }

    /// <summary>Builds the raw JavaScript runtime for host-level asset registration.</summary>
    public static string BuildInteractionScript(TopologyRenderOptions? options = null) {
        options ??= new TopologyRenderOptions();
        var cssPrefix = TopologyHtmlRenderer.GetCssClassPrefix(options);
        return HtmlTopologyAssets.InteractionScript
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace(".cfx-topology-controls", "." + cssPrefix + "-controls")
            .Replace(".cfx-topology-scenarios", "." + cssPrefix + "-scenarios")
            .Replace(".cfx-topology-scenario-panel", "." + cssPrefix + "-scenario-panel")
            .Replace(".cfx-topology-selection-panel", "." + cssPrefix + "-selection-panel")
            .Replace(".cfx-topology-force-controls", "." + cssPrefix + "-force-controls")
            .Replace("cfx-topology-html-", cssPrefix + "-html-");
    }

    internal static TopologyRenderOptions Prepare(TopologyRenderOptions? options) {
        return (options ?? new TopologyRenderOptions()).ForInteractiveHtmlRendering();
    }

    private static string InteractionScriptTag(TopologyRenderOptions options) {
        return "<script>\n" + BuildInteractionScript(options) + "\n</script>";
    }
}

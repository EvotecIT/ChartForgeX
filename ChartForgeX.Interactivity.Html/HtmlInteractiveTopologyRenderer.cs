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
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options, HtmlAssetReferences? externalAssets) =>
        RenderPage(chart, options, externalAssets, null);

    /// <summary>Renders interactive chart controls around an optional trusted SVG presentation of the same topology.</summary>
    /// <remarks>The factory receives an independent policy that retains all content needed by interactive controls,
    /// including initially hidden labels and groups. Null uses the static renderer. Return producer-generated SVG;
    /// this overload does not sanitize arbitrary untrusted markup.</remarks>
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options, HtmlAssetReferences? externalAssets,
        Func<TopologyRenderOptions, string>? svgPresentation) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options = Prepare(options ?? chart.DefaultRenderOptions);
        var presentationSvg = svgPresentation?.Invoke(TopologyHtmlRenderer.PrepareInteractiveSvgOptions(chart, options));
        if (svgPresentation != null && string.IsNullOrWhiteSpace(presentationSvg))
            throw new InvalidOperationException("The SVG presentation factory returned no presentation.");
        var theme = chart.Theme ?? TopologyTheme.Light();
        var title = string.IsNullOrWhiteSpace(chart.Title) ? chart.Labels.UntitledTopology : chart.Title!;
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, TopologyHtmlRenderer.BuildPageStyle(options, theme));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(_staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: false, assetSource: "document", presentationSvg: presentationSvg)).Line()
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

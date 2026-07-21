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
        options = Prepare(options);
        return _staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: true) + InteractionScriptTag(options);
    }

    /// <summary>Renders interactive topology markup without CSS or JavaScript assets.</summary>
    public string RenderFragmentWithoutAssets(TopologyChart chart, TopologyRenderOptions? options = null) {
        options = Prepare(options);
        return _staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: false);
    }

    /// <summary>Renders a complete self-contained interactive topology HTML page.</summary>
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options = Prepare(options);
        var theme = chart.Theme ?? TopologyTheme.Light();
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX topology" : chart.Title!;
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, TopologyHtmlRenderer.BuildPageStyle(options, theme));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(_staticRenderer.RenderInteractiveFragment(chart, options, includeAssets: false, assetSource: "document")).Line()
            .RawTrusted(InteractionScriptTag(options)).Line()
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

    private static TopologyRenderOptions Prepare(TopologyRenderOptions? options) {
        return (options ?? new TopologyRenderOptions()).ForInteractiveHtmlRendering();
    }

    private static string InteractionScriptTag(TopologyRenderOptions options) {
        return "<script>\n" + BuildInteractionScript(options) + "\n</script>";
    }
}

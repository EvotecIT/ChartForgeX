using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Html;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts into simple HTML wrappers with inline SVG.
/// </summary>
public sealed partial class TopologyHtmlRenderer {
    private const string DefaultCssClassPrefix = "cfx-topology";
    private readonly TopologySvgRenderer _svg = new();

    /// <summary>
    /// Renders an embeddable HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An HTML fragment.</returns>
    public string RenderFragment(TopologyChart chart, TopologyRenderOptions? options = null) {
        return RenderFragmentCore(chart, options, includeAssets: true);
    }

    private string RenderFragmentCore(TopologyChart chart, TopologyRenderOptions? options, bool includeAssets) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var id = string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!;
        if (options.View != null && !string.IsNullOrWhiteSpace(options.View.Id)) id += "-" + options.View.Id;
        var theme = chart.Theme ?? TopologyTheme.Light();
        var cssPrefix = CssClassPrefix(options);
        var wrapperClass = cssPrefix + "-wrapper";
        var viewportClass = cssPrefix + "-viewport";
        var controlsClass = cssPrefix + "-controls";
        var scenariosClass = cssPrefix + "-scenarios";
        var scenarioPanelClass = cssPrefix + "-scenario-panel";
        var forceControlsClass = cssPrefix + "-force-controls";
        var enableViewportControls = options.EnableHtmlInteractions && options.EnableHtmlViewportControls;
        var enableExportControls = options.EnableHtmlInteractions && options.EnableHtmlExportControls;
        var enableFullscreenControl = options.EnableHtmlInteractions && options.EnableHtmlFullscreenControl;
        var enableScenarioInteractions = options.EnableHtmlInteractions && chart.Scenarios.Count > 0;
        var isGraphExplorationLayout = chart.LayoutMode == TopologyLayoutMode.ForceDirected || chart.LayoutMode == TopologyLayoutMode.RelationshipRadial;
        var enableForceGraphControls = options.EnableHtmlInteractions && options.EnableHtmlForceGraphControls && isGraphExplorationLayout;
        var renderScenarioControls = enableScenarioInteractions && options.EnableHtmlScenarioControls;
        var enableScenarioPanel = enableScenarioInteractions && options.EnableHtmlScenarioPanel;
        var enableSelectionPanel = options.EnableHtmlInteractions && options.EnableHtmlSelectionPanel;
        var enableScenarioUrlState = enableScenarioInteractions && options.EnableHtmlScenarioUrlState;
        var enableSync = options.EnableHtmlInteractions && options.EnableHtmlSynchronizedState && !string.IsNullOrWhiteSpace(options.HtmlSyncGroupName);
        var syncGroup = enableSync ? options.HtmlSyncGroupName!.Trim() : string.Empty;
        var activeScenarioId = enableScenarioInteractions ? ResolveActiveScenarioId(chart, options.ActiveScenarioId) : options.ActiveScenarioId;
        var forceControlsChart = enableForceGraphControls ? TopologyLayoutEngine.Prepare(chart, options.View, options) : chart;
        var writer = new HtmlMarkupWriter();
        if (includeAssets) {
            writer.StartElement("style")
                .Attribute("data-cfx-topology-assets", "true")
                .RawTrusted(StyleSheet(cssPrefix, CssFontFamily(theme.FontFamily), theme.Background, includePageShell: false))
                .EndElement();
        }

        writer.StartElement("div")
            .Attribute("class", wrapperClass)
            .Attribute("data-chart-id", id)
            .Attribute("data-layout-mode", chart.LayoutMode.ToString())
            .Attribute("data-cfx-interactive", options.EnableHtmlInteractions)
            .Attribute("data-cfx-viewport-controls", enableViewportControls)
            .Attribute("data-cfx-export-controls", enableExportControls)
            .Attribute("data-cfx-fullscreen-control", enableFullscreenControl)
            .Attribute("data-cfx-scenario-controls", enableScenarioInteractions)
            .Attribute("data-cfx-scenario-control-mode", options.HtmlScenarioControlMode == TopologyHtmlScenarioControlMode.Checkboxes ? "checkboxes" : "buttons")
            .Attribute("data-cfx-force-graph-controls", enableForceGraphControls)
            .Attribute("data-cfx-scenario-panel", enableScenarioPanel)
            .Attribute("data-cfx-selection-panel", enableSelectionPanel)
            .Attribute("data-cfx-controls-placement", ControlPlacementValue(options.HtmlControlPlacement))
            .Attribute("data-cfx-scenario-url-state", enableScenarioUrlState)
            .Attribute("data-cfx-active-scenario", string.IsNullOrWhiteSpace(activeScenarioId) ? null : activeScenarioId)
            .Attribute("data-cfx-sync-enabled", enableSync)
            .Attribute("data-cfx-sync-group", syncGroup)
            .Attribute("style", "width:100%;max-width:" + chart.Viewport.Width.ToString("0.###", CultureInfo.InvariantCulture) + "px;box-sizing:border-box;overflow:visible")
            .EndStartElement();
        if (renderScenarioControls) WriteScenarioControls(writer, scenariosClass, chart, activeScenarioId, options.HtmlScenarioControlMode);
        if (enableScenarioPanel) WriteScenarioPanel(writer, scenarioPanelClass, chart, activeScenarioId, enableScenarioUrlState);
        if (enableForceGraphControls) WriteForceGraphControls(writer, forceControlsClass, forceControlsChart, options);
        writer.StartElement("div").Attribute("class", viewportClass).EndStartElement();
        if (enableViewportControls || enableExportControls || enableFullscreenControl) {
            writer.StartElement("div").Attribute("class", controlsClass).Attribute("aria-label", "Topology controls").EndStartElement();
            if (enableViewportControls) {
                WriteIconButton(writer, "data-cfx-topology-zoom", "in", "Zoom in", "Zoom in", null, IconPlus());
                WriteIconButton(writer, "data-cfx-topology-zoom", "out", "Zoom out", "Zoom out", null, IconMinus());
                WriteIconButton(writer, "data-cfx-topology-fit", "true", "Fit topology", "Fit topology", null, IconFit());
                WriteIconButton(writer, "data-cfx-topology-reset", "true", "Reset viewport", "Reset viewport", null, IconReset());
            }

            if (enableFullscreenControl) {
                WriteIconButton(writer, "data-cfx-topology-fullscreen", "true", "Open topology fullscreen", "Open topology fullscreen", null, IconFullscreen());
            }

            if (enableExportControls) {
                WriteIconButton(writer, "data-cfx-topology-export", "svg", "Export SVG", "Export SVG", null, IconDownload(), "SVG");
                WriteIconButton(writer, "data-cfx-topology-export", "png", "Export PNG", "Export PNG", null, IconImage(), "PNG");
            }

            writer.EndElement();
        }

        if (enableSelectionPanel) WriteSelectionPanel(writer, cssPrefix + "-selection-panel");

        writer.RawTrusted(RenderEmbeddedSvg(chart, options, enableScenarioInteractions, enableForceGraphControls));
        writer.EndElement().EndElement();
        if (includeAssets && options.EnableHtmlInteractions) writer.RawTrusted(InteractionScript(cssPrefix));
        return writer.Build();
    }

    private string RenderEmbeddedSvg(TopologyChart chart, TopologyRenderOptions options, bool interactiveScenarioControls, bool forceGraphControls) {
        if (!interactiveScenarioControls && !forceGraphControls) return _svg.Render(chart, options);
        var activeScenarioId = options.ActiveScenarioId;
        var includeEdgeLabels = options.IncludeEdgeLabels;
        var includeGroups = options.IncludeGroups;
        try {
            if (interactiveScenarioControls) options.ActiveScenarioId = null;
            if (forceGraphControls) {
                options.IncludeEdgeLabels = true;
                options.IncludeGroups = true;
            }

            return _svg.Render(chart, options);
        } finally {
            options.ActiveScenarioId = activeScenarioId;
            options.IncludeEdgeLabels = includeEdgeLabels;
            options.IncludeGroups = includeGroups;
        }
    }

    /// <summary>
    /// Renders a complete static HTML page containing one topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A complete HTML page.</returns>
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var theme = chart.Theme ?? TopologyTheme.Light();
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX topology" : chart.Title!;
        var cssPrefix = CssClassPrefix(options);
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, StyleSheet(cssPrefix, CssFontFamily(theme.FontFamily), theme.Background));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(RenderFragmentCore(chart, options, includeAssets: false)).Line();
        if (options.EnableHtmlInteractions) writer.RawTrusted(InteractionScript(cssPrefix)).Line();
        writer.EndElement().Line()
            .EndElement().Line();
        return writer.Build();
    }

    private static void WriteIconButton(HtmlMarkupWriter writer, string dataAttribute, string dataValue, string title, string ariaLabel, bool? pressed, string icon, string? text = null) {
        writer.StartElement("button")
            .Attribute("type", "button")
            .Attribute(dataAttribute, dataValue)
            .Attribute("title", title)
            .Attribute("aria-label", ariaLabel)
            .Attribute("aria-pressed", pressed)
            .EndStartElement()
            .RawTrusted(icon);
        if (!string.IsNullOrWhiteSpace(text)) {
            writer.StartElement("span").EndStartElement().Text(text!).EndElement();
        }

        writer
            .EndElement();
    }

    private static void WriteButton(HtmlMarkupWriter writer, string dataAttribute, string dataValue, string title, string ariaLabel, bool? pressed, string text) {
        writer.StartElement("button")
            .Attribute("type", "button")
            .Attribute(dataAttribute, dataValue)
            .Attribute("title", title)
            .Attribute("aria-label", ariaLabel)
            .Attribute("aria-pressed", pressed)
            .EndStartElement()
            .Text(text)
            .EndElement();
    }

    private static void WriteSelectionPanel(HtmlMarkupWriter writer, string className) {
        writer.StartElement("aside")
            .Attribute("class", className)
            .Attribute("data-cfx-topology-selection-panel", "true")
            .Attribute("aria-live", "polite")
            .BooleanAttribute("hidden")
            .EndStartElement()
            .StartElement("div").Attribute("class", className + "__head").EndStartElement()
            .StartElement("span").Attribute("class", className + "__eyebrow").Attribute("data-cfx-selection-kind", "true").EndStartElement().Text("Selection").EndElement()
            .StartElement("strong").Attribute("class", className + "__title").Attribute("data-cfx-selection-title", "true").EndStartElement().Text("No item selected").EndElement()
            .StartElement("span").Attribute("class", className + "__status").Attribute("data-cfx-selection-status", "true").EndStartElement().EndElement()
            .EndElement()
            .StartElement("dl").Attribute("class", className + "__facts").Attribute("data-cfx-selection-facts", "true").EndStartElement().EndElement()
            .StartElement("div").Attribute("class", className + "__meta").Attribute("data-cfx-selection-meta", "true").EndStartElement().EndElement()
            .StartElement("div").Attribute("class", className + "__related").Attribute("data-cfx-selection-related", "true").EndStartElement().EndElement()
            .EndElement();
    }

    private static string IconPlus() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M12 5v14M5 12h14\"/></svg>";

    private static string IconMinus() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M5 12h14\"/></svg>";

    private static string IconFit() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M8 3H3v5M16 3h5v5M8 21H3v-5M16 21h5v-5M9 9h6v6H9z\"/></svg>";

    private static string IconReset() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M5 12a7 7 0 1 0 2.05-4.95M5 5v5h5\"/></svg>";

    private static string IconFullscreen() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M8 3H3v5M16 3h5v5M8 21H3v-5M16 21h5v-5\"/></svg>";

    private static string IconDownload() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M12 3v12M7 10l5 5 5-5M5 21h14\"/></svg>";

    private static string IconImage() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M5 5h14v14H5zM8 15l3-3 2 2 3-4 3 5M8 8h.01\"/></svg>";

    private static void WriteForceGraphControls(HtmlMarkupWriter writer, string className, TopologyChart chart, TopologyRenderOptions options) {
        writer.StartElement("div")
            .Attribute("class", className)
            .Attribute("data-cfx-force-graph-panel", "true")
            .Attribute("aria-label", "Force graph controls")
            .EndStartElement();
        writer.StartElement("div").Attribute("class", className + "__row").EndStartElement();
        writer.StartElement("input")
            .Attribute("type", "search")
            .Attribute("data-cfx-force-search", "true")
            .Attribute("placeholder", "Find node, edge, group")
            .Attribute("aria-label", "Find node, edge, or group")
            .EndVoidElement();
        WriteForceSelect(writer, "data-cfx-force-status", "Status", new[] { "All", "Healthy", "Warning", "Critical", "Unknown", "Disabled" });
        WriteGroupSelect(writer, chart);
        writer.EndElement();
        writer.StartElement("div").Attribute("class", className + "__toggles").EndStartElement();
        WriteToggle(writer, "data-cfx-force-toggle", "edges", "Edges", true);
        WriteToggle(writer, "data-cfx-force-toggle", "focus", "Focus", true);
        WriteToggle(writer, "data-cfx-force-toggle", "edge-labels", "Labels", options.IncludeEdgeLabels);
        WriteToggle(writer, "data-cfx-force-toggle", "groups", "Groups", options.IncludeGroups);
        WriteToggle(writer, "data-cfx-force-toggle", "hide-moving-edges", "Quiet pan", true);
        writer.EndElement();
        writer.StartElement("output").Attribute("data-cfx-force-summary", "true").EndStartElement().Text("Graph ready").EndElement();
        writer.EndElement();
    }

    private static void WriteForceSelect(HtmlMarkupWriter writer, string dataAttribute, string ariaLabel, string[] values) {
        writer.StartElement("select").Attribute(dataAttribute, "true").Attribute("aria-label", ariaLabel).EndStartElement();
        foreach (var value in values) writer.StartElement("option").Attribute("value", value == "All" ? string.Empty : value).EndStartElement().Text(value).EndElement();
        writer.EndElement();
    }

    private static void WriteGroupSelect(HtmlMarkupWriter writer, TopologyChart chart) {
        writer.StartElement("select").Attribute("data-cfx-force-group", "true").Attribute("aria-label", "Group").EndStartElement();
        writer.StartElement("option").Attribute("value", string.Empty).EndStartElement().Text("All groups").EndElement();
        foreach (var group in chart.Groups.Where(group => !string.IsNullOrWhiteSpace(group.Id)).OrderBy(group => group.Label, StringComparer.OrdinalIgnoreCase)) {
            writer.StartElement("option").Attribute("value", group.Id).EndStartElement().Text(string.IsNullOrWhiteSpace(group.Label) ? group.Id : group.Label).EndElement();
        }

        writer.EndElement();
    }

    private static void WriteToggle(HtmlMarkupWriter writer, string dataAttribute, string dataValue, string text, bool isChecked) {
        writer.StartElement("label").EndStartElement();
        writer.StartElement("input")
            .Attribute("type", "checkbox")
            .Attribute(dataAttribute, dataValue)
            .Attribute("checked", isChecked ? "checked" : null)
            .EndVoidElement();
        writer.Text(text).EndElement();
    }

    private static string StyleSheet(string cssPrefix, string fontFamily, string background, bool includePageShell = true) {
        var stylesheet = string.Empty;
        if (includePageShell) {
            stylesheet += HtmlSurfacePolish.ReportBodyCss(background, fontFamily, "24px");
        }

        stylesheet += TopologyHtmlAssets.Style.Replace("__CFX_FONT_FAMILY__", fontFamily);
        if (includePageShell) {
            stylesheet += HtmlSurfacePolish.ResponsiveCenteredBodyCss;
            stylesheet += HtmlSurfacePolish.PrintBodyCss("0", ".cfx-topology-wrapper{width:100%;max-width:none}.cfx-topology-wrapper svg{width:100%;height:auto}.cfx-topology-scenarios,.cfx-topology-scenario-panel{display:none}");
        }

        return stylesheet
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace(".cfx-topology-controls", "." + cssPrefix + "-controls")
            .Replace(".cfx-topology-scenarios", "." + cssPrefix + "-scenarios")
            .Replace(".cfx-topology-scenario-panel", "." + cssPrefix + "-scenario-panel")
            .Replace(".cfx-topology-selection-panel", "." + cssPrefix + "-selection-panel")
            .Replace(".cfx-topology-force-controls", "." + cssPrefix + "-force-controls")
            .Replace(".cfx-topology__edge", "." + cssPrefix + "__edge")
            .Replace(".cfx-topology__node-card", "." + cssPrefix + "__node-card")
            .Replace(".cfx-topology-html-", "." + cssPrefix + "-html-");
    }

    private static string CssClassPrefix(TopologyRenderOptions options) {
        return NormalizeCssClassPrefix(options.CssClassPrefix, DefaultCssClassPrefix);
    }

    private static string ControlPlacementValue(TopologyHtmlControlPlacement placement) => placement switch {
        TopologyHtmlControlPlacement.TopRight => "top-right",
        TopologyHtmlControlPlacement.LeftRail => "left-rail",
        TopologyHtmlControlPlacement.RightRail => "right-rail",
        TopologyHtmlControlPlacement.BottomLeft => "bottom-left",
        TopologyHtmlControlPlacement.BottomRight => "bottom-right",
        _ => "top-left"
    };

}

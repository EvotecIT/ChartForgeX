using System;
using System.Globalization;
using System.Text;
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
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var id = string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!;
        if (options.View != null && !string.IsNullOrWhiteSpace(options.View.Id)) id += "-" + options.View.Id;
        var cssPrefix = CssClassPrefix(options);
        var wrapperClass = cssPrefix + "-wrapper";
        var viewportClass = cssPrefix + "-viewport";
        var controlsClass = cssPrefix + "-controls";
        var scenariosClass = cssPrefix + "-scenarios";
        var scenarioPanelClass = cssPrefix + "-scenario-panel";
        var enableViewportControls = options.EnableHtmlInteractions && options.EnableHtmlViewportControls;
        var enableExportControls = options.EnableHtmlInteractions && options.EnableHtmlExportControls;
        var enableScenarioInteractions = options.EnableHtmlInteractions && chart.Scenarios.Count > 0;
        var renderScenarioControls = enableScenarioInteractions && options.EnableHtmlScenarioControls;
        var enableScenarioPanel = enableScenarioInteractions && options.EnableHtmlScenarioPanel;
        var enableScenarioUrlState = enableScenarioInteractions && options.EnableHtmlScenarioUrlState;
        var enableSync = options.EnableHtmlInteractions && options.EnableHtmlSynchronizedState && !string.IsNullOrWhiteSpace(options.HtmlSyncGroupName);
        var syncGroup = enableSync ? options.HtmlSyncGroupName!.Trim() : string.Empty;
        var activeScenarioId = enableScenarioInteractions ? ResolveActiveScenarioId(chart, options.ActiveScenarioId) : options.ActiveScenarioId;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("div")
            .Attribute("class", wrapperClass)
            .Attribute("data-chart-id", id)
            .Attribute("data-layout-mode", chart.LayoutMode.ToString())
            .Attribute("data-cfx-interactive", options.EnableHtmlInteractions)
            .Attribute("data-cfx-viewport-controls", enableViewportControls)
            .Attribute("data-cfx-export-controls", enableExportControls)
            .Attribute("data-cfx-scenario-controls", enableScenarioInteractions)
            .Attribute("data-cfx-scenario-panel", enableScenarioPanel)
            .Attribute("data-cfx-scenario-url-state", enableScenarioUrlState)
            .Attribute("data-cfx-active-scenario", string.IsNullOrWhiteSpace(activeScenarioId) ? null : activeScenarioId)
            .Attribute("data-cfx-sync-enabled", enableSync)
            .Attribute("data-cfx-sync-group", syncGroup)
            .Attribute("style", "width:100%;max-width:" + chart.Viewport.Width.ToString("0.###", CultureInfo.InvariantCulture) + "px;box-sizing:border-box;overflow:visible")
            .EndStartElement();
        if (renderScenarioControls) WriteScenarioControls(writer, scenariosClass, chart, activeScenarioId);
        if (enableScenarioPanel) WriteScenarioPanel(writer, scenarioPanelClass, chart, activeScenarioId, enableScenarioUrlState);
        writer.StartElement("div").Attribute("class", viewportClass).EndStartElement();
        if (enableViewportControls || enableExportControls) {
            writer.StartElement("div").Attribute("class", controlsClass).Attribute("aria-label", "Topology controls").EndStartElement();
            if (enableViewportControls) {
                WriteButton(writer, "data-cfx-topology-zoom", "in", "Zoom in", "Zoom in", null, "+");
                WriteButton(writer, "data-cfx-topology-zoom", "out", "Zoom out", "Zoom out", null, "-");
                WriteButton(writer, "data-cfx-topology-mode", "pan", "Pan topology", "Pan topology", false, "Pan");
                WriteButton(writer, "data-cfx-topology-reset", "true", "Reset viewport", "Reset viewport", null, "0");
            }

            if (enableExportControls) {
                WriteButton(writer, "data-cfx-topology-export", "svg", "Export SVG", "Export SVG", null, "SVG");
                WriteButton(writer, "data-cfx-topology-export", "png", "Export PNG", "Export PNG", null, "PNG");
            }

            writer.EndElement();
        }

        writer.RawTrusted(RenderEmbeddedSvg(chart, options, enableScenarioInteractions));
        writer.EndElement().EndElement();
        return writer.Build();
    }

    private string RenderEmbeddedSvg(TopologyChart chart, TopologyRenderOptions options, bool interactiveScenarioControls) {
        if (!interactiveScenarioControls) return _svg.Render(chart, options);
        var activeScenarioId = options.ActiveScenarioId;
        try {
            options.ActiveScenarioId = null;
            return _svg.Render(chart, options);
        } finally {
            options.ActiveScenarioId = activeScenarioId;
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
            .RawTrusted(RenderFragment(chart, options)).Line();
        if (options.EnableHtmlInteractions) writer.RawTrusted(InteractionScript(cssPrefix)).Line();
        writer.EndElement().Line()
            .EndElement().Line();
        return writer.Build();
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

    private static string StyleSheet(string cssPrefix, string fontFamily, string background) {
        var stylesheet = new StringBuilder()
            .Append(HtmlSurfacePolish.ReportBodyCss(background, fontFamily, "24px"))
            .Append(".cfx-topology-wrapper{margin:0 auto;overflow:visible}")
            .Append(".cfx-topology-viewport{position:relative}")
            .Append(".cfx-topology-wrapper svg{max-width:100%;height:auto;display:block;overflow:visible}")
            .Append(".cfx-topology-wrapper[data-cfx-viewport-controls='true'] .cfx-topology-viewport{overflow:hidden;touch-action:none}")
            .Append(".cfx-topology-wrapper[data-cfx-viewport-controls='true'] svg{transform:translate(var(--cfx-topology-pan-x,0),var(--cfx-topology-pan-y,0)) scale(var(--cfx-topology-zoom,1));transform-origin:center center;transition:transform .12s ease;will-change:transform}")
            .Append(".cfx-topology-controls{position:absolute;z-index:5;top:12px;left:12px;display:grid;gap:6px}")
            .Append(".cfx-topology-controls button,.cfx-topology-scenarios button{min-width:34px;height:34px;border:1px solid rgba(37,99,235,.28);border-radius:6px;background:rgba(255,255,255,.92);color:#1e3a8a;cursor:pointer;font:700 12px/1 ")
            .Append(fontFamily)
            .Append(";box-shadow:0 8px 18px rgba(15,23,42,.1)}")
            .Append(".cfx-topology-scenarios{display:flex;flex-wrap:wrap;gap:8px;margin:0 0 10px;align-items:center}")
            .Append(".cfx-topology-scenarios button{height:32px;padding:0 12px;border-color:color-mix(in srgb,var(--cfx-topology-scenario-color,#2563eb) 32%,transparent);color:#0f172a}")
            .Append(".cfx-topology-scenarios button:before{content:'';display:inline-block;width:8px;height:8px;border-radius:50%;margin-right:7px;background:var(--cfx-topology-scenario-color,#64748b)}")
            .Append(".cfx-topology-scenario-panel{display:grid;gap:8px;max-width:760px;margin:0 0 12px;padding:10px 12px;border:1px solid rgba(148,163,184,.42);border-radius:8px;background:rgba(255,255,255,.88);box-shadow:0 10px 24px rgba(15,23,42,.07);color:#0f172a}")
            .Append(".cfx-topology-scenario-panel__summary{display:grid;gap:2px}")
            .Append(".cfx-topology-scenario-panel__title{font:700 14px/1.25 ")
            .Append(fontFamily)
            .Append(";color:var(--cfx-topology-scenario-color,#0f172a)}")
            .Append(".cfx-topology-scenario-panel__meta{font:500 12px/1.35 ")
            .Append(fontFamily)
            .Append(";color:#475569}")
            .Append(".cfx-topology-scenario-panel__controls{display:flex;flex-wrap:wrap;gap:6px}")
            .Append(".cfx-topology-scenario-panel__controls button{min-height:28px;padding:4px 9px;border:1px solid rgba(148,163,184,.4);border-radius:6px;background:#fff;color:#0f172a;cursor:pointer;font:700 11px/1 ")
            .Append(fontFamily)
            .Append("}")
            .Append(".cfx-topology-scenario-panel__controls button:hover,.cfx-topology-scenario-panel__controls button:focus-visible,.cfx-topology-scenario-panel__controls button[aria-pressed='true']{border-color:var(--cfx-topology-scenario-color,#2563eb);color:var(--cfx-topology-scenario-color,#2563eb);background:#eff6ff}")
            .Append(".cfx-topology-scenario-panel__steps{display:flex;flex-wrap:wrap;gap:6px;margin:0;padding:0;list-style:none}")
            .Append(".cfx-topology-scenario-panel__steps li{display:flex;align-items:center;gap:6px;min-height:24px;padding:3px 8px;border:1px solid rgba(148,163,184,.34);border-radius:6px;background:rgba(248,250,252,.82);cursor:pointer;font:700 11px/1.2 ")
            .Append(fontFamily)
            .Append(";color:#0f172a}")
            .Append(".cfx-topology-scenario-panel__steps li:before{content:attr(data-cfx-scenario-step-index);display:inline-grid;place-items:center;width:16px;height:16px;border-radius:50%;background:var(--cfx-topology-scenario-color,#2563eb);color:#fff;font-size:10px}")
            .Append(".cfx-topology-scenario-panel__steps li[aria-current='step']{border-color:var(--cfx-topology-scenario-color,#2563eb);background:#eff6ff;color:#0f172a}")
            .Append(".cfx-topology-controls button:hover,.cfx-topology-controls button:focus-visible,.cfx-topology-controls button[aria-pressed='true'],.cfx-topology-scenarios button:hover,.cfx-topology-scenarios button:focus-visible,.cfx-topology-scenarios button[aria-pressed='true']{border-color:var(--cfx-topology-scenario-color,#2563eb);color:#0f172a;background:#eff6ff}")
            .Append(".cfx-topology-wrapper[data-cfx-topology-mode='pan'] .cfx-topology-viewport{cursor:grab}")
            .Append(".cfx-topology-wrapper[data-cfx-topology-mode='pan'] .cfx-topology-viewport:active{cursor:grabbing}")
            .Append(".cfx-topology-wrapper [data-cfx-role='topology-node'],.cfx-topology-wrapper [data-cfx-role='topology-edge'],.cfx-topology-wrapper [data-cfx-role='topology-group']{cursor:pointer}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-selected{filter:drop-shadow(0 10px 18px rgba(37,99,235,.22))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-muted{opacity:.32}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-related{opacity:1}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-hovered{filter:drop-shadow(0 8px 16px rgba(15,23,42,.16))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-hover-muted{opacity:.42}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-hover-related{opacity:1}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-muted{opacity:.18}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-active{opacity:1;filter:drop-shadow(0 10px 18px rgba(37,99,235,.18))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-active .cfx-topology__edge{stroke:var(--cfx-topology-active-scenario-color,#2563eb)!important;stroke-width:5!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-active .cfx-topology__node-card{stroke:var(--cfx-topology-active-scenario-color,#2563eb)!important;stroke-width:2.4!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview-muted{opacity:.34}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview{opacity:1;filter:drop-shadow(0 8px 14px rgba(15,23,42,.16))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview .cfx-topology__edge{stroke:var(--cfx-topology-preview-scenario-color,#2563eb)!important;stroke-width:4!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview .cfx-topology__node-card{stroke:var(--cfx-topology-preview-scenario-color,#2563eb)!important;stroke-width:2!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-muted{opacity:.38}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-active{opacity:1;filter:drop-shadow(0 10px 18px rgba(15,23,42,.2))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-active .cfx-topology__edge{stroke-width:6!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-active .cfx-topology__node-card{stroke-width:3!important}")
            .Append(HtmlSurfacePolish.ResponsiveCenteredBodyCss)
            .Append(HtmlSurfacePolish.PrintBodyCss("0", ".cfx-topology-wrapper{width:100%;max-width:none}.cfx-topology-wrapper svg{width:100%;height:auto}.cfx-topology-scenarios,.cfx-topology-scenario-panel{display:none}"))
            .ToString();
        return stylesheet
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace(".cfx-topology-controls", "." + cssPrefix + "-controls")
            .Replace(".cfx-topology-scenarios", "." + cssPrefix + "-scenarios")
            .Replace(".cfx-topology-scenario-panel", "." + cssPrefix + "-scenario-panel")
            .Replace(".cfx-topology__edge", "." + cssPrefix + "__edge")
            .Replace(".cfx-topology__node-card", "." + cssPrefix + "__node-card")
            .Replace(".cfx-topology-html-", "." + cssPrefix + "-html-");
    }

    private static string CssClassPrefix(TopologyRenderOptions options) {
        return NormalizeCssClassPrefix(options.CssClassPrefix, DefaultCssClassPrefix);
    }

}

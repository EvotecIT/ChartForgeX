using System;
using ChartForgeX.Core;
using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders a ChartForgeX chart as a self-contained interactive HTML document.
/// </summary>
public sealed class HtmlInteractiveChartRenderer {
    /// <summary>
    /// Renders the specified chart to a complete interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(Chart chart) => RenderPage(chart, null);

    /// <summary>
    /// Renders the specified chart to a complete interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML interaction adapter.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(Chart chart, Action<HtmlChartInteractionOptions>? configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions();
        configure?.Invoke(options);

        var title = options.PageTitle ?? ChartTitle(chart, "ChartForgeX interactive chart");
        var writer = HtmlInteractivePage.StartDocument(title);
        writer.StartElement("main").Attribute("class", "cfx-shell").EndStartElement().Line()
            .RawTrusted(BuildChartSection(chart, options, title)).Line()
            .EndElement().Line();
        HtmlInteractivePage.EndDocument(writer, options.ScriptNonce);
        return writer.Build();
    }

    internal static string InteractiveStyle => HtmlInteractiveAssets.Style;

    internal static string InteractiveScript => HtmlInteractiveAssets.Script;

    internal static string BuildChartSection(Chart chart, HtmlChartInteractionOptions options, string titleFallback) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (options == null) throw new ArgumentNullException(nameof(options));
        var scope = options.IdScope ?? options.Interaction.ChartId ?? Slugify(ChartTitle(chart, titleFallback));
        var chartId = options.Interaction.ChartId ?? scope;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("section")
            .Attribute("class", "cfx-interactive-chart")
            .Attribute("data-cfx-chart-id", chartId)
            .Attribute("data-cfx-interaction-features", options.Interaction.Features.ToString())
            .Attribute("data-cfx-interaction-group", options.Interaction.GroupName)
            .EndStartElement().Line()
            .StartElement("div").Attribute("class", "cfx-toolbar").EndStartElement()
            .RawTrusted(BuildToolbar(options))
            .EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-stage").EndStartElement().Line()
            .RawTrusted(chart.ToSvg(scope)).Line()
            .StartElement("div").Attribute("class", "cfx-brush-box").BooleanAttribute("hidden").EndStartElement().EndElement().Line()
            .EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-tooltip").Attribute("role", "status").Attribute("aria-live", "polite").BooleanAttribute("hidden").EndStartElement().EndElement().Line()
            .EndElement();
        return writer.Build();
    }

    private static string BuildToolbar(HtmlChartInteractionOptions options) {
        var writer = new HtmlMarkupWriter();
        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Zoom)) {
            AppendToolbarButton(writer, "Zoom +", ("data-cfx-zoom", "in"), ("title", "Zoom in"));
            AppendToolbarButton(writer, "Zoom -", ("data-cfx-zoom", "out"), ("title", "Zoom out"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Pan)) {
            AppendToolbarButton(writer, "Pan", ("data-cfx-mode-button", "pan"), ("aria-pressed", "false"), ("title", "Pan chart"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Brush)) {
            AppendToolbarButton(writer, "Brush", ("data-cfx-mode-button", "brush"), ("aria-pressed", "false"), ("title", "Brush select region"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Export)) {
            AppendToolbarButton(writer, "SVG", ("data-cfx-export", "svg"), ("title", "Download SVG"));
            AppendToolbarButton(writer, "PNG", ("data-cfx-export", "png"), ("title", "Download PNG"));
        }

        if (options.IncludeResetButton) {
            AppendToolbarButton(writer, "Reset", ("data-cfx-reset", "true"));
        }

        return writer.Build();
    }

    private static void AppendToolbarButton(HtmlMarkupWriter writer, string label, params (string Name, string Value)[] attributes) {
        writer.StartElement("button")
            .Attribute("class", "cfx-tool")
            .Attribute("type", "button");
        for (var i = 0; i < attributes.Length; i++) {
            writer.Attribute(attributes[i].Name, attributes[i].Value);
        }

        writer.EndStartElement().Text(label).EndElement();
    }

    internal static string ChartTitle(Chart chart, string fallback) => string.IsNullOrWhiteSpace(chart.Title) ? fallback : chart.Title;

    internal static string Slugify(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var sb = new System.Text.StringBuilder(value.Length);
        var previousDash = false;
        foreach (var ch in value) {
            if (char.IsLetterOrDigit(ch)) {
                sb.Append(char.ToLowerInvariant(ch));
                previousDash = false;
            } else if (!previousDash && sb.Length > 0) {
                sb.Append('-');
                previousDash = true;
            }
        }

        if (previousDash && sb.Length > 0) sb.Length--;
        return sb.Length == 0 ? "chart" : sb.ToString();
    }
}

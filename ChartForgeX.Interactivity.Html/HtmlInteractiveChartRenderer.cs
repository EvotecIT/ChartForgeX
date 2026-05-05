using System;
using System.Text;
using ChartForgeX.Core;

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
        var sb = new StringBuilder();
        HtmlInteractivePage.AppendDocumentStart(sb, title);
        sb.AppendLine("<main class=\"cfx-shell\">");
        sb.AppendLine(BuildChartSection(chart, options, title));
        sb.AppendLine("</main>");
        HtmlInteractivePage.AppendDocumentEnd(sb, options.ScriptNonce);
        return sb.ToString();
    }

    internal static string InteractiveStyle => HtmlInteractiveAssets.Style;

    internal static string InteractiveScript => HtmlInteractiveAssets.Script;

    internal static string BuildChartSection(Chart chart, HtmlChartInteractionOptions options, string titleFallback) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (options == null) throw new ArgumentNullException(nameof(options));
        var scope = options.IdScope ?? options.Interaction.ChartId ?? Slugify(ChartTitle(chart, titleFallback));
        var chartId = options.Interaction.ChartId ?? scope;
        var sb = new StringBuilder();
        HtmlInteractiveMarkup.AppendStartTag(
            sb,
            "section",
            HtmlInteractiveMarkup.Attr("class", "cfx-interactive-chart"),
            HtmlInteractiveMarkup.Attr("data-cfx-chart-id", chartId),
            HtmlInteractiveMarkup.Attr("data-cfx-interaction-features", options.Interaction.Features.ToString()),
            HtmlInteractiveMarkup.OptionalAttr("data-cfx-interaction-group", options.Interaction.GroupName));
        sb.AppendLine();
        HtmlInteractiveMarkup.AppendElement(sb, "div", BuildToolbar(options), HtmlInteractiveMarkup.Attr("class", "cfx-toolbar"));
        sb.AppendLine();
        sb.AppendLine("<div class=\"cfx-stage\">");
        sb.AppendLine(chart.ToSvg(scope));
        sb.AppendLine("<div class=\"cfx-brush-box\" hidden></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"cfx-tooltip\" role=\"status\" aria-live=\"polite\" hidden></div>");
        sb.AppendLine("</section>");
        return sb.ToString();
    }

    private static string BuildToolbar(HtmlChartInteractionOptions options) {
        var sb = new StringBuilder();
        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Zoom)) {
            AppendToolbarButton(sb, "Zoom +", HtmlInteractiveMarkup.Attr("data-cfx-zoom", "in"), HtmlInteractiveMarkup.Attr("title", "Zoom in"));
            AppendToolbarButton(sb, "Zoom -", HtmlInteractiveMarkup.Attr("data-cfx-zoom", "out"), HtmlInteractiveMarkup.Attr("title", "Zoom out"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Pan)) {
            AppendToolbarButton(sb, "Pan", HtmlInteractiveMarkup.Attr("data-cfx-mode-button", "pan"), HtmlInteractiveMarkup.Attr("aria-pressed", "false"), HtmlInteractiveMarkup.Attr("title", "Pan chart"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Brush)) {
            AppendToolbarButton(sb, "Brush", HtmlInteractiveMarkup.Attr("data-cfx-mode-button", "brush"), HtmlInteractiveMarkup.Attr("aria-pressed", "false"), HtmlInteractiveMarkup.Attr("title", "Brush select region"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Export)) {
            AppendToolbarButton(sb, "SVG", HtmlInteractiveMarkup.Attr("data-cfx-export", "svg"), HtmlInteractiveMarkup.Attr("title", "Download SVG"));
            AppendToolbarButton(sb, "PNG", HtmlInteractiveMarkup.Attr("data-cfx-export", "png"), HtmlInteractiveMarkup.Attr("title", "Download PNG"));
        }

        if (options.IncludeResetButton) {
            AppendToolbarButton(sb, "Reset", HtmlInteractiveMarkup.Attr("data-cfx-reset", "true"));
        }

        return sb.ToString();
    }

    private static void AppendToolbarButton(StringBuilder sb, string label, params HtmlInteractiveMarkup.HtmlAttribute?[] attributes) {
        var merged = new HtmlInteractiveMarkup.HtmlAttribute?[attributes.Length + 2];
        merged[0] = HtmlInteractiveMarkup.Attr("class", "cfx-tool");
        merged[1] = HtmlInteractiveMarkup.Attr("type", "button");
        for (var i = 0; i < attributes.Length; i++) {
            merged[i + 2] = attributes[i];
        }

        sb.Append(HtmlInteractiveMarkup.Button(label, merged));
    }

    internal static string EscapeHtml(string value) => System.Net.WebUtility.HtmlEncode(value);

    internal static string ChartTitle(Chart chart, string fallback) => string.IsNullOrWhiteSpace(chart.Title) ? fallback : chart.Title;

    internal static string Slugify(string value) {
        var sb = new StringBuilder(value.Length);
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

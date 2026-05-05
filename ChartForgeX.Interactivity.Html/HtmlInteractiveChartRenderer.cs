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
        var group = options.Interaction.GroupName == null ? string.Empty : " data-cfx-interaction-group=\"" + EscapeHtml(options.Interaction.GroupName) + "\"";
        var sb = new StringBuilder();
        sb.AppendLine("<section class=\"cfx-interactive-chart\" data-cfx-chart-id=\"" + EscapeHtml(chartId) + "\" data-cfx-interaction-features=\"" + EscapeHtml(options.Interaction.Features.ToString()) + "\"" + group + ">");
        sb.AppendLine("<div class=\"cfx-toolbar\">" + BuildToolbar(options) + "</div>");
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
            sb.Append("<button class=\"cfx-tool\" type=\"button\" data-cfx-zoom=\"in\" title=\"Zoom in\">Zoom +</button>");
            sb.Append("<button class=\"cfx-tool\" type=\"button\" data-cfx-zoom=\"out\" title=\"Zoom out\">Zoom -</button>");
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Pan)) {
            sb.Append("<button class=\"cfx-tool\" type=\"button\" data-cfx-mode-button=\"pan\" aria-pressed=\"false\" title=\"Pan chart\">Pan</button>");
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Brush)) {
            sb.Append("<button class=\"cfx-tool\" type=\"button\" data-cfx-mode-button=\"brush\" aria-pressed=\"false\" title=\"Brush select region\">Brush</button>");
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Export)) {
            sb.Append("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"svg\" title=\"Download SVG\">SVG</button>");
            sb.Append("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"png\" title=\"Download PNG\">PNG</button>");
        }

        if (options.IncludeResetButton) {
            sb.Append("<button class=\"cfx-tool\" type=\"button\" data-cfx-reset=\"true\">Reset</button>");
        }

        return sb.ToString();
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

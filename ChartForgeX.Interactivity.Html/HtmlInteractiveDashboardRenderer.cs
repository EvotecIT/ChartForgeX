using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders several ChartForgeX charts as one self-contained interactive HTML dashboard.
/// </summary>
public sealed class HtmlInteractiveDashboardRenderer {
    /// <summary>
    /// Renders the specified charts to a complete interactive dashboard document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(IEnumerable<Chart> charts) => RenderPage(charts, null);

    /// <summary>
    /// Renders the specified charts to a complete interactive dashboard document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML dashboard adapter.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(IEnumerable<Chart> charts, Action<HtmlInteractiveDashboardOptions>? configure) {
        if (charts == null) throw new ArgumentNullException(nameof(charts));
        var chartArray = charts.ToArray();
        if (chartArray.Length == 0) throw new ArgumentException("Interactive dashboards require at least one chart.", nameof(charts));
        for (var i = 0; i < chartArray.Length; i++) {
            if (chartArray[i] == null) throw new ArgumentException("Interactive dashboards cannot contain null charts.", nameof(charts));
        }

        var options = new HtmlInteractiveDashboardOptions();
        configure?.Invoke(options);
        var title = options.PageTitle ?? "ChartForgeX interactive dashboard";
        var scope = options.IdScope ?? HtmlInteractiveChartRenderer.Slugify(title);
        var groupName = options.Interaction.GroupName ?? scope;

        var sb = new StringBuilder();
        HtmlInteractivePage.AppendDocumentStart(sb, title);
        sb.AppendLine("<main class=\"cfx-shell\">");
        sb.AppendLine("<div class=\"cfx-dashboard\" style=\"--cfx-dashboard-columns:" + options.Columns.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\">");
        for (var i = 0; i < chartArray.Length; i++) {
            var childOptions = CreateChildOptions(options, scope, groupName, i);
            sb.AppendLine(HtmlInteractiveChartRenderer.BuildChartSection(chartArray[i], childOptions, title));
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</main>");
        HtmlInteractivePage.AppendDocumentEnd(sb, options.ScriptNonce);
        return sb.ToString();
    }

    private static HtmlChartInteractionOptions CreateChildOptions(HtmlInteractiveDashboardOptions options, string scope, string groupName, int index) {
        var chartId = scope + "-" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var childOptions = new HtmlChartInteractionOptions {
            IdScope = chartId,
            IncludeResetButton = options.IncludeResetButton
        };
        childOptions.Interaction.ChartId = chartId;
        childOptions.Interaction.GroupName = groupName;
        childOptions.Interaction.Features = options.Interaction.Features;
        return childOptions;
    }
}

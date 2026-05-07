using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Html;

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

        var writer = HtmlInteractivePage.StartDocument(title);
        writer.StartElement("main").Attribute("class", "cfx-shell").EndStartElement().Line()
            .StartElement("div").Attribute("class", "cfx-dashboard").Attribute("style", "--cfx-dashboard-columns:" + options.Columns.ToString(System.Globalization.CultureInfo.InvariantCulture)).EndStartElement().Line();
        for (var i = 0; i < chartArray.Length; i++) {
            var childOptions = CreateChildOptions(options, scope, groupName, i);
            writer.RawTrusted(HtmlInteractiveChartRenderer.BuildChartSection(chartArray[i], childOptions, title)).Line();
        }

        writer.EndElement().Line()
            .EndElement().Line();
        HtmlInteractivePage.EndDocument(writer, options.ScriptNonce);
        return writer.Build();
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

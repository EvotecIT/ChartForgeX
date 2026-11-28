using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static readonly ChartSeriesKind[] DrilldownKinds = {
        ChartSeriesKind.Bar, ChartSeriesKind.HorizontalBar, ChartSeriesKind.Line, ChartSeriesKind.StepLine, ChartSeriesKind.Area, ChartSeriesKind.StepArea
    };

    internal sealed class DrilldownMarkup {
        internal string Attribute { get; set; } = string.Empty;
        internal string Templates { get; set; } = string.Empty;
        internal List<string> AutoThemeStyleSheets { get; } = new();
    }

    // Embedded children are prepared once, beside the wide drawing; an adaptive narrow drawing names the same targets.
    private static DrilldownMarkup? BuildDrilldown(Chart chart, HtmlChartInteractionOptions options, string identityScope, string chartId, string titleFallback, bool narrow) {
        var drilldown = options.Drilldown;
        if (!drilldown.Enabled) return null;
        if (chart.Series.Count == 0 || chart.Series.Any(series => Array.IndexOf(DrilldownKinds, series.Kind) < 0))
            throw new NotSupportedException("Drill-down targets need a bar, column, line or area chart.");
        var labels = chart.Options.XAxis.Labels;
        var json = new StringBuilder("{\"label\":").Append(HtmlJsonString.Encode(drilldown.RootLabel ?? ChartTitle(chart, titleFallback)))
            .Append(",\"text\":").Append(options.Localization.ToDrilldownJson()).Append(",\"targets\":[");
        var markup = new DrilldownMarkup();
        var templates = new HtmlMarkupWriter();
        for (var index = 0; index < drilldown.Targets.Count; index++) {
            var target = drilldown.Targets[index];
            var at = labels.FindIndex(item => string.Equals(item.Text, target.Category, StringComparison.Ordinal));
            if (at < 0) throw new ArgumentException("Drill-down category '" + target.Category + "' is not one of the chart's category labels.", nameof(options));
            var label = labels[at];
            if (index > 0) json.Append(',');
            json.Append("{\"id\":").Append(HtmlJsonString.Encode(target.Id))
                .Append(",\"category\":").Append(HtmlJsonString.Encode(target.Category))
                .Append(",\"x\":").Append(label.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"label\":").Append(HtmlJsonString.Encode(target.Label))
                .Append(",\"embedded\":").Append(target.OnDemand ? "false" : "true").Append('}');
            if (narrow || target.Child == null) continue;
            var childOptions = DrilldownChildOptions(options, target, identityScope, chartId);
            var child = PrepareChartSection(target.Child, childOptions, target.Label, "host", null);
            if (child.AutoThemeStyleSheet != null) markup.AutoThemeStyleSheets.Add(child.AutoThemeStyleSheet);
            templates.StartElement("template").Attribute("data-cfx-drill-child", target.Id).EndStartElement()
                .RawTrusted(child.Html).EndElement().Line();
        }
        markup.Attribute = json.Append("]}").ToString();
        markup.Templates = templates.Build();
        return markup;
    }

    // A child starts from the parent's presentation so the drilled view reads as the same report.
    private static HtmlChartInteractionOptions DrilldownChildOptions(HtmlChartInteractionOptions parent, HtmlChartDrilldownTarget target, string identityScope, string chartId) {
        var child = new HtmlChartInteractionOptions {
            Runtime = parent.Runtime, ResponsiveLayout = parent.ResponsiveLayout, Toolbar = parent.Toolbar,
            TextPresentation = parent.TextPresentation, IncludeResetButton = parent.IncludeResetButton, Nonce = parent.Nonce,
            IdScope = identityScope + "-" + target.Id
        };
        child.Interaction.Features = parent.Interaction.Features;
        child.Interaction.SelectionMode = parent.Interaction.SelectionMode;
        child.Interaction.ChartId = chartId + "-" + target.Id;
        parent.Localization.CopyTo(child.Localization);
        target.Configure?.Invoke(child);
        return child;
    }

    private static string? CombineAutoThemeStyleSheets(string? own, DrilldownMarkup? drilldown) {
        if (drilldown == null || drilldown.AutoThemeStyleSheets.Count == 0) return own;
        var sheets = new List<string>();
        if (own != null) sheets.Add(own);
        foreach (var sheet in drilldown.AutoThemeStyleSheets) if (!sheets.Contains(sheet)) sheets.Add(sheet);
        return string.Join("\n", sheets);
    }
}

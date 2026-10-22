using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Interactivity;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Prepares matching interactive markup and native exports for an existing Treemap or Sunburst root.</summary>
    /// <param name="chart">The unchanged source hierarchy chart.</param>
    /// <param name="view">An exact source root identity, or null for the complete hierarchy.</param>
    /// <param name="configure">Optional interaction configuration; enable HierarchyNavigation for managed navigation controls.</param>
    /// <returns>A detached section; the embedding host owns request transport and accepted basis tokens.</returns>
    public HtmlChartSection PrepareHierarchySection(Chart chart, ChartHierarchyView? view = null, Action<HtmlChartInteractionOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions(); configure?.Invoke(options);
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.PrepareHierarchy(context, view);
        PreparedVisual? narrow = null;
        if (options.ResponsiveLayout == HtmlChartResponsiveLayout.Adaptive) {
            ValidateAdaptiveOptions(options.Adaptive);
            narrow = chart.PrepareHierarchy(AdaptiveDrawingContext(context, options.Adaptive), view);
        }
        return PrepareChartSection(chart, options, ChartTitle(chart, "ChartForgeX interactive chart"), "host", null,
            preparedDrawing: prepared, narrowDrawing: narrow);
    }

    private static void AppendPreparedHierarchy(StringBuilder json, PreparedVisual prepared) {
        var view = prepared.HierarchyPreparation;
        if (view == null) return;
        json.Append(",\"hierarchy\":{\"kind\":").Append(HtmlJsonString.Encode(view.Kind.ToString().ToLowerInvariant()))
            .Append(",\"seriesKey\":").Append(HtmlJsonString.Encode(view.SeriesKey))
            .Append(",\"rootId\":").Append(view.Request == null ? "null" : HtmlJsonString.Encode(view.Request.RootId))
            .Append(",\"path\":[");
        for (var index = 0; index < view.Path.Count; index++) {
            if (index > 0) json.Append(',');
            json.Append("{\"id\":").Append(HtmlJsonString.Encode(view.Path[index].Id))
                .Append(",\"label\":").Append(HtmlJsonString.Encode(view.Path[index].Label)).Append('}');
        }
        json.Append("]}");
    }

    private static string BuildHierarchyNavigation(HtmlChartInteractionOptions options, PreparedVisual prepared) {
        if (!options.Interaction.HasFeature(ChartInteractionFeatures.HierarchyNavigation) || prepared.HierarchyPreparation == null) return string.Empty;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("nav").Attribute("class", "cfx-hierarchy-navigation").Attribute("data-cfx-hierarchy-navigation", "true")
            .Attribute("aria-label", options.Localization[HtmlChartText.HierarchyNavigation]).EndStartElement();
        foreach (var control in new[] { ("up", HtmlChartText.HierarchyUp), ("home", HtmlChartText.HierarchyHome) })
            writer.StartElement("button").Attribute("type", "button").Attribute("class", "cfx-tool")
                .Attribute("data-cfx-hierarchy-control", control.Item1).BooleanAttribute("disabled").EndStartElement()
                .Text(options.Localization[control.Item2]).EndElement();
        writer.StartElement("span").Attribute("class", "cfx-hierarchy-navigation__path").Attribute("data-cfx-hierarchy-path", "true")
            .EndStartElement().Text(prepared.HierarchyPreparation.Path.Count == 0 ? options.Localization[HtmlChartText.HierarchyHome]
                : string.Join(" / ", prepared.HierarchyPreparation.Path.Select(node => node.Label))).EndElement()
            .StartElement("span").Attribute("data-cfx-hierarchy-status", "true").Attribute("role", "status").Attribute("aria-live", "polite")
            .EndStartElement().EndElement().EndElement();
        return writer.Build();
    }
}

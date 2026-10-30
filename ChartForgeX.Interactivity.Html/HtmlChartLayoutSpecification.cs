using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

// The portable layout IR retains native paint and source semantics. It contains no sampled widths or coordinate derivatives.
internal static class HtmlChartLayoutSpecification {
    internal static string Serialize(Chart chart, int minimum, int maximum, int height, int printWidth, string? scope) {
        chart.Options.XAxis = chart.Options.XAxis.Clone(); chart.Options.XAxis.LabelDensity = ChartLabelDensity.All;
        chart.Options.YAxis = chart.Options.YAxis.Clone(); chart.Options.YAxis.LabelDensity = ChartLabelDensity.All;
        chart.WithSize(maximum, height);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var options = new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(scope == null ? null : scope + "|spec"), chart.Options.SvgColorVariables).WithPatternTiles();
        var svg = prepared.ToSvg(options);
        // A declared plot band stretches with the container while gutters before it stay fixed and gutters after it move
        // with the right edge. Drawings without a plot band stay centred; only their heading keeps its left inset.
        // Only state timelines stretch their schedule plot: Gantt marks (open ends, milestones, item captions) are not yet
        // safe to scale horizontally, so other schedules keep the centred projection.
        var stateLanes = chart.Series.Count > 0 && chart.Series.All(series => series.Kind == ChartSeriesKind.StateTimeline);
        var region = prepared.Regions.FirstOrDefault(item => item.Role is "cartesian-plot" or "matrix-plot" || stateLanes && item.Role == "schedule-plot");
        var plot = region?.Bounds ?? new ChartRect(24, 24, Math.Max(1, maximum - 48), Math.Max(1, height - 48));
        var output = new StringBuilder("{\"schema\":\"chartforgex/layout-1\",\"width\":").Append(maximum)
            .Append(",\"height\":").Append(height).Append(",\"minimum\":").Append(minimum).Append(",\"maximum\":").Append(maximum)
            .Append(",\"printWidth\":").Append(printWidth).Append(",\"plot\":[").Append(N(plot.X)).Append(',').Append(N(plot.Y)).Append(',').Append(N(plot.Width)).Append(',').Append(N(plot.Height)).Append(']')
            .Append(",\"cartesian\":").Append(region == null ? "false" : "true")
            .Append(stateLanes ? LaneGutter(prepared.Regions, plot) : string.Empty)
            .Append(",\"spacing\":").Append(N(chart.Options.Theme.Spacing))
            .Append(",\"legendPosition\":").Append(HtmlJsonString.Encode(chart.Options.LegendPosition.ToString()))
            .Append(",\"svg\":").Append(HtmlJsonString.Encode(svg));
        // Source values and labels travel with the serialized native scene, including typed observations and category/time formatting.
        HtmlChartLayoutBars.Append(output, chart, prepared);
        var root = XElement.Parse(svg);
        output.Append(",\"series\":["); var first = true;
        foreach (var series in root.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "series")) {
            if (!first) output.Append(','); first = false;
            output.Append("{\"kind\":").Append(HtmlJsonString.Encode((string?)series.Attribute("data-cfx-kind") ?? ""))
                .Append(",\"name\":").Append(HtmlJsonString.Encode((string?)series.Attribute("data-cfx-series-name") ?? "")).Append(",\"points\":[");
            var pointFirst = true;
            foreach (var point in series.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "point")) {
                var x = (string?)point.Attribute("data-cfx-x"); var y = (string?)point.Attribute("data-cfx-y");
                if (!double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var xv) || !double.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out var yv)) continue;
                if (!pointFirst) output.Append(','); pointFirst = false;
                output.Append("{\"x\":").Append(N(xv)).Append(",\"y\":").Append(N(yv)).Append(",\"label\":")
                    .Append(HtmlJsonString.Encode((string?)point.Attribute("data-cfx-display-label") ?? "")).Append('}');
            }
            output.Append("]}");
        }
        return output.Append("]}").ToString();
    }
    // Schedule lane labels sit in a left gutter sized for the widest drawing. The browser may narrow it down to this
    // minimum and truncate labels there, so a narrow container keeps a usable plot instead of a fixed wide gutter.
    private static string LaneGutter(System.Collections.Generic.IEnumerable<VisualSemanticRegion> regions, ChartRect plot) {
        var labels = regions.Where(item => item.Role == ChartDomRoles.ScheduleRowLabel).ToArray();
        if (labels.Length == 0) return string.Empty;
        var start = labels.Min(item => item.Bounds.Left);
        if (start >= plot.Left) return string.Empty;
        var minimum = Math.Min(plot.Left, start + MinimumLaneGutter);
        return ",\"gutter\":[" + N(start) + "," + N(minimum) + "]";
    }
    private const double MinimumLaneGutter = 88;
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}

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
        var region = prepared.Regions.FirstOrDefault(item => item.Role == "cartesian-plot");
        var plot = region?.Bounds ?? new ChartRect(24, 24, Math.Max(1, maximum - 48), Math.Max(1, height - 48));
        var output = new StringBuilder("{\"schema\":\"chartforgex/layout-1\",\"width\":").Append(maximum)
            .Append(",\"height\":").Append(height).Append(",\"minimum\":").Append(minimum).Append(",\"maximum\":").Append(maximum)
            .Append(",\"printWidth\":").Append(printWidth).Append(",\"plot\":[").Append(N(plot.X)).Append(',').Append(N(plot.Y)).Append(',').Append(N(plot.Width)).Append(',').Append(N(plot.Height)).Append(']')
            .Append(",\"cartesian\":").Append(region == null ? "false" : "true")
            .Append(",\"maximumBarWidth\":").Append(N(chart.Options.MaximumBarWidth ?? chart.Options.Theme.MaximumBarWidth))
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
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}

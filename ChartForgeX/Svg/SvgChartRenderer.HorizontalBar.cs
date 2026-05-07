using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHorizontalBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map, string id) {
        var s = chart.Series[index];
        var layout = HorizontalBarLayout(chart, plot, index);
        var zeroX = Math.Min(plot.Right, Math.Max(plot.Left, map.X(0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackHorizontalBaseValue(chart, index, p) : 0;
            var baseX = chart.Options.BarMode == ChartBarMode.Stacked ? map.X(baseValue) : zeroX;
            var valueX = map.X(baseValue + p.Y);
            var left = Math.Min(baseX, valueX);
            var width = Math.Abs(valueX - baseX);
            var y = map.Y(p.X) + layout.Offset - layout.BarHeight / 2;
            var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarHeight / 2) : Math.Min(7, layout.BarHeight / 2);
            WriteHorizontalBar(sb, index, pointIndex, p.X, p.Y, baseValue, left, y, width, layout.BarHeight, radius, BarFill(chart, s, index, pointIndex, id));
            DrawSvgBarHighlight(sb, left, y, width, layout.BarHeight);
            if (ShouldDrawDataLabels(chart, s)) {
                var label = FormatValue(chart, p.Y);
                var placement = DataLabelPlacement(chart, s);
                var inside = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center || (chart.Options.BarMode == ChartBarMode.Stacked && placement == ChartDataLabelPlacement.Auto);
                if (inside) {
                    if (width < EstimateTextWidth(label, chart.Options.Theme.DataLabelFontSize) + 8) continue;
                    if (!ReserveSvgLabel(label, left + width / 2, y + layout.BarHeight / 2, chart, plot, reservedLabels)) continue;
                    DrawDataLabel(sb, chart, label, left + width / 2, y + layout.BarHeight / 2, plot, series: s, pointIndex: pointIndex);
                } else if (placement == ChartDataLabelPlacement.Above || placement == ChartDataLabelPlacement.Below) {
                    var labelY = placement == ChartDataLabelPlacement.Above ? y - 8 : y + layout.BarHeight + 12;
                    if (!ReserveSvgLabel(label, left + width / 2, labelY, chart, plot, reservedLabels)) continue;
                    DrawDataLabel(sb, chart, label, left + width / 2, labelY, plot, series: s, pointIndex: pointIndex);
                } else {
                    var labelX = placement == ChartDataLabelPlacement.Right ? left + width + 8 : placement == ChartDataLabelPlacement.Left ? left - 8 : p.Y >= 0 ? left + width + 8 : left - 8;
                    var anchor = labelX >= left + width / 2 ? "start" : "end";
                    if (!ReserveSvgHorizontalLabel(label, labelX, y + layout.BarHeight / 2, anchor, chart, plot, reservedLabels)) continue;
                    DrawHorizontalValueLabel(sb, chart, label, labelX, y + layout.BarHeight / 2, anchor, plot, s, pointIndex);
                }
            }
        }
    }

    private static void WriteHorizontalBar(StringBuilder sb, int seriesIndex, int pointIndex, double category, double value, double baseValue, double x, double y, double width, double height, double radius, string fill) {
        var writer = new SvgMarkupWriter(512);
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "horizontal-bar")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-category", category)
            .Attribute("data-cfx-value", value)
            .Attribute("data-cfx-base", baseValue)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", radius)
            .Attribute("fill", fill)
            .Attribute("opacity", ChartVisualPrimitives.BarFillOpacity)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static HorizontalBarLayoutInfo HorizontalBarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var horizontalSeries = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.HorizontalBar)
            .Select(item => item.index)
            .ToArray();
        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, horizontalSeries.Length);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, Array.IndexOf(horizontalSeries, seriesIndex));
        var categoryValues = new HashSet<double>();
        foreach (var index in horizontalSeries) {
            foreach (var point in chart.Series[index].Points) categoryValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, categoryValues.Count);
        var slotHeight = plot.Height / categoryCount;
        var groupHeight = slotHeight * (groupCount == 1 ? 0.56 : 0.76);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupHeight * 0.08);
        var barHeight = Math.Max(3, Math.Min(30, (groupHeight - gap * (groupCount - 1)) / groupCount));
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barHeight + gap);
        return new HorizontalBarLayoutInfo(barHeight, offset);
    }

    private static double StackHorizontalBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static void DrawHorizontalStackTotals(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        var reservedLabels = new List<ChartLabelBounds>();
        DrawHorizontalStackTotalSet(sb, chart, positiveTotals, plot, map, 8, "start", reservedLabels);
        DrawHorizontalStackTotalSet(sb, chart, negativeTotals, plot, map, -8, "end", reservedLabels);
    }

    private static void DrawHorizontalStackTotalSet(StringBuilder sb, Chart chart, Dictionary<double, double> totals, ChartRect plot, ChartMapper map, double offset, string anchor, List<ChartLabelBounds> reservedLabels) {
        foreach (var item in totals.OrderBy(item => item.Key)) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var x = map.X(item.Value) + offset;
            var y = map.Y(item.Key);
            if (!ReserveSvgHorizontalLabel(label, x, y, anchor, chart, plot, reservedLabels)) continue;
            DrawHorizontalValueLabel(sb, chart, label, x, y, anchor, plot);
        }
    }
}

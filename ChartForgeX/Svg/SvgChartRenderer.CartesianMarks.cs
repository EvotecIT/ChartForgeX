using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawTrendLine(StringBuilder sb, Chart chart, int index, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = Color(chart, index);
        var start = series.Points[0];
        var end = series.Points[series.Points.Count - 1];
        var x1 = map.X(start.X);
        var y1 = map.Y(start.Y);
        var x2 = map.X(end.X);
        var y2 = map.Y(end.Y);
        var slope = (end.Y - start.Y) / (end.X - start.X);
        var intercept = start.Y - slope * start.X;
        AppendSvg(sb, writer => writer
            .StartElement("line")
            .Attribute("data-cfx-role", "trend-line")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-slope", slope)
            .Attribute("data-cfx-intercept", intercept)
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", Math.Max(ChartVisualPrimitives.TrendLineMinStrokeWidth, series.StrokeWidth))
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-dasharray", "8 6")
            .Attribute("opacity", "0.92")
            .EndEmptyElement()
            .Line());
    }

    private static void DrawSlope(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = Color(chart, index);
        var startColor = PointColor(chart, series, index, 0);
        var endColor = PointColor(chart, series, index, 1);
        var start = series.Points[0];
        var end = series.Points[1];
        var xStart = map.X(start.X);
        var yStart = map.Y(start.Y);
        var xEnd = map.X(end.X);
        var yEnd = map.Y(end.Y);
        var radius = Math.Max(ChartVisualPrimitives.SlopeMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.SlopeMarkerRadiusExtra);
        var summary = series.Name + ": " + FormatValue(chart, start.Y) + " to " + FormatValue(chart, end.Y);

        AppendSvg(sb, writer => writer
            .StartElement("g")
            .Attribute("data-cfx-role", "slope")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-start", start.Y)
            .Attribute("data-cfx-end", end.Y)
            .Attribute("data-cfx-delta", end.Y - start.Y)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndStartElement()
            .StartElement("line")
            .Attribute("data-cfx-role", "slope-line")
            .Attribute("data-cfx-series", index)
            .Attribute("x1", xStart)
            .Attribute("y1", yStart)
            .Attribute("x2", xEnd)
            .Attribute("y2", yEnd)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", series.StrokeWidth + ChartVisualPrimitives.LineHaloStrokeExtra)
            .Attribute("stroke-linecap", "round")
            .Attribute("opacity", ChartVisualPrimitives.StrokeHaloOpacity)
            .EndEmptyElement()
            .StartElement("line")
            .Attribute("data-cfx-role", "slope-line")
            .Attribute("data-cfx-series", index)
            .Attribute("x1", xStart)
            .Attribute("y1", yStart)
            .Attribute("x2", xEnd)
            .Attribute("y2", yEnd)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", series.StrokeWidth)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .StartElement("circle")
            .Attribute("data-cfx-role", "slope-start")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-value", start.Y)
            .Attribute("cx", xStart)
            .Attribute("cy", yStart)
            .Attribute("r", radius)
            .Attribute("fill", startColor.ToCss())
            .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth)
            .EndEmptyElement()
            .StartElement("circle")
            .Attribute("data-cfx-role", "slope-end")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-value", end.Y)
            .Attribute("cx", xEnd)
            .Attribute("cy", yEnd)
            .Attribute("r", radius)
            .Attribute("fill", endColor.ToCss())
            .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth)
            .EndEmptyElement()
            .EndElement()
            .Line());
        if (!ShouldDrawDataLabels(chart, series)) return;
        DrawHorizontalValueLabel(sb, chart, FormatValue(chart, start.Y), xStart - radius - 8, yStart, "end", plot, series, 0);
        DrawHorizontalValueLabel(sb, chart, FormatValue(chart, end.Y), xEnd + radius + 8, yEnd, "start", plot, series, 1);
    }

    private static void DrawStackedArea(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var upper = new List<ChartPoint>(series.Points.Count);
        var lower = new List<ChartPoint>(series.Points.Count);
        foreach (var point in series.Points) {
            var baseValue = StackAreaBaseValue(chart, index, point);
            upper.Add(new ChartPoint(map.X(point.X), map.Y(baseValue + point.Y)));
            lower.Add(new ChartPoint(map.X(point.X), map.Y(baseValue)));
        }

        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        AppendSvg(sb, writer => writer
            .StartElement("path")
            .Attribute("data-cfx-role", "stacked-area")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-point-count", series.Points.Count)
            .Attribute("d", BuildClosedPolygonPath(upperPath, lowerPath))
            .Attribute("fill", color.ToCss())
            .Attribute("opacity", "0.42")
            .EndEmptyElement()
            .Line());
        var line = BuildLinePath(upperPath, false);
        AppendSvg(sb, writer => writer
            .StartElement("path")
            .Attribute("data-cfx-role", "stacked-area-line")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-point-count", series.Points.Count)
            .Attribute("d", line)
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", series.StrokeWidth + 4)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("opacity", "0.12")
            .EndEmptyElement()
            .Line());
        AppendSvg(sb, writer => writer
            .StartElement("path")
            .Attribute("data-cfx-role", "stacked-area-line")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-point-count", series.Points.Count)
            .Attribute("d", line)
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", series.StrokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .EndEmptyElement()
            .Line());
        if (!ShouldDrawDataLabels(chart, series)) return;
        var labelPoints = series.Points.Select(point => new ChartPoint(map.X(point.X), map.Y(StackAreaBaseValue(chart, index, point) + point.Y))).ToArray();
        DrawPointLabels(sb, chart, series, labelPoints, plot);
    }

    private static string BuildClosedPolygonPath(IReadOnlyList<ChartPoint> upper, IReadOnlyList<ChartPoint> lower) {
        if (upper.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.Append("M ").Append(F(upper[0].X)).Append(' ').Append(F(upper[0].Y));
        for (var i = 1; i < upper.Count; i++) sb.Append(" L ").Append(F(upper[i].X)).Append(' ').Append(F(upper[i].Y));
        for (var i = lower.Count - 1; i >= 0; i--) sb.Append(" L ").Append(F(lower[i].X)).Append(' ').Append(F(lower[i].Y));
        sb.Append(" Z");
        return sb.ToString();
    }

    private static void DrawLollipops(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var s = chart.Series[index];
        var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
        var radius = Math.Max(4, chart.Options.Theme.MarkerRadius + 2.25);
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var x = map.X(p.X);
            var y = map.Y(p.Y);
            var c = PointColor(chart, s, index, pointIndex);
            AppendSvg(sb, writer => writer
                .StartElement("line")
                .Attribute("data-cfx-role", "lollipop-stem")
                .Attribute("data-cfx-series", index)
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("data-cfx-x", p.X)
                .Attribute("data-cfx-y", p.Y)
                .Attribute("x1", x)
                .Attribute("y1", zeroY)
                .Attribute("x2", x)
                .Attribute("y2", y)
                .Attribute("stroke", c.ToCss())
                .Attribute("stroke-width", Math.Max(ChartVisualPrimitives.LollipopStemMinStrokeWidth, s.StrokeWidth * 0.62))
                .Attribute("stroke-linecap", "round")
                .Attribute("opacity", ChartVisualPrimitives.LollipopStemOpacity)
                .EndEmptyElement()
                .Line());
            AppendSvg(sb, writer => writer
                .StartElement("circle")
                .Attribute("data-cfx-role", "lollipop-marker")
                .Attribute("data-cfx-series", index)
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("data-cfx-x", p.X)
                .Attribute("data-cfx-y", p.Y)
                .Attribute("cx", x)
                .Attribute("cy", y)
                .Attribute("r", radius)
                .Attribute("fill", c.ToCss())
                .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.LollipopMarkerStrokeWidth)
                .EndEmptyElement()
                .Line());
        }

        if (ShouldDrawDataLabels(chart, s)) DrawPointLabels(sb, chart, s, s.Points.Select(p => new ChartPoint(map.X(p.X), map.Y(p.Y))).ToArray(), plot);
    }

    private static void DrawBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartRange range, ChartMapper map, string id) {
        var s = chart.Series[index];
        var layout = BarLayout(chart, plot, index);
        var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackBaseValue(chart, index, p) : 0;
            var y = map.Y(baseValue + p.Y);
            var baseY = chart.Options.BarMode == ChartBarMode.Stacked ? map.Y(baseValue) : zeroY;
            var top = Math.Min(y, baseY);
            var height = Math.Abs(baseY - y);
            var x = map.X(p.X) + layout.Offset - layout.BarWidth / 2;
            var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarWidth / 2) : Math.Min(7, layout.BarWidth / 2);
            AppendSvg(sb, writer => writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "bar")
                .Attribute("data-cfx-series", index)
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("data-cfx-x", p.X)
                .Attribute("data-cfx-y", p.Y)
                .Attribute("data-cfx-base", baseValue)
                .Attribute("x", x)
                .Attribute("y", top)
                .Attribute("width", layout.BarWidth)
                .Attribute("height", height)
                .Attribute("rx", radius)
                .Attribute("fill", BarFill(chart, s, index, pointIndex, id))
                .Attribute("opacity", ChartVisualPrimitives.BarFillOpacity)
                .EndEmptyElement()
                .Line());
            DrawSvgFillPatternOverlay(sb, s, index, pointIndex, id, x, top, layout.BarWidth, height, radius, "bar-pattern");
            DrawSvgBarHighlight(sb, x, top, layout.BarWidth, height);
            if (ShouldDrawDataLabels(chart, s)) {
                var label = FormatDataLabel(chart, s, pointIndex, p.Y);
                var placement = DataLabelPlacement(chart, s);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var labelX = placement == ChartDataLabelPlacement.Right ? x + layout.BarWidth + 8 : x - 8;
                    var anchor = placement == ChartDataLabelPlacement.Right ? "start" : "end";
                    if (!ReserveSvgHorizontalLabel(label, labelX, top + height / 2, anchor, chart, plot, reservedLabels)) continue;
                    DrawHorizontalValueLabel(sb, chart, label, labelX, top + height / 2, anchor, plot, s, pointIndex);
                    continue;
                }

                var inside = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center || (chart.Options.BarMode == ChartBarMode.Stacked && placement == ChartDataLabelPlacement.Auto);
                if (inside && height < chart.Options.Theme.DataLabelFontSize + 8) continue;
                var labelY = placement == ChartDataLabelPlacement.Above
                    ? top - 10
                    : placement == ChartDataLabelPlacement.Below
                        ? top + height + 10
                        : inside
                            ? top + height / 2
                            : p.Y >= 0 ? top - 10 : top + height + 10;
                if (!ReserveSvgLabel(label, x + layout.BarWidth / 2, labelY, chart, plot, reservedLabels)) continue;
                DrawDataLabel(sb, chart, label, x + layout.BarWidth / 2, labelY, plot, series: s, pointIndex: pointIndex);
            }
        }
    }

    private static void DrawSvgBarHighlight(StringBuilder sb, double x, double y, double width, double height) {
        if (width <= ChartVisualPrimitives.BarHighlightInset * 2 + 3 || height <= ChartVisualPrimitives.BarHighlightInset * 2 + 1) return;
        var inset = ChartVisualPrimitives.BarHighlightInset;
        AppendSvg(sb, writer => writer
            .StartElement("line")
            .Attribute("data-cfx-role", "bar-highlight")
            .Attribute("x1", x + inset)
            .Attribute("y1", y + inset)
            .Attribute("x2", x + width - inset)
            .Attribute("y2", y + inset)
            .Attribute("stroke", "#fff")
            .Attribute("stroke-opacity", ChartVisualPrimitives.BarHighlightOpacity)
            .Attribute("stroke-width", ChartVisualPrimitives.BarHighlightStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .Line());
    }

    private static BarLayoutInfo BarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var barSeries = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.Bar)
            .Select(item => item.index)
            .ToArray();
        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, barSeries.Length);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, Array.IndexOf(barSeries, seriesIndex));
        var xValues = new HashSet<double>();
        foreach (var index in barSeries) {
            foreach (var point in chart.Series[index].Points) xValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, xValues.Count);
        var slotWidth = plot.Width / categoryCount;
        var groupWidth = slotWidth * (groupCount == 1 ? 0.58 : 0.74);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupWidth * 0.08);
        var barWidth = Math.Max(3, (groupWidth - gap * (groupCount - 1)) / groupCount);
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barWidth + gap);
        return new BarLayoutInfo(barWidth, offset);
    }

    private static double StackBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static double StackAreaBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.StackedArea) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static void DrawStackTotals(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        var reservedLabels = new List<ChartLabelBounds>();
        DrawStackTotalSet(sb, chart, positiveTotals, plot, map, -14, reservedLabels);
        DrawStackTotalSet(sb, chart, negativeTotals, plot, map, 14, reservedLabels);
    }

    private static void DrawStackTotalSet(StringBuilder sb, Chart chart, Dictionary<double, double> totals, ChartRect plot, ChartMapper map, double offset, List<ChartLabelBounds> reservedLabels) {
        foreach (var item in totals.OrderBy(item => item.Key)) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var x = map.X(item.Key);
            var y = map.Y(item.Value) + offset;
            if (!ReserveSvgLabel(label, x, y, chart, plot, reservedLabels)) continue;
            DrawDataLabel(sb, chart, label, x, y, plot, "stack-total-label");
        }
    }

    private static void AddStackTotal(Dictionary<double, double> totals, double x, double y) {
        double current;
        totals.TryGetValue(x, out current);
        totals[x] = current + y;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRangeArea(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map, string id) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var lower = new List<ChartPoint>();
        var upper = new List<ChartPoint>();
        var middle = new List<ChartPoint>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            lower.Add(new ChartPoint(map.X(low.X), map.Y(low.Y)));
            upper.Add(new ChartPoint(map.X(high.X), map.Y(high.Y)));
            middle.Add(new ChartPoint(map.X(low.X), map.Y((low.Y + high.Y) / 2.0)));
        }

        if (lower.Count == 0) return;
        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var middlePath = ChartPathBuilder.FromPoints(middle, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var upperLine = BuildLinePath(upperPath, false);
        var lowerLine = BuildLinePath(lowerPath, false);
        var middleLine = BuildLinePath(middlePath, false);
        var summary = series.Name + " interval area with " + lower.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " point(s)";

        var writer = new SvgMarkupWriter(2048);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "range-area-series")
            .Attribute("data-cfx-series", index)
            .Attribute("data-cfx-interval-count", lower.Count)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndStartElement()
            .Line();
        WriteRangeAreaPath(writer, "range-area", index, lower.Count, BuildClosedPolygonPath(upperPath, lowerPath), $"url(#{id}-area{index})", null, null, null);
        WriteRangeAreaPath(writer, "range-area-midline", index, middle.Count, middleLine, "none", color.ToCss(), ChartVisualPrimitives.RangeAreaMidlineStrokeWidth, ChartVisualPrimitives.RangeAreaMidlineOpacity, true);
        WriteRangeAreaPath(writer, "range-area-upper-halo", index, upper.Count, upperLine, "none", color.ToCss(), series.StrokeWidth + ChartVisualPrimitives.RangeAreaHaloStrokeExtra, ChartVisualPrimitives.StrokeHaloOpacity);
        WriteRangeAreaPath(writer, "range-area-lower-halo", index, lower.Count, lowerLine, "none", color.ToCss(), series.StrokeWidth + ChartVisualPrimitives.RangeAreaHaloStrokeExtra, ChartVisualPrimitives.StrokeHaloOpacity);
        WriteRangeAreaPath(writer, "range-area-upper", index, upper.Count, upperLine, "none", color.ToCss(), Math.Max(ChartVisualPrimitives.RangeAreaMinStrokeWidth, series.StrokeWidth), null);
        WriteRangeAreaPath(writer, "range-area-lower", index, lower.Count, lowerLine, "none", color.ToCss(), Math.Max(ChartVisualPrimitives.RangeAreaMinStrokeWidth, series.StrokeWidth), ChartVisualPrimitives.RangeAreaLowerStrokeOpacity);
        writer.EndElement().Line();
        sb.Append(writer.Build());

        if (!ShouldDrawDataLabels(chart, series)) return;
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var item = pointIndex / 2;
            var x = map.X(low.X);
            var yLow = map.Y(low.Y);
            var yHigh = map.Y(high.Y);
            var label = FormatValue(chart, low.Y) + "-" + FormatValue(chart, high.Y);
            DrawRangeIntervalLabel(sb, chart, series, item, plot, reservedLabels, label, x, yLow, yHigh);
        }
    }

    private static void WriteRangeAreaPath(SvgMarkupWriter writer, string role, int seriesIndex, int intervalCount, string path, string fill, string? stroke, double? strokeWidth, double? opacity, bool dashed = false) {
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-interval-count", intervalCount)
            .Attribute("d", path)
            .Attribute("fill", fill);
        if (stroke != null) {
            writer
                .Attribute("stroke", stroke)
                .Attribute("stroke-width", strokeWidth.GetValueOrDefault())
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round");
            if (dashed) {
                writer.Attribute("stroke-dasharray", SvgMarkupWriter.FormatNumber(ChartVisualPrimitives.RangeAreaDash) + " " + SvgMarkupWriter.FormatNumber(ChartVisualPrimitives.RangeAreaGap));
            }
        }

        writer
            .OptionalAttribute("opacity", opacity)
            .EndEmptyElement()
            .Line();
    }
}

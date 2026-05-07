using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRangeBand(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var lower = new List<ChartPoint>();
        var upper = new List<ChartPoint>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            lower.Add(new ChartPoint(map.X(low.X), map.Y(low.Y)));
            upper.Add(new ChartPoint(map.X(high.X), map.Y(high.Y)));
        }

        if (lower.Count == 0) return;
        var path = BuildRangeBandPath(lower, upper);
        var writer = new SvgMarkupWriter(1024);
        WriteRangeBandArea(writer, index, lower.Count, path, color.ToCss());
        WriteRangeBandBoundary(writer, "range-band-upper", index, upper.Count, BuildLinePath(upper, false), color.ToCss());
        WriteRangeBandBoundary(writer, "range-band-lower", index, lower.Count, BuildLinePath(lower, false), color.ToCss());
        sb.Append(writer.Build());
        if (ShouldDrawDataLabels(chart, series)) {
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
    }

    private static void WriteRangeBandArea(SvgMarkupWriter writer, int seriesIndex, int intervalCount, string path, string color) {
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "range-band")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-interval-count", intervalCount)
            .Attribute("d", path)
            .Attribute("fill", color)
            .Attribute("opacity", ChartVisualPrimitives.RangeBandFillOpacity)
            .EndEmptyElement()
            .Line();
    }

    private static void WriteRangeBandBoundary(SvgMarkupWriter writer, string role, int seriesIndex, int intervalCount, string path, string color) {
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-interval-count", intervalCount)
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", color)
            .Attribute("stroke-width", ChartVisualPrimitives.RangeBandBoundaryStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("opacity", ChartVisualPrimitives.RangeBandBoundaryOpacity)
            .EndEmptyElement()
            .Line();
    }

    private static void DrawRangeIntervalLabel(StringBuilder sb, Chart chart, ChartSeries series, int pointIndex, ChartRect plot, List<ChartLabelBounds> reservedLabels, string label, double x, double yLow, double yHigh) {
        var placement = DataLabelPlacement(chart, series);
        var top = Math.Min(yLow, yHigh);
        var bottom = Math.Max(yLow, yHigh);
        if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
            var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
            var labelX = placement == ChartDataLabelPlacement.Left ? x - 8 : x + 8;
            if (ReserveSvgHorizontalLabel(label, labelX, (top + bottom) / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, (top + bottom) / 2, anchor, plot, series, pointIndex);
            return;
        }

        var labelY = placement == ChartDataLabelPlacement.Below
            ? bottom + 10
            : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                ? (top + bottom) / 2
                : top - 10;
        if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: pointIndex);
    }

    private static string BuildRangeBandPath(IReadOnlyList<ChartPoint> lower, IReadOnlyList<ChartPoint> upper) {
        var path = BuildLinePath(upper, false);
        for (var i = lower.Count - 1; i >= 0; i--) path += " L " + F(lower[i].X) + " " + F(lower[i].Y);
        return path + " Z";
    }
}

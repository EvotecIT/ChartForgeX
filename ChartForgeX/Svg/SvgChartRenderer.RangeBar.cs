using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRangeBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map, string id) {
        var series = chart.Series[index];
        var intervalCount = Math.Max(1, series.Points.Count / 2);
        var barWidth = Math.Max(8, Math.Min(28, plot.Width / Math.Max(1, intervalCount * 4.0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var start = series.Points[pointIndex];
            var end = series.Points[pointIndex + 1];
            var x = map.X(start.X);
            var y1 = map.Y(start.Y);
            var y2 = map.Y(end.Y);
            var top = Math.Min(y1, y2);
            var height = Math.Max(2, Math.Abs(y2 - y1));
            var intervalIndex = pointIndex / 2;
            var color = PointColor(chart, series, index, intervalIndex);
            var summary = FormatValue(chart, Math.Min(start.Y, end.Y)) + "-" + FormatValue(chart, Math.Max(start.Y, end.Y));
            var label = FormatRangeBarLabel(chart, series, intervalIndex, start.Y, end.Y);

            WriteRangeBarInterval(sb, series, index, intervalIndex, id, start.X, start.Y, end.Y, summary, color, x, y1, y2, top, height, barWidth);
            if (ShouldDrawDataLabels(chart, series)) DrawRangeBarLabel(sb, chart, series, intervalIndex, plot, reservedLabels, label, x, y1, y2, top, height, barWidth);
        }
    }

    private static string FormatRangeBarLabel(Chart chart, ChartSeries series, int intervalIndex, double startValue, double endValue) {
        if (intervalIndex >= 0 && intervalIndex < series.PointLabels.Count && series.PointLabels[intervalIndex] != null) return series.PointLabels[intervalIndex]!;
        return FormatValue(chart, Math.Min(startValue, endValue)) + "-" + FormatValue(chart, Math.Max(startValue, endValue));
    }

    private static void WriteRangeBarInterval(
        StringBuilder sb,
        ChartSeries series,
        int seriesIndex,
        int pointIndex,
        string id,
        double valueX,
        double startValue,
        double endValue,
        string summary,
        ChartColor color,
        double x,
        double y1,
        double y2,
        double top,
        double height,
        double barWidth) {
        var colorCss = color.ToCss();
        var writer = new SvgMarkupWriter(768);
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "range-bar")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-x", valueX)
            .Attribute("data-cfx-start", startValue)
            .Attribute("data-cfx-end", endValue)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("x", x - barWidth / 2)
            .Attribute("y", top)
            .Attribute("width", barWidth)
            .Attribute("height", height)
            .Attribute("rx", Math.Min(7, barWidth / 2))
            .Attribute("fill", colorCss)
            .Attribute("opacity", ChartVisualPrimitives.RangeBarFillOpacity)
            .EndEmptyElement()
            .Line();
        WriteFillPatternOverlay(writer, series, seriesIndex, pointIndex, id, x - barWidth / 2, top, barWidth, height, Math.Min(7, barWidth / 2), "range-bar-pattern");
        WriteRangeBarCap(writer, seriesIndex, pointIndex, "start", startValue, x - barWidth * 0.75, y1, x + barWidth * 0.75, y1, colorCss);
        WriteRangeBarCap(writer, seriesIndex, pointIndex, "end", endValue, x - barWidth * 0.75, y2, x + barWidth * 0.75, y2, colorCss);
        sb.Append(writer.Build());
    }

    private static void WriteRangeBarCap(SvgMarkupWriter writer, int seriesIndex, int pointIndex, string bound, double value, double x1, double y1, double x2, double y2, string color) {
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "range-bar-cap")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-bound", bound)
            .Attribute("data-cfx-value", value)
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color)
            .Attribute("stroke-width", ChartVisualPrimitives.RangeBarCapStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .Line();
    }

    private static void DrawRangeBarLabel(StringBuilder sb, Chart chart, ChartSeries series, int pointIndex, ChartRect plot, List<ChartLabelBounds> reservedLabels, string label, double x, double y1, double y2, double top, double height, double barWidth) {
        var placement = DataLabelPlacement(chart, series);
        if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
            var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
            var labelX = placement == ChartDataLabelPlacement.Left ? x - barWidth * 0.9 - 6 : x + barWidth * 0.9 + 6;
            if (ReserveSvgHorizontalLabel(label, labelX, top + height / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, top + height / 2, anchor, plot, series, pointIndex);
            return;
        }

        var bottom = Math.Max(y1, y2);
        var labelY = placement == ChartDataLabelPlacement.Below
            ? bottom + 10
            : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                ? top + height / 2
                : top - 10;
        if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: pointIndex);
    }
}

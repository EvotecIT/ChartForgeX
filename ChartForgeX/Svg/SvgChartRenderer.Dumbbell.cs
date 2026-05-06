using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawDumbbells(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var startColor = chart.Options.Theme.MutedText;
        var radius = Math.Max(ChartVisualPrimitives.DumbbellMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.DumbbellMarkerRadiusExtra);
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var start = series.Points[pointIndex];
            var end = series.Points[pointIndex + 1];
            var x = map.X(start.X);
            var yStart = map.Y(start.Y);
            var yEnd = map.Y(end.Y);
            var item = pointIndex / 2;
            var color = PointColor(chart, series, index, item);
            var label = FormatValue(chart, start.Y) + "-" + FormatValue(chart, end.Y);

            WriteDumbbellSummary(sb, chart, index, item, start.X, start.Y, end.Y, label, color, startColor, x, yStart, yEnd, radius);
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                var top = Math.Min(yStart, yEnd);
                var bottom = Math.Max(yStart, yEnd);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                    var labelX = placement == ChartDataLabelPlacement.Left ? x - radius - 8 : x + radius + 8;
                    if (ReserveSvgHorizontalLabel(label, labelX, (top + bottom) / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, (top + bottom) / 2, anchor, plot, series, item);
                } else {
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? bottom + radius + 8
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? (top + bottom) / 2
                            : top - radius - 8;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }

    private static void WriteDumbbellSummary(
        StringBuilder sb,
        Chart chart,
        int seriesIndex,
        int pointIndex,
        double valueX,
        double startValue,
        double endValue,
        string label,
        ChartColor endColor,
        ChartColor startColor,
        double x,
        double yStart,
        double yEnd,
        double radius) {
        var endColorCss = endColor.ToCss();
        var writer = new SvgMarkupWriter(1024);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "dumbbell")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-x", valueX)
            .Attribute("data-cfx-start", startValue)
            .Attribute("data-cfx-end", endValue)
            .Attribute("data-cfx-delta", endValue - startValue)
            .Attribute("role", "img")
            .Attribute("aria-label", label)
            .EndStartElement()
            .Line()
            .StartElement("line")
            .Attribute("data-cfx-role", "dumbbell-connector")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("x1", x)
            .Attribute("y1", yStart)
            .Attribute("x2", x)
            .Attribute("y2", yEnd)
            .Attribute("stroke", endColorCss)
            .Attribute("stroke-width", ChartVisualPrimitives.DumbbellConnectorStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("opacity", ChartVisualPrimitives.DumbbellConnectorOpacity)
            .EndEmptyElement()
            .Line();
        WriteDumbbellMarker(writer, "dumbbell-start", seriesIndex, pointIndex, startValue, x, yStart, radius, startColor.ToCss(), chart.Options.Theme.CardBackground.ToCss());
        WriteDumbbellMarker(writer, "dumbbell-end", seriesIndex, pointIndex, endValue, x, yEnd, radius, endColorCss, chart.Options.Theme.CardBackground.ToCss());
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void WriteDumbbellMarker(SvgMarkupWriter writer, string role, int seriesIndex, int pointIndex, double value, double x, double y, double radius, string fill, string stroke) {
        writer
            .StartElement("circle")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-value", value)
            .Attribute("cx", x)
            .Attribute("cy", y)
            .Attribute("r", radius)
            .Attribute("fill", fill)
            .Attribute("stroke", stroke)
            .Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth)
            .EndEmptyElement()
            .Line();
    }
}

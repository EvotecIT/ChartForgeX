using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawBubbles(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var range = BubbleSizeRange(series);
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var center = series.Points[pointIndex];
            var size = series.Points[pointIndex + 1].Y;
            var x = map.X(center.X);
            var y = map.Y(center.Y);
            var radius = BubbleRadius(size, range.min, range.max, plot);
            var item = pointIndex / 2;
            var color = PointColor(chart, series, index, item);
            var summary = "x " + FormatValue(chart, center.X) + ", y " + FormatValue(chart, center.Y) + ", size " + FormatValue(chart, size);

            WriteBubbleMarker(sb, chart, index, item, center.X, center.Y, size, x, y, radius, color, summary);
            var label = FormatValue(chart, size);
            if (ShouldDrawDataLabels(chart, series)) DrawBubbleLabel(sb, chart, series, item, plot, reservedLabels, label, x, y, radius);
        }
    }

    private static void WriteBubbleMarker(StringBuilder sb, Chart chart, int seriesIndex, int pointIndex, double valueX, double valueY, double size, double x, double y, double radius, ChartColor color, string summary) {
        var writer = new SvgMarkupWriter(512);
        writer
            .StartElement("circle")
            .Attribute("data-cfx-role", "bubble")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-x", valueX)
            .Attribute("data-cfx-y", valueY)
            .Attribute("data-cfx-size", size)
            .Attribute("cx", x)
            .Attribute("cy", y)
            .Attribute("r", radius)
            .Attribute("fill", color.ToCss())
            .Attribute("fill-opacity", ChartVisualPrimitives.BubbleFillOpacity)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.BubbleStrokeOpacity)
            .Attribute("stroke-width", ChartVisualPrimitives.BubbleStrokeWidth)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndEmptyElement()
            .Line()
            .StartElement("circle")
            .Attribute("data-cfx-role", "bubble-highlight")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-size", size)
            .Attribute("cx", x - radius * 0.28)
            .Attribute("cy", y - radius * 0.28)
            .Attribute("r", Math.Max(1.4, radius * 0.18))
            .Attribute("fill", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("opacity", ChartVisualPrimitives.BubbleHighlightOpacity)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void DrawBubbleLabel(StringBuilder sb, Chart chart, ChartSeries series, int pointIndex, ChartRect plot, List<ChartLabelBounds> reservedLabels, string label, double x, double y, double radius) {
        var placement = DataLabelPlacement(chart, series);
        if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
            var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
            var labelX = placement == ChartDataLabelPlacement.Left ? x - radius - 8 : x + radius + 8;
            if (ReserveSvgHorizontalLabel(label, labelX, y, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, y, anchor, plot, series, pointIndex);
            return;
        }

        var aboveY = y - radius - 12;
        var belowY = y + radius + 12;
        var labelY = placement == ChartDataLabelPlacement.Below
            ? belowY
            : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                ? y
                : aboveY < plot.Top + 2 ? belowY : aboveY;
        if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: pointIndex);
    }

    private static (double min, double max) BubbleSizeRange(ChartSeries series) {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        for (var i = 1; i < series.Points.Count; i += 2) {
            var size = series.Points[i].Y;
            if (size < min) min = size;
            if (size > max) max = size;
        }

        if (double.IsInfinity(min)) return (1, 1);
        return (min, max);
    }

    private static double BubbleRadius(double size, double min, double max, ChartRect plot) {
        var minimumRadius = 6.0;
        var maximumRadius = Math.Min(32, Math.Max(14, Math.Min(plot.Width, plot.Height) * 0.075));
        if (Math.Abs(max - min) < double.Epsilon) return (minimumRadius + maximumRadius) / 2;
        var normalizedArea = (size - min) / (max - min);
        return minimumRadius + Math.Sqrt(Math.Max(0, Math.Min(1, normalizedArea))) * (maximumRadius - minimumRadius);
    }
}

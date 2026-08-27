using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawBubbles(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var range = BubbleSizeRange(series);
        var reserved = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var center = series.Points[pointIndex];
            var size = series.Points[pointIndex + 1].Y;
            var x = map.X(center.X);
            var y = map.Y(center.Y);
            var radius = BubbleRadius(size, range.min, range.max, plot);
            var item = pointIndex / 2;
            var color = PointColor(chart, series, index, item);

            c.DrawCircle(x, y, radius, ChartColor.FromRgba(color.R, color.G, color.B, 78));
            c.DrawCircleOutline(x, y, radius, color, ChartVisualPrimitives.BubbleStrokeWidth);
            c.DrawCircle(x - radius * 0.28, y - radius * 0.28, Math.Max(1.4, radius * 0.18), ChartColor.FromRgba(chart.Options.Theme.CardBackground.R, chart.Options.Theme.CardBackground.G, chart.Options.Theme.CardBackground.B, 66));
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, size);
                var fontSize = PngDataLabelFontSize(chart, series, item);
                var dataStyle = DataLabelStyle(chart, series, item);
                var labelWidth = EstimatePngStyledTextWidth(label, fontSize, dataStyle, emphasized: true);
                var placement = DataLabelPlacement(chart, series);
                var labelX = placement == ChartDataLabelPlacement.Left
                    ? x - radius - labelWidth - 8
                    : placement == ChartDataLabelPlacement.Right
                        ? x + radius + 8
                        : x - labelWidth / 2.0;
                var aboveY = y - radius - fontSize - 4;
                var belowY = y + radius + 4;
                var labelY = placement == ChartDataLabelPlacement.Below
                    ? belowY
                    : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right
                        ? y - fontSize / 2.0
                        : aboveY < plot.Top + 2 ? belowY : aboveY;
                if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved, dataStyle)) continue;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, dataStyle);
            }
        }
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

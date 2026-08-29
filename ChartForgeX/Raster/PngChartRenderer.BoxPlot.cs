using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawBoxPlots(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var boxCount = Math.Max(1, series.Points.Count / 5);
        var boxWidth = Math.Max(14, Math.Min(46, plot.Width / Math.Max(1, boxCount * 5.0)));
        var capWidth = boxWidth * 0.74;
        var reserved = new List<ChartLabelBounds>();

        for (var pointIndex = 0; pointIndex + 4 < series.Points.Count; pointIndex += 5) {
            var minimum = series.Points[pointIndex];
            var q1 = series.Points[pointIndex + 1];
            var median = series.Points[pointIndex + 2];
            var q3 = series.Points[pointIndex + 3];
            var maximum = series.Points[pointIndex + 4];
            var x = map.X(minimum.X);
            var yMin = map.Y(minimum.Y);
            var yQ1 = map.Y(q1.Y);
            var yMedian = map.Y(median.Y);
            var yQ3 = map.Y(q3.Y);
            var yMax = map.Y(maximum.Y);
            var top = Math.Min(yQ1, yQ3);
            var height = Math.Max(2, Math.Abs(yQ3 - yQ1));
            var item = pointIndex / 5;
            var color = PointColor(chart, series, index, item);
            var halo = PngStrokeHalo(color);

            c.DrawLine(x, yMax, x, yMin, halo, ChartVisualPrimitives.BoxPlotPngHaloStrokeWidth);
            c.DrawLine(x - capWidth / 2, yMin, x + capWidth / 2, yMin, halo, ChartVisualPrimitives.BoxPlotPngHaloStrokeWidth);
            c.DrawLine(x - capWidth / 2, yMax, x + capWidth / 2, yMax, halo, ChartVisualPrimitives.BoxPlotPngHaloStrokeWidth);
            c.DrawLine(x, yMax, x, yMin, color, ChartVisualPrimitives.BoxPlotStrokeWidth);
            c.DrawLine(x - capWidth / 2, yMin, x + capWidth / 2, yMin, color, ChartVisualPrimitives.BoxPlotStrokeWidth);
            c.DrawLine(x - capWidth / 2, yMax, x + capWidth / 2, yMax, color, ChartVisualPrimitives.BoxPlotStrokeWidth);
            c.StrokeRoundedRect(x - boxWidth / 2, top, boxWidth, height, ChartVisualPrimitives.BoxPlotBodyRadius, halo, ChartVisualPrimitives.BoxPlotPngBodyHaloStrokeWidth);
            c.FillRoundedRect(x - boxWidth / 2, top, boxWidth, height, ChartVisualPrimitives.BoxPlotBodyRadius, ApplyOpacity(color, ChartVisualPrimitives.BoxPlotBodyFillOpacity));
            c.StrokeRoundedRect(x - boxWidth / 2, top, boxWidth, height, ChartVisualPrimitives.BoxPlotBodyRadius, color, ChartVisualPrimitives.BoxPlotStrokeWidth);
            c.DrawLine(x - boxWidth / 2, yMedian, x + boxWidth / 2, yMedian, halo, ChartVisualPrimitives.BoxPlotPngHaloStrokeWidth);
            c.DrawLine(x - boxWidth / 2, yMedian, x + boxWidth / 2, yMedian, color, ChartVisualPrimitives.BoxPlotMedianStrokeWidth);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, median.Y);
                var fontSize = PngDataLabelFontSize(chart, series, item);
                var dataStyle = DataLabelStyle(chart, series, item);
                var labelWidth = EstimatePngStyledTextWidth(label, fontSize, dataStyle, emphasized: true);
                var labelHeight = EstimatePngStyledTextHeight(fontSize, dataStyle);
                var placement = DataLabelPlacement(chart, series);
                var labelX = placement == ChartDataLabelPlacement.Left
                    ? x - boxWidth / 2.0 - labelWidth - 8
                    : placement == ChartDataLabelPlacement.Right
                        ? x + boxWidth / 2.0 + 8
                        : x - labelWidth / 2.0;
                var labelY = placement == ChartDataLabelPlacement.Below
                    ? Math.Max(yQ1, yQ3) + 4
                    : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right
                        ? yMedian - labelHeight / 2.0
                        : Math.Min(yQ1, yQ3) - labelHeight - 4;
                if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved, dataStyle)) continue;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, dataStyle);
            }
        }
    }
}

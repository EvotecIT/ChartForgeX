using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawOhlc(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 4);
        var tickWidth = Math.Max(7, Math.Min(18, plot.Width / Math.Max(1, itemCount * 6.0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 3 < series.Points.Count; pointIndex += 4) {
            var open = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var low = series.Points[pointIndex + 2];
            var close = series.Points[pointIndex + 3];
            var rising = close.Y >= open.Y;
            var item = pointIndex / 4;
            var semanticColor = rising ? chart.Options.Theme.Positive : chart.Options.Theme.Negative;
            var color = item < series.PointColors.Count && series.PointColors[item] is { } pointColor ? pointColor : semanticColor;
            var x = map.X(open.X);
            var yOpen = map.Y(open.Y);
            var yHigh = map.Y(high.Y);
            var yLow = map.Y(low.Y);
            var yClose = map.Y(close.Y);
            var halo = PngStrokeHalo(color);

            c.DrawLine(x, yHigh, x, yLow, halo, ChartVisualPrimitives.OhlcPngHaloStrokeWidth);
            c.DrawLine(x - tickWidth, yOpen, x, yOpen, halo, ChartVisualPrimitives.OhlcPngHaloStrokeWidth);
            c.DrawLine(x, yClose, x + tickWidth, yClose, halo, ChartVisualPrimitives.OhlcPngHaloStrokeWidth);
            c.DrawLine(x, yHigh, x, yLow, color, ChartVisualPrimitives.OhlcStrokeWidth);
            c.DrawLine(x - tickWidth, yOpen, x, yOpen, color, ChartVisualPrimitives.OhlcStrokeWidth);
            c.DrawLine(x, yClose, x + tickWidth, yClose, color, ChartVisualPrimitives.OhlcStrokeWidth);
            if (!ShouldDrawDataLabels(chart, series)) continue;

            var label = FormatValue(chart, close.Y);
            var fontSize = PngDataLabelFontSize(chart, series, item);
            var dataStyle = DataLabelStyle(chart, series, item);
            var labelWidth = EstimatePngStyledTextWidth(label, fontSize, dataStyle, emphasized: true);
            var placement = DataLabelPlacement(chart, series);
            var labelX = placement == ChartDataLabelPlacement.Left
                ? x - tickWidth - ChartVisualPrimitives.OhlcLabelOffset - labelWidth
                : placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Outside
                    ? x + tickWidth + ChartVisualPrimitives.OhlcLabelOffset
                    : x - labelWidth / 2.0;
            var labelY = placement == ChartDataLabelPlacement.Below
                ? yClose + 4
                : placement == ChartDataLabelPlacement.Above
                    ? yClose - fontSize - 4
                    : yClose - fontSize / 2.0;
            if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reservedLabels, dataStyle)) continue;
            DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, dataStyle);
        }
    }
}

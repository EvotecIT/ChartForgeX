using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawCandlesticks(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 4);
        var candleWidth = Math.Max(8, Math.Min(22, plot.Width / Math.Max(1, itemCount * 5.0)));
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
            var bodyTop = Math.Min(yOpen, yClose);
            var bodyHeight = Math.Max(2, Math.Abs(yClose - yOpen));
            var fill = ApplyOpacity(color, rising ? ChartVisualPrimitives.CandlestickRisingFillOpacity : ChartVisualPrimitives.CandlestickFallingFillOpacity);
            var halo = PngStrokeHalo(color);

            c.DrawLine(x, yHigh, x, yLow, halo, ChartVisualPrimitives.CandlestickPngHaloStrokeWidth);
            c.DrawLine(x, yHigh, x, yLow, color, ChartVisualPrimitives.CandlestickStrokeWidth);
            c.StrokeRoundedRect(x - candleWidth / 2, bodyTop, candleWidth, bodyHeight, ChartVisualPrimitives.CandlestickBodyRadius, halo, ChartVisualPrimitives.CandlestickPngBodyHaloStrokeWidth);
            c.FillRoundedRect(x - candleWidth / 2, bodyTop, candleWidth, bodyHeight, ChartVisualPrimitives.CandlestickBodyRadius, fill);
            c.StrokeRoundedRect(x - candleWidth / 2, bodyTop, candleWidth, bodyHeight, ChartVisualPrimitives.CandlestickBodyRadius, color, ChartVisualPrimitives.CandlestickStrokeWidth);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, close.Y);
                var fontSize = PngDataLabelFontSize(chart, series, item);
                var dataStyle = DataLabelStyle(chart, series, item);
                var labelWidth = EstimatePngStyledTextWidth(label, fontSize, dataStyle, emphasized: true);
                var placement = DataLabelPlacement(chart, series);
                var labelX = placement == ChartDataLabelPlacement.Left
                    ? x - candleWidth / 2.0 - labelWidth - 8
                    : placement == ChartDataLabelPlacement.Right
                        ? x + candleWidth / 2.0 + 8
                        : x - labelWidth / 2.0;
                var aboveY = bodyTop - fontSize - 4;
                var belowY = Math.Max(yOpen, yClose) + 5;
                var labelY = placement == ChartDataLabelPlacement.Below
                    ? belowY
                    : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right
                        ? bodyTop + bodyHeight / 2.0 - fontSize / 2.0
                        : aboveY < plot.Top + 2 ? belowY : aboveY;
                if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reservedLabels, dataStyle)) continue;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, dataStyle);
            }
        }
    }
}

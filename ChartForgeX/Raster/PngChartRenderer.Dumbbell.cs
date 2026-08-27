using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawDumbbells(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var startColor = chart.Options.Theme.MutedText;
        var radius = Math.Max(ChartVisualPrimitives.DumbbellMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.DumbbellMarkerRadiusExtra);
        var reserved = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var start = series.Points[pointIndex];
            var end = series.Points[pointIndex + 1];
            var x = map.X(start.X);
            var yStart = map.Y(start.Y);
            var yEnd = map.Y(end.Y);
            var item = pointIndex / 2;
            var color = PointColor(chart, series, index, item);

            c.DrawLine(x, yStart, x, yEnd, ChartColor.FromRgba(color.R, color.G, color.B, 124), ChartVisualPrimitives.DumbbellConnectorStrokeWidth);
            DrawMarker(c, chart, x, yStart, radius, startColor);
            DrawMarker(c, chart, x, yEnd, radius, color);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, start.Y) + "-" + FormatValue(chart, end.Y);
                var fontSize = PngDataLabelFontSize(chart, series, item);
                var dataStyle = DataLabelStyle(chart, series, item);
                var labelWidth = EstimatePngStyledTextWidth(label, fontSize, dataStyle, true);
                var labelHeight = EstimatePngStyledTextHeight(fontSize, dataStyle);
                var placement = DataLabelPlacement(chart, series);
                var top = Math.Min(yStart, yEnd);
                var bottom = Math.Max(yStart, yEnd);
                var labelX = placement == ChartDataLabelPlacement.Left
                    ? x - radius - labelWidth - 8
                    : placement == ChartDataLabelPlacement.Right
                        ? x + radius + 8
                        : x - labelWidth / 2.0;
                var labelY = placement == ChartDataLabelPlacement.Below
                    ? bottom + radius + 4
                    : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right
                        ? (top + bottom) / 2.0 - labelHeight / 2.0
                        : top - radius - labelHeight - 4;
                if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved, dataStyle)) continue;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, dataStyle);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawPolarArea(RgbaCanvas c, Chart chart, ChartRect plot) {
        var series = chart.Series[0];
        var values = new List<PngIndexedPieValue>();
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
            var point = series.Points[pointIndex];
            if (point.Y > 0) values.Add(new PngIndexedPieValue(point, pointIndex));
        }
        if (values.Count == 0) return;
        var legendValues = new List<PngIndexedPieValue>();
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) legendValues.Add(new PngIndexedPieValue(series.Points[pointIndex], pointIndex));

        var max = 0.0;
        var total = 0.0;
        foreach (var value in values) {
            max = Math.Max(max, value.Point.Y);
            total += value.Point.Y;
        }

        var chartPlot = PngPieChartPlot(chart, plot, legendValues);
        var radius = Math.Max(ChartVisualPrimitives.PolarAreaMinRadius, Math.Min(chartPlot.Width, chartPlot.Height) * ChartVisualPrimitives.PolarAreaRadiusFactor);
        var cx = chartPlot.Left + chartPlot.Width * ChartVisualPrimitives.PolarAreaCenterXFactor;
        var cy = chartPlot.Top + chartPlot.Height / 2;
        var sweep = Math.PI * 2 / series.Points.Count;
        var separator = chart.Options.Theme.CardBackground;

        if (chart.Options.ShowGrid) DrawPolarAreaGrid(c, chart, cx, cy, radius);
        DrawPolarAreaZeroSlots(c, chart, series, cx, cy, radius, sweep);
        for (var i = 0; i < values.Count; i++) {
            var point = values[i].Point;
            var pointIndex = values[i].PointIndex;
            var start = -Math.PI / 2 + pointIndex * sweep;
            var end = start + sweep;
            var segmentRadius = radius * Math.Sqrt(point.Y / max);
            var color = PieSliceColor(chart, series, pointIndex);
            c.FillRingSlice(cx, cy, segmentRadius, 0, start, end, color);
            DrawSliceSeparator(c, cx, cy, segmentRadius, 0, start, separator);
            c.DrawArc(cx, cy, segmentRadius, start, end, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);

            if (ShouldDrawDataLabels(chart, series) && segmentRadius > ChartVisualPrimitives.PolarAreaLabelMinRadius) {
                var mid = start + sweep / 2;
                var label = FormatValue(chart, point.Y);
                var dataStyle = DataLabelStyle(chart, series, pointIndex);
                var fontSize = PngDataLabelFontSize(chart, series, pointIndex);
                var labelWidth = EstimatePngStyledTextWidth(label, fontSize, dataStyle, emphasized: true);
                var labelHeight = EstimatePngStyledTextHeight(fontSize, dataStyle);
                var labelRadius = segmentRadius * ChartVisualPrimitives.PolarAreaLabelRadiusFactor;
                DrawReadablePngLabel(c, plot, cx + Math.Cos(mid) * labelRadius - labelWidth / 2.0, cy + Math.Sin(mid) * labelRadius - labelHeight / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, dataStyle);
            }
        }

        if (chart.Options.ShowGrid) c.DrawCircleOutline(cx, cy, radius, ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.PolarAreaGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
        if (chart.Options.ShowLegend) DrawSliceLegend(c, chart, series, legendValues, plot, total);
    }

    private static void DrawPolarAreaZeroSlots(RgbaCanvas c, Chart chart, ChartSeries series, double cx, double cy, double radius, double sweep) {
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
            if (series.Points[pointIndex].Y > 0) continue;
            var start = -Math.PI / 2 + pointIndex * sweep;
            var end = start + sweep;
            var color = PieSliceColor(chart, series, pointIndex);
            var fill = ApplyOpacity(color, ChartVisualPrimitives.PolarAreaZeroSlotFillOpacity);
            var stroke = ApplyOpacity(color, ChartVisualPrimitives.PolarAreaZeroSlotStrokeOpacity);
            var inner = radius * ChartVisualPrimitives.PolarAreaZeroSlotInnerRadiusFactor;
            c.FillRingSlice(cx, cy, radius, inner, start, end, fill);
            DrawSliceSeparator(c, cx, cy, radius, inner, start, stroke);
            DrawSliceSeparator(c, cx, cy, radius, inner, end, stroke);
            c.DrawArc(cx, cy, radius, start, end, stroke, ChartVisualPrimitives.GridStrokeWidth);
            c.DrawArc(cx, cy, inner, start, end, stroke, ChartVisualPrimitives.GridStrokeWidth);
        }
    }

    private static void DrawPolarAreaGrid(RgbaCanvas c, Chart chart, double cx, double cy, double radius) {
        var grid = ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.PolarAreaGridOpacity);
        for (var i = 1; i <= ChartVisualPrimitives.PolarAreaGridRings; i++) c.DrawCircleOutline(cx, cy, radius * i / ChartVisualPrimitives.PolarAreaGridRings, grid, ChartVisualPrimitives.GridStrokeWidth);
    }
}

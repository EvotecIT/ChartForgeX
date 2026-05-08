using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawRangeArea(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var lower = new List<ChartPoint>();
        var upper = new List<ChartPoint>();
        var middle = new List<ChartPoint>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            lower.Add(new ChartPoint(map.X(low.X), map.Y(low.Y)));
            upper.Add(new ChartPoint(map.X(high.X), map.Y(high.Y)));
            middle.Add(new ChartPoint(map.X(low.X), map.Y((low.Y + high.Y) / 2.0)));
        }

        if (lower.Count == 0) return;
        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var middlePath = ChartPathBuilder.FromPoints(middle, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var style = chart.Options.LineVisualStyle;
        var polygon = new List<ChartPoint>(upperPath.Count + lowerPath.Count);
        foreach (var point in upperPath) polygon.Add(point);
        for (var i = lowerPath.Count - 1; i >= 0; i--) polygon.Add(lowerPath[i]);
        c.FillPolygonVerticalGradient(polygon, ChartColor.FromRgba(color.R, color.G, color.B, 118), ChartColor.FromRgba(color.R, color.G, color.B, 18));
        DrawDashedPngPath(c, middlePath, ApplyOpacity(color, ChartVisualPrimitives.RangeAreaMidlineOpacity), ChartVisualPrimitives.RangeAreaMidlineStrokeWidth, ChartVisualPrimitives.RangeAreaDash, ChartVisualPrimitives.RangeAreaGap);
        DrawPremiumPngLinePath(c, upperPath, color, series.StrokeWidth, style);
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0) DrawPngLinePath(c, lowerPath, PngStrokeAmbientHalo(color, style), series.StrokeWidth + style.AmbientHaloStrokeExtra);
        if (style.HaloOpacity > 0 && style.HaloStrokeExtra > 0) DrawPngLinePath(c, lowerPath, PngStrokeHalo(color, style.HaloOpacity), series.StrokeWidth + ChartVisualPrimitives.RangeAreaHaloStrokeExtra);
        DrawPngLinePath(c, lowerPath, ApplyOpacity(color, ChartVisualPrimitives.RangeAreaLowerStrokeOpacity), Math.Max(ChartVisualPrimitives.RangeAreaMinStrokeWidth, series.StrokeWidth));
        if (style.HighlightOpacity > 0) DrawPngLinePath(c, lowerPath, PngLineHighlight(style), Math.Max(1.0, series.StrokeWidth * style.HighlightStrokeRatio));

        if (!ShouldDrawDataLabels(chart, series)) return;
        var reserved = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var item = pointIndex / 2;
            var label = FormatValue(chart, low.Y) + "-" + FormatValue(chart, high.Y);
            var fontSize = PngDataLabelFontSize(chart, series, item);
            var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
            var yLow = map.Y(low.Y);
            var yHigh = map.Y(high.Y);
            var top = Math.Min(yLow, yHigh);
            var bottom = Math.Max(yLow, yHigh);
            var placement = DataLabelPlacement(chart, series);
            var centerX = map.X(low.X);
            var x = placement == ChartDataLabelPlacement.Left
                ? centerX - labelWidth - 8
                : placement == ChartDataLabelPlacement.Right
                    ? centerX + 8
                    : centerX - labelWidth / 2.0;
            var y = placement == ChartDataLabelPlacement.Below
                ? bottom + 4
                : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right
                    ? (top + bottom) / 2.0 - fontSize / 2.0
                    : top - fontSize - 5;
            if (!ReservePngLabel(label, x, y, chart, plot, fontSize, reserved)) continue;
            DrawReadablePngLabel(c, plot, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, DataLabelStyle(chart, series, item));
        }
    }

    private static void DrawDashedPngPath(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, double thickness, double dash, double gap) {
        for (var i = 1; i < points.Count; i++) {
            var a = points[i - 1];
            var b = points[i];
            c.DrawDashedLine(a.X, a.Y, b.X, b.Y, color, thickness, dash, gap);
        }
    }
}

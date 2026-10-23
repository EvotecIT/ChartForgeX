using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawErrorBar(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, ChartSeries series, int seriesIndex, int item, ChartColor color, SvgPaint sourcePaint, double radius,
        VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        ChartAxisWindow? xViewport, ChartAxisWindow? yViewport) {
        var source = series.ErrorBars[item];
        var sourceLeft = source.XBounds?.Lower ?? source.X; var sourceRight = source.XBounds?.Upper ?? source.X;
        var sourceLow = source.YBounds?.Lower ?? source.Y; var sourceHigh = source.YBounds?.Upper ?? source.Y;
        var visible = map.OverlapsSourceWindow(xViewport, sourceLeft, sourceRight, horizontalAxis: true)
            && map.OverlapsSourceWindow(yViewport, sourceLow, sourceHigh, horizontalAxis: false);
        var x = map.X(source.X); var y = map.Y(source.Y);
        var left = map.X(sourceLeft); var right = map.X(sourceRight); var low = map.Y(sourceLow); var high = map.Y(sourceHigh);
        var horizontalCap = ErrorCap(plot.Width, series.ErrorBars.Count); var verticalCap = ErrorCap(plot.Height, series.ErrorBars.Count);
        var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.ErrorBarStrokeWidth;
        var extent = VisualMarkerScene.FootprintExtent(series, radius);
        var halfWidth = Math.Max(extent, source.YBounds.HasValue ? (horizontalCap + stroke) / 2 : 0);
        var halfHeight = Math.Max(extent, source.XBounds.HasValue ? (verticalCap + stroke) / 2 : 0);
        var xStroke = source.XBounds.HasValue ? stroke / 2 : 0; var yStroke = source.YBounds.HasValue ? stroke / 2 : 0;
        var bounds = Extents(Math.Min(x - halfWidth, Math.Min(left, right) - xStroke), Math.Min(y - halfHeight, Math.Min(low, high) - yStroke),
            Math.Max(x + halfWidth, Math.Max(left, right) + xStroke), Math.Max(y + halfHeight, Math.Max(low, high) + yStroke));
        var label = ResolveObservationLabel(chart, context, series, item, colors, format => format("value", source.Y));
        var values = new List<(string Name, double Value)> { ("x", source.X), ("value", source.Y) };
        if (source.XBounds is { } xb) { values.Add(("x-lower", xb.Lower)); values.Add(("x-upper", xb.Upper)); }
        if (source.YBounds is { } yb) { values.Add(("y-lower", yb.Lower)); values.Add(("y-upper", yb.Upper)); }
        using (ObservationGroup(builder, series, seriesIndex, item, item * 3, 3, bounds, label, visible, null, values.ToArray())) {
            if (visible) {
                var paint = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.ErrorBarRangeOpacity);
                var linePaint = VisualChartPaint.Stroke(sourcePaint.WithOpacity(paint, ChartVisualPrimitives.ErrorBarRangeOpacity));
                var capPaint = VisualChartPaint.Stroke(sourcePaint);
                if (source.XBounds.HasValue) {
                    builder.Line(left, y, right, y, paint, stroke, role: "error-range-x", paint: linePaint);
                    builder.Line(left, y - verticalCap / 2, left, y + verticalCap / 2, color, stroke, role: "error-cap-x", paint: capPaint);
                    builder.Line(right, y - verticalCap / 2, right, y + verticalCap / 2, color, stroke, role: "error-cap-x", paint: capPaint);
                }
                if (source.YBounds.HasValue) {
                    builder.Line(x, low, x, high, paint, stroke, role: "error-range", paint: linePaint);
                    builder.Line(x - horizontalCap / 2, low, x + horizontalCap / 2, low, color, stroke, role: "error-cap", paint: capPaint);
                    builder.Line(x - horizontalCap / 2, high, x + horizontalCap / 2, high, color, stroke, role: "error-cap", paint: capPaint);
                }
                VisualMarkerScene.Draw(builder, series, item, x, y, radius, color, sourcePaint, "error-marker",
                    pattern: ObservationPattern(series, item), backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "error-marker-pattern");
            }
        }
        if (visible) ObservationLabel(chart, context, series, seriesIndex, item, new ChartPoint(x, y), bounds, source.Y, label, labels, obstacles);
    }

    private static double ErrorCap(double pixels, int count) => Math.Max(9, Math.Min(24, pixels / Math.Max(1, count * 8)));

    /// <summary>Fits both physical whiskers and their perpendicular caps through the existing automatic-axis paint owner.</summary>
    private static void IncludeErrorBarPaintExtents(ChartSeries series, VisualRenderContext context, ChartRect plot,
        ChartAxis xAxis, ChartAxis yAxis, MarkerExtents x, MarkerExtents y) {
        var radius = VisualMarkerScene.Enabled(series) ? VisualMarkerScene.Extent(series, ResolveMarkerRadius(series, context)) : 0;
        var strokeRadius = (series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.ErrorBarStrokeWidth) / 2;
        foreach (var source in series.ErrorBars) {
            x.Include(source.X, radius, xAxis); y.Include(source.Y, radius, yAxis);
            if (source.XBounds is { } xb) {
                x.Include(xb.Lower, strokeRadius, xAxis); x.Include(xb.Upper, strokeRadius, xAxis);
                y.Include(source.Y, ErrorCap(plot.Height, series.ErrorBars.Count) / 2 + strokeRadius, yAxis);
            }
            if (source.YBounds is { } yb) {
                y.Include(yb.Lower, strokeRadius, yAxis); y.Include(yb.Upper, strokeRadius, yAxis);
                x.Include(source.X, ErrorCap(plot.Width, series.ErrorBars.Count) / 2 + strokeRadius, xAxis);
            }
        }
    }
}

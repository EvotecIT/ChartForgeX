using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawExtensionPoints(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        ChartBubbleSizeScale? bubbleScale, ChartSeriesColorSurface? colorSurface, ChartAxisWindow? xViewport = null, ChartAxisWindow? yViewport = null) {
        var series = chart.Series[index];
        if (series.Kind == ChartSeriesKind.Slope || series.Kind == ChartSeriesKind.TrendLine) {
            DrawEndpointLine(chart, context, builder, plot, map, index, colors, labels, obstacles, xViewport, yViewport);
            return;
        }
        var stride = ObservationStride(series.Kind);
        var count = series.RenderPoints.Count / stride;
        var quantitativeOutline = VisualMarkerScene.QuantitativeOutline(series, colors);
        for (var item = 0; item < count; item++) {
            var raw = item * stride;
            var point = series.RenderPoints[raw];
            var visible = ViewportObservationVisible(xViewport, null, point.X, point.Y, point.Y, map);
            var minimumY = point.Y; var maximumY = point.Y;
            if (series.Kind != ChartSeriesKind.Bubble)
                for (var bound = 1; bound < stride; bound++) { minimumY = Math.Min(minimumY, series.RenderPoints[raw + bound].Y); maximumY = Math.Max(maximumY, series.RenderPoints[raw + bound].Y); }
            visible = visible && ViewportObservationVisible(null, yViewport, point.X, minimumY, maximumY, map);
            var x = map.X(point.X); var y = map.Y(point.Y);
            var color = PointColor(series, index, item, colors);
            var sourcePaint = VisualChartPaint.Series(series, color, item);
            if (colorSurface != null) (color, sourcePaint) = colorSurface.Paint(series, index, item, colors);
            var radius = ResolveMarkerRadius(series, context);
            if (series.Kind == ChartSeriesKind.Bubble) {
                var size = series.RenderPoints[raw + 1].Y;
                radius = VisualMarkerScene.Enabled(series) ? bubbleScale!.Radius(size, plot, series.Markers.Shape) : 0;
                var bounds = Extents(x, y, x, y, Math.Max(radius, VisualMarkerScene.Extent(series, radius)));
                var label = ResolveObservationLabel(chart, context, series, item, colors, _ => bubbleScale!.Presentation.Value(size));
                var fill = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleFillOpacity);
                var fillPaint = sourcePaint.WithOpacity(fill, ChartVisualPrimitives.BubbleFillOpacity);
                var insideSourceX = ViewportObservationVisible(xViewport, yViewport, point.X, point.Y, point.Y, map);
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, colorSurface!, bubbleScale!.Presentation, insideSourceX, ("x", point.X), ("y", point.Y), ("size", size))) {
                    var outline = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleStrokeOpacity);
                    if (insideSourceX) VisualMarkerScene.Draw(builder, series, item, x, y, radius, fill, fillPaint,
                        "bubble", outline, VisualMarkerScene.FamilyStrokeWidth(series), sourcePaint.WithOpacity(outline, ChartVisualPrimitives.BubbleStrokeOpacity),
                        ObservationPattern(series, item), ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "bubble-pattern",
                        quantitativeOutline: quantitativeOutline);
                }
                if (!insideSourceX) continue;
                var actualFill = colorSurface?.Scale != null ? VisualMarkerScene.ResolveFill(series, item, fill, fillPaint)
                    : ((ChartColor Color, SvgPaint Paint)?)null;
                ObservationLabel(chart, context, series, index, item, new ChartPoint(x, y - radius), bounds, size, label, labels, obstacles,
                    markFill: actualFill?.Color, markFillPaint: actualFill?.Paint);
            } else if (series.Kind == ChartSeriesKind.ErrorBar) {
                DrawErrorBar(chart, context, builder, plot, map, series, index, item, color, sourcePaint, radius, colors, labels, obstacles, xViewport, yViewport);
            } else if (series.Kind == ChartSeriesKind.Dumbbell) {
                var end = series.RenderPoints[raw + 1].Y; var endY = map.Y(end);
                var bounds = Extents(x, y, x, endY, Math.Max(radius, VisualMarkerScene.Extent(series, radius)));
                var label = ResolveObservationLabel(chart, context, series, item, colors, format => format("start", point.Y) + "–" + format("end", end));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, visible, null, ("x", point.X), ("start", point.Y), ("end", end), ("delta", end - point.Y))) {
                    if (visible) {
                        builder.Line(x, y, x, endY, ChartColorMath.WithOpacity(color, ChartVisualPrimitives.DumbbellConnectorOpacity),
                            series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.DumbbellConnectorStrokeWidth, role: "dumbbell-connector",
                            paint: VisualChartPaint.Stroke(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, ChartVisualPrimitives.DumbbellConnectorOpacity), ChartVisualPrimitives.DumbbellConnectorOpacity)));
                        VisualMarkerScene.Draw(builder, series, item, x, y, radius, colors.MutedForeground, SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text), "dumbbell-start");
                        VisualMarkerScene.Draw(builder, series, item, x, endY, radius, color, sourcePaint, "dumbbell-end",
                            pattern: ObservationPattern(series, item), backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "dumbbell-pattern");
                    }
                }
                if (visible) ObservationLabel(chart, context, series, index, item, new ChartPoint(x, endY), bounds, end, label, labels, obstacles);
            } else if (series.Kind == ChartSeriesKind.Lollipop) {
                var baseline = map.YBaseline();
                visible = ViewportObservationVisible(xViewport, null, point.X, point.Y, point.Y, map) && ViewportLogicalYVisible(yViewport, plot, baseline, y);
                var bounds = Extents(x, baseline, x, y, Math.Max(radius, VisualMarkerScene.Extent(series, radius)));
                var label = ResolveObservationLabel(chart, context, series, item, colors, format => format("value", point.Y));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, visible, null, ("x", point.X), ("value", point.Y))) {
                    if (visible) {
                        builder.Line(x, baseline, x, y, ChartColorMath.WithOpacity(color, ChartVisualPrimitives.LollipopStemOpacity),
                            Math.Max(ChartVisualPrimitives.LollipopStemMinStrokeWidth, SeriesStroke(series, context) * .62), role: "lollipop-stem",
                            paint: VisualChartPaint.Stroke(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, ChartVisualPrimitives.LollipopStemOpacity), ChartVisualPrimitives.LollipopStemOpacity)));
                        VisualMarkerScene.Draw(builder, series, item, x, y, radius, color, sourcePaint, "lollipop-marker",
                            colors.Surface, VisualMarkerScene.FamilyStrokeWidth(series), SvgPaint.Of(colors.Surface, SvgColorRole.Surface),
                            ObservationPattern(series, item), ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "lollipop-pattern");
                    }
                }
                var axis = ChartCartesianOrientation.ValueAxis(chart, series.ValueAxis);
                if (visible) ObservationLabel(chart, context, series, index, item, new ChartPoint(x, y), bounds, point.Y, label, labels, obstacles,
                    MappedBarDirection(axis, point.Y, baseline, y));
            }
        }
    }

    private static void DrawEndpointLine(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        ChartAxisWindow? xViewport = null, ChartAxisWindow? yViewport = null) {
        var series = chart.Series[index];
        if (series.RenderPoints.Count < 2) return;
        var first = series.RenderPoints[0]; var last = series.RenderPoints[series.RenderPoints.Count - 1];
        var points = new[] { new ChartPoint(map.X(first.X), map.Y(first.Y)), new ChartPoint(map.X(last.X), map.Y(last.Y)) };
        var path = ChartPathBuilder.FromPoints(points, ChartInterpolation.Linear);
        var trend = series.Kind == ChartSeriesKind.TrendLine;
        var sourceColor = Color(series, index, colors);
        DrawLayeredPath(chart, builder, path, sourceColor, VisualChartPaint.Series(series, sourceColor), Math.Max(ChartVisualPrimitives.TrendLineMinStrokeWidth, SeriesStroke(series, context)),
            trend ? "trend-line" : "slope-line", trend ? new[] { 8d, 6d } : null);
        if (trend) {
            var slope = (last.Y - first.Y) / (last.X - first.X);
            var intercept = first.Y - slope * first.X;
            builder.PushGroup(SeriesId(index) + "-regression", "regression", new Dictionary<string, string> {
                ["data-cfx-slope"] = Number(slope), ["data-cfx-intercept"] = Number(intercept), ["data-cfx-source-count"] = Number(series.TrendLineSourcePoints.Count)
            }).Dispose();
        }
        obstacles.Add(new LabelObstacle(SeriesId(index) + "-line", new LabelMarkShape(new[] { points.ToList() }, false, SeriesStroke(series, context), plot)));
        var radius = ResolveMarkerRadius(series, context);
        for (var endpoint = 0; endpoint < 2; endpoint++) {
            var raw = endpoint == 0 ? 0 : series.RenderPoints.Count - 1; var source = series.RenderPoints[raw]; var point = points[endpoint];
            var bounds = Extents(point.X, point.Y, point.X, point.Y, trend ? 0 : Math.Max(radius, VisualMarkerScene.Extent(series, radius)));
            var label = ResolveObservationLabel(chart, context, series, endpoint, colors, format => format("value", source.Y));
            var visible = ViewportObservationVisible(xViewport, yViewport, source.X, source.Y, source.Y, map);
            using (ObservationGroup(builder, series, index, endpoint, raw, 1, bounds, label, visible, null, ("x", source.X), ("value", source.Y))) {
                if (visible && !trend) {
                    var color = PointColor(series, index, endpoint, colors);
                    VisualMarkerScene.Draw(builder, series, endpoint, point.X, point.Y, radius, color, VisualChartPaint.Series(series, color, endpoint), "slope-marker",
                        pattern: ObservationPattern(series, endpoint), backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "slope-pattern");
                }
            }
            if (visible) ObservationLabel(chart, context, series, index, endpoint, point, bounds, source.Y, label, labels, obstacles);
        }
    }
}

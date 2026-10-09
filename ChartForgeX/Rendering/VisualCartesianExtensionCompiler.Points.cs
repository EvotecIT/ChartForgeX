using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawExtensionPoints(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index];
        if (series.Kind == ChartSeriesKind.Slope || series.Kind == ChartSeriesKind.TrendLine) {
            DrawEndpointLine(chart, context, builder, plot, map, index, colors, labels, obstacles);
            return;
        }
        var stride = ObservationStride(series.Kind);
        var count = series.Points.Count / stride;
        var minSize = series.Kind == ChartSeriesKind.Bubble && count > 0 ? Enumerable.Range(0, count).Min(i => series.Points[i * 2 + 1].Y) : 0;
        var maxSize = series.Kind == ChartSeriesKind.Bubble && count > 0 ? Enumerable.Range(0, count).Max(i => series.Points[i * 2 + 1].Y) : 0;
        for (var item = 0; item < count; item++) {
            var raw = item * stride;
            var point = series.Points[raw];
            var x = map.X(point.X); var y = map.Y(point.Y);
            var color = PointColor(series, index, item, colors);
            var sourcePaint = VisualChartPaint.Series(series, color, item);
            var radius = ResolveMarkerRadius(series, context);
            if (series.Kind == ChartSeriesKind.Bubble) {
                var size = series.Points[raw + 1].Y;
                radius = ResolveBubbleRadius(series, context, plot, minSize, maxSize, size);
                var bounds = Extents(x, y, x, y, radius);
                var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, size));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, ("x", point.X), ("y", point.Y), ("size", size))) {
                    var fill = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleFillOpacity);
                    builder.Ellipse(x, y, radius, radius, fill, ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleStrokeOpacity),
                        series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BubbleStrokeWidth, role: "bubble",
                        paint: new VisualScenePaintBinding(fill: sourcePaint.WithOpacity(fill, ChartVisualPrimitives.BubbleFillOpacity),
                            stroke: sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BubbleStrokeOpacity), ChartVisualPrimitives.BubbleStrokeOpacity)));
                    DrawPattern(builder, EllipsePath(x, y, radius, radius), ObservationPattern(series, item), fill, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "bubble-pattern");
                }
                ObservationLabel(chart, context, series, index, item, new ChartPoint(x, y - radius), bounds, size, label, labels, obstacles);
            } else if (series.Kind == ChartSeriesKind.ErrorBar) {
                var lower = series.Points[raw + 1].Y; var upper = series.Points[raw + 2].Y;
                var lowerY = map.Y(lower); var upperY = map.Y(upper);
                var cap = Math.Max(9, Math.Min(24, plot.Width / Math.Max(1, count * 8)));
                var bounds = Extents(x - cap / 2, Math.Min(y - radius, Math.Min(lowerY, upperY)), x + cap / 2, Math.Max(y + radius, Math.Max(lowerY, upperY)));
                var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, point.Y));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, ("x", point.X), ("value", point.Y), ("lower", lower), ("upper", upper))) {
                    var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.ErrorBarStrokeWidth;
                    var paint = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.ErrorBarRangeOpacity);
                    builder.Line(x, lowerY, x, upperY, paint, stroke, role: "error-range", paint: VisualChartPaint.Stroke(sourcePaint.WithOpacity(paint, ChartVisualPrimitives.ErrorBarRangeOpacity)));
                    builder.Line(x - cap / 2, lowerY, x + cap / 2, lowerY, color, stroke, role: "error-cap", paint: VisualChartPaint.Stroke(sourcePaint));
                    builder.Line(x - cap / 2, upperY, x + cap / 2, upperY, color, stroke, role: "error-cap", paint: VisualChartPaint.Stroke(sourcePaint));
                    builder.Ellipse(x, y, radius, radius, color, role: "error-marker", paint: VisualChartPaint.Fill(sourcePaint));
                    DrawPattern(builder, EllipsePath(x, y, radius, radius), ObservationPattern(series, item), color, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "error-marker-pattern");
                }
                ObservationLabel(chart, context, series, index, item, new ChartPoint(x, y), bounds, point.Y, label, labels, obstacles);
            } else if (series.Kind == ChartSeriesKind.Dumbbell) {
                var end = series.Points[raw + 1].Y; var endY = map.Y(end);
                var bounds = Extents(x, y, x, endY, radius);
                var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, point.Y) + "–" + Value(chart, end));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, ("x", point.X), ("start", point.Y), ("end", end), ("delta", end - point.Y))) {
                    builder.Line(x, y, x, endY, ChartColorMath.WithOpacity(color, ChartVisualPrimitives.DumbbellConnectorOpacity),
                        series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.DumbbellConnectorStrokeWidth, role: "dumbbell-connector",
                        paint: VisualChartPaint.Stroke(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, ChartVisualPrimitives.DumbbellConnectorOpacity), ChartVisualPrimitives.DumbbellConnectorOpacity)));
                    builder.Ellipse(x, y, radius, radius, colors.MutedForeground, role: "dumbbell-start", paint: VisualChartPaint.Fill(colors.MutedForeground, SvgColorRole.Text));
                    builder.Ellipse(x, endY, radius, radius, color, role: "dumbbell-end", paint: VisualChartPaint.Fill(sourcePaint));
                    DrawPattern(builder, EllipsePath(x, endY, radius, radius), ObservationPattern(series, item), color, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "dumbbell-pattern");
                }
                ObservationLabel(chart, context, series, index, item, new ChartPoint(x, endY), bounds, end, label, labels, obstacles);
            } else if (series.Kind == ChartSeriesKind.Lollipop) {
                var baseline = map.YBaseline(); var bounds = Extents(x, baseline, x, y, radius);
                var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, point.Y));
                using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, ("x", point.X), ("value", point.Y))) {
                    builder.Line(x, baseline, x, y, ChartColorMath.WithOpacity(color, ChartVisualPrimitives.LollipopStemOpacity),
                        Math.Max(ChartVisualPrimitives.LollipopStemMinStrokeWidth, SeriesStroke(series, context) * .62), role: "lollipop-stem",
                        paint: VisualChartPaint.Stroke(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, ChartVisualPrimitives.LollipopStemOpacity), ChartVisualPrimitives.LollipopStemOpacity)));
                    builder.Ellipse(x, y, radius, radius, color, colors.Surface, ChartVisualPrimitives.LollipopMarkerStrokeWidth, role: "lollipop-marker",
                        paint: new VisualScenePaintBinding(fill: sourcePaint, stroke: SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                    DrawPattern(builder, EllipsePath(x, y, radius, radius), ObservationPattern(series, item), color, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "lollipop-pattern");
                }
                ObservationLabel(chart, context, series, index, item, new ChartPoint(x, y), bounds, point.Y, label, labels, obstacles);
            }
        }
    }

    private static void DrawEndpointLine(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var first = series.Points[0]; var last = series.Points[series.Points.Count - 1];
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
                ["data-cfx-slope"] = Number(slope), ["data-cfx-intercept"] = Number(intercept), ["data-cfx-source-count"] = Number(series.TrendSourcePoints.Count)
            }).Dispose();
        }
        obstacles.Add(new LabelObstacle(SeriesId(index) + "-line", new LabelMarkShape(new[] { points.ToList() }, false, SeriesStroke(series, context), plot)));
        var radius = ResolveMarkerRadius(series, context);
        for (var endpoint = 0; endpoint < 2; endpoint++) {
            var raw = endpoint == 0 ? 0 : series.Points.Count - 1; var source = series.Points[raw]; var point = points[endpoint];
            var bounds = Extents(point.X, point.Y, point.X, point.Y, trend ? 0 : radius);
            var label = ResolveObservationLabel(chart, context, series, endpoint, colors, () => Value(chart, source.Y));
            using (ObservationGroup(builder, series, index, endpoint, raw, 1, bounds, label, ("x", source.X), ("value", source.Y))) {
                if (!trend) {
                    var color = PointColor(series, index, endpoint, colors);
                    builder.Ellipse(point.X, point.Y, radius, radius, color, role: "slope-marker", paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, endpoint)));
                    DrawPattern(builder, EllipsePath(point.X, point.Y, radius, radius), ObservationPattern(series, endpoint), color, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "slope-pattern");
                }
            }
            ObservationLabel(chart, context, series, index, endpoint, point, bounds, source.Y, label, labels, obstacles);
        }
    }
}

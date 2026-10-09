using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawRangeEnvelope(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index]; var count = series.Points.Count / 2;
        var lower = new List<ChartPoint>(); var upper = new List<ChartPoint>(); var middle = new List<ChartPoint>();
        var area = series.Kind == ChartSeriesKind.RangeArea; var color = Color(series, index, colors);
        var sourcePaint = VisualChartPaint.Series(series, color);
        for (var item = 0; item < count; item++) {
            var low = series.Points[item * 2]; var high = series.Points[item * 2 + 1]; var x = map.X(low.X);
            var breakBefore = low.BreakBefore || high.BreakBefore;
            lower.Add(new ChartPoint(x, map.Y(low.Y), breakBefore)); upper.Add(new ChartPoint(x, map.Y(high.Y), breakBefore));
            middle.Add(new ChartPoint(x, map.Y((low.Y + high.Y) / 2), breakBefore));
        }
        var offset = 0;
        foreach (var segment in ChartPointSegments.Split(upper)) {
            var lowSegment = lower.GetRange(offset, segment.Count);
            var highPath = ChartPathBuilder.FromPoints(segment, series.Interpolation, series.StepPosition);
            var lowPath = ChartPathBuilder.FromPoints(lowSegment, series.Interpolation, series.StepPosition);
            var highFlat = highPath.Flatten(12); var lowFlat = lowPath.Flatten(12);
            var commands = new List<ChartPathCommand>();
            commands.Add(ChartPathCommand.MoveTo(highFlat[0].X, highFlat[0].Y));
            for (var p = 1; p < highFlat.Count; p++) commands.Add(ChartPathCommand.LineTo(highFlat[p].X, highFlat[p].Y));
            for (var p = lowFlat.Count - 1; p >= 0; p--) commands.Add(ChartPathCommand.LineTo(lowFlat[p].X, lowFlat[p].Y));
            var polygon = new ChartPath(commands); var opacity = area ? context.Theme.AreaOpacity : ChartVisualPrimitives.RangeBandFillOpacity;
            var fill = ChartColorMath.WithOpacity(color, opacity);
            builder.Path(polygon, fill, role: area ? "range-area" : "range-band", close: true,
                paint: VisualChartPaint.Fill(sourcePaint.WithOpacity(fill, opacity)));
            DrawPattern(builder, polygon, series.FillPattern, fill, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), area ? "range-area-pattern" : "range-band-pattern");
            var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : area ? Math.Max(ChartVisualPrimitives.RangeAreaMinStrokeWidth, context.Theme.SeriesStrokeWidth) : ChartVisualPrimitives.RangeBandBoundaryStrokeWidth;
            var upperOpacity = area ? 1 : ChartVisualPrimitives.RangeBandBoundaryOpacity;
            var lowerOpacity = area ? ChartVisualPrimitives.RangeAreaLowerStrokeOpacity : ChartVisualPrimitives.RangeBandBoundaryOpacity;
            var upperColor = ChartColorMath.WithOpacity(color, upperOpacity); var lowerColor = ChartColorMath.WithOpacity(color, lowerOpacity);
            DrawLayeredPath(chart, builder, highPath, upperColor, sourcePaint.WithOpacity(upperColor, upperOpacity), stroke, "range-upper");
            DrawLayeredPath(chart, builder, lowPath, lowerColor, sourcePaint.WithOpacity(lowerColor, lowerOpacity), stroke, "range-lower");
            if (area) builder.Path(ChartPathBuilder.FromPoints(middle.GetRange(offset, segment.Count), series.Interpolation, series.StepPosition),
                stroke: ChartColorMath.WithOpacity(color, ChartVisualPrimitives.RangeAreaMidlineOpacity), strokeWidth: ChartVisualPrimitives.RangeAreaMidlineStrokeWidth,
                role: "range-midline", dash: new[] { ChartVisualPrimitives.RangeAreaDash, ChartVisualPrimitives.RangeAreaGap },
                paint: VisualChartPaint.Stroke(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, ChartVisualPrimitives.RangeAreaMidlineOpacity), ChartVisualPrimitives.RangeAreaMidlineOpacity)));
            obstacles.Add(new LabelObstacle(SeriesId(index) + "-envelope-" + Number(offset), new LabelMarkShape(new[] { highFlat.Concat(lowFlat.AsEnumerable().Reverse()).ToList() }, true, stroke, plot)));
            offset += segment.Count;
        }
        for (var item = 0; item < count; item++) {
            var low = series.Points[item * 2]; var high = series.Points[item * 2 + 1];
            var hasOverride = item < series.PointColors.Count && series.PointColors[item].HasValue || item < series.PointFillPatterns.Count && series.PointFillPatterns[item].HasValue;
            var showMarkers = VisualMarkerScene.Enabled(series, hasOverride || series.MarkerRadius.HasValue);
            var radius = showMarkers ? series.MarkerRadius ?? context.Theme.MarkerRadius : 0;
            var bounds = Extents(lower[item].X, lower[item].Y, upper[item].X, upper[item].Y, VisualMarkerScene.Extent(series, radius));
            var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, low.Y) + "–" + Value(chart, high.Y));
            using (ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label, ("x", low.X), ("lower", low.Y), ("upper", high.Y))) {
                // Per-observation colours/textures are represented by boundary markers rather than repainting the continuous envelope.
                if (showMarkers) {
                    var pointColor = PointColor(series, index, item, colors);
                    var pointPaint = VisualChartPaint.Series(series, pointColor, item);
                    VisualMarkerScene.Draw(builder, series, item, upper[item].X, upper[item].Y, radius, pointColor, pointPaint, "range-marker",
                        pattern: ObservationPattern(series, item), backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "range-marker-pattern");
                    VisualMarkerScene.Draw(builder, series, item, lower[item].X, lower[item].Y, radius, pointColor, pointPaint, "range-marker",
                        pattern: ObservationPattern(series, item), backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "range-marker-pattern");
                }
            }
            var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
            var anchor = placement == ChartDataLabelPlacement.Below ? lower[item]
                : placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right ? middle[item] : upper[item];
            AddLabel(chart, context, series, index, item, anchor, bounds, label, labels, high.Y);
        }
    }

    private static void DrawRangeBars(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index]; var count = series.Points.Count / 2;
        var width = Math.Max(8, Math.Min(28, plot.Width / Math.Max(1, count * 4)));
        var style = chart.Options.ResolvePreparedBarVisualStyle();
        for (var item = 0; item < count; item++) {
            var first = series.Points[item * 2]; var second = series.Points[item * 2 + 1];
            var x = map.X(first.X); var firstY = map.Y(first.Y); var secondY = map.Y(second.Y);
            var bounds = new ChartRect(x - width / 2, Math.Min(firstY, secondY), width, Math.Max(2, Math.Abs(firstY - secondY)));
            var color = PointColor(series, index, item, colors);
            var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, Math.Min(first.Y, second.Y)) + "–" + Value(chart, Math.Max(first.Y, second.Y)));
            using (ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label, ("x", first.X), ("start", first.Y), ("end", second.Y))) {
                DrawBarSurface(chart, context, builder, series, item, bounds, color, colors, "range-bar", true);
                var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.RangeBarCapStrokeWidth;
                if (style.Kind == ChartBarStyle.SegmentedCapsule) {
                    DrawRangeCap(firstY); DrawRangeCap(secondY);
                } else {
                    builder.Line(x - width * .75, firstY, x + width * .75, firstY, color, stroke, role: "range-bar-cap", paint: VisualChartPaint.Stroke(VisualChartPaint.Series(series, color, item)));
                    builder.Line(x - width * .75, secondY, x + width * .75, secondY, color, stroke, role: "range-bar-cap", paint: VisualChartPaint.Stroke(VisualChartPaint.Series(series, color, item)));
                }
                void DrawRangeCap(double y) {
                    var geometry = ChartSegmentedBarGeometry.RangeCap(style, x, y, bounds.Width);
                    DrawSegmentedCap(builder, geometry, style, color, "range-bar", VisualChartPaint.Series(series, color, item));
                }
            }
            ObservationLabel(chart, context, series, index, item, new ChartPoint(x, Math.Min(firstY, secondY)), bounds, Math.Max(first.Y, second.Y), label, labels, obstacles);
        }
    }
}

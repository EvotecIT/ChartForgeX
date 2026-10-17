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
        var series = chart.Series[index]; var count = series.RenderPoints.Count / 2;
        var paired = new ChartPairedBoundaryGeometry(series, map);
        var lower = paired.First; var upper = paired.Second; var middle = new List<ChartPoint>();
        var area = series.Kind == ChartSeriesKind.RangeArea; var color = Color(series, index, colors);
        var sourcePaint = VisualChartPaint.Series(series, color);
        for (var item = 0; item < count; item++) {
            var low = series.RenderPoints[item * 2]; var high = series.RenderPoints[item * 2 + 1]; var x = map.X(low.X);
            var breakBefore = low.BreakBefore || high.BreakBefore;
            middle.Add(new ChartPoint(x, map.Y((low.Y + high.Y) / 2), breakBefore));
        }
        var offset = 0;
        foreach (var segment in ChartPointSegments.Split(upper)) {
            var lowSegment = lower.GetRange(offset, segment.Count);
            var highPath = ChartPathBuilder.FromPoints(segment, series.Interpolation, series.StepPosition);
            var lowPath = ChartPathBuilder.FromPoints(lowSegment, series.Interpolation, series.StepPosition);
            var highFlat = highPath.Flatten(12); var lowFlat = lowPath.Flatten(12);
            var polygon = ChartPairedBoundaryGeometry.Envelope(highFlat, lowFlat); var opacity = area ? context.Theme.AreaOpacity : ChartVisualPrimitives.RangeBandFillOpacity;
            var fill = ChartColorMath.WithOpacity(color, opacity);
            using (FillSegment(builder, offset, segment.Count)) {
                builder.Path(polygon, fill, role: area ? "range-area" : "range-band", close: true,
                    paint: VisualChartPaint.Fill(sourcePaint.WithOpacity(fill, opacity)));
                DrawPattern(builder, polygon, series.FillPattern, fill, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), area ? "range-area-pattern" : "range-band-pattern");
            }
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
            var low = series.RenderPoints[item * 2]; var high = series.RenderPoints[item * 2 + 1];
            var hasOverride = item < series.PointColors.Count && series.PointColors[item].HasValue || item < series.PointFillPatterns.Count && series.PointFillPatterns[item].HasValue;
            var showMarkers = VisualMarkerScene.Enabled(series, hasOverride || series.MarkerRadius.HasValue);
            var radius = showMarkers ? series.MarkerRadius ?? context.Theme.MarkerRadius : 0;
            var bounds = Extents(lower[item].X, lower[item].Y, upper[item].X, upper[item].Y, VisualMarkerScene.Extent(series, radius));
            var label = ResolveObservationLabel(chart, context, series, item, colors, format => format("lower", low.Y) + "–" + format("upper", high.Y));
            var values = new[] { ("x", low.X), ("lower", low.Y), ("upper", high.Y) };
            // Adapters use the captured native midpoint; averaging mapped boundaries is wrong on nonlinear axes.
            using (area
                ? ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label,
                    new Dictionary<string, string> { ["data-cfx-midline-y"] = Number(middle[item].Y) }, values)
                : ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label, values)) {
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

}

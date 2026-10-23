using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static bool IsPointSeries(ChartSeriesKind kind) => kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine
        or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.Scatter;

    private static double ResolveMarkerRadius(ChartSeries series, VisualRenderContext context) => VisualMarkerScene.Radius(series, context);

    /// <summary>Shows an otherwise degenerate isolated pair without adding a fictional connection or filled span.</summary>
    private static bool PairedObservationMarkersEnabled(ChartSeries series, int item) {
        var raw = item * 2;
        var startsRun = raw == 0 || series.RenderPoints[raw].BreakBefore || series.RenderPoints[raw + 1].BreakBefore;
        var endsRun = raw + 2 == series.RenderPoints.Count || series.RenderPoints[raw + 2].BreakBefore || series.RenderPoints[raw + 3].BreakBefore;
        var hasOverride = item < series.PointColors.Count && series.PointColors[item].HasValue
            || item < series.PointFillPatterns.Count && series.PointFillPatterns[item].HasValue;
        return VisualMarkerScene.Enabled(series, hasOverride || series.MarkerRadius.HasValue || startsRun && endsRun);
    }

    /// <summary>Contains finite-size marks by extending only automatic ends of the final pixel-space axes.</summary>
    private static bool ExpandMarkerRanges(Chart chart, VisualRenderContext context, ChartRect plot,
        ChartRange range, ChartRange? secondaryRange, ChartStackLayout stacks, ChartBubbleSizeScale? bubbleScale) {
        var x = new MarkerExtents(); var secondaryX = new MarkerExtents(); var primaryY = new MarkerExtents(); var secondaryY = new MarkerExtents();
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        // Horizontal categories are mapped linearly, including reference stroke footprints.
        var primaryAxis = horizontal && chart.Options.YAxis.Scale != ChartScaleKind.Time
            ? new ChartAxis { Minimum = chart.Options.YAxis.Minimum, Maximum = chart.Options.YAxis.Maximum } : chart.Options.YAxis;
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.Kind is ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot) {
                if (plot.Width <= 0 || plot.Height <= 0) continue;
                var secondary = series.ValueAxis == ChartAxisSide.Secondary;
                var valueAxis = ChartCartesianOrientation.ValueAxis(chart, series.ValueAxis);
                var map = horizontal ? ChartMapper.ForHorizontalBars(plot, secondary ? secondaryRange! : range, valueAxis, chart.Options.YAxis)
                    : new ChartMapper(plot, secondary ? secondaryRange! : range, chart.Options.XAxis, valueAxis);
                var intervalRadius = series.Kind == ChartSeriesKind.BoxPlot ? BoxPlotValuePaintRadius(series)
                    : RangeBarValuePaintRadius(chart, context, plot, map, index);
                IncludeIntervalExtents(horizontal ? secondary ? secondaryX : x : secondary ? secondaryY : primaryY, series, valueAxis, intervalRadius);
                continue;
            }
            if (series.Kind == ChartSeriesKind.Dumbbell) {
                var startMarker = series.ResolveDumbbellMarker(start: true); var endMarker = series.ResolveDumbbellMarker(start: false);
                var startExtent = VisualMarkerScene.Extent(series, VisualMarkerScene.Radius(series, context, startMarker), startMarker);
                var endExtent = VisualMarkerScene.Extent(series, VisualMarkerScene.Radius(series, context, endMarker), endMarker);
                for (var raw = 0; raw + 1 < series.RenderPoints.Count; raw += 2) {
                    var first = series.RenderPoints[raw]; var second = series.RenderPoints[raw + 1];
                    if (horizontal) {
                        Include(first.Y, first.X, startExtent, series.ValueAxis); Include(second.Y, second.X, endExtent, series.ValueAxis);
                    } else {
                        Include(first.X, first.Y, startExtent, series.ValueAxis); Include(second.X, second.Y, endExtent, series.ValueAxis);
                    }
                }
                continue;
            }
            if (horizontal && !ChartCartesianOrientation.IsHorizontalLine(chart, series) && !ChartCartesianOrientation.IsHorizontalComparison(chart, series)) continue;
            var pointSeries = IsPointSeries(series.Kind);
            if (!pointSeries && series.Kind is not (ChartSeriesKind.Bubble or ChartSeriesKind.ErrorBar
                or ChartSeriesKind.Dumbbell or ChartSeriesKind.Lollipop or ChartSeriesKind.Slope
                or ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.DifferenceArea)) continue;
            if (series.Kind == ChartSeriesKind.ErrorBar) {
                var secondary = series.ValueAxis == ChartAxisSide.Secondary;
                IncludeErrorBarPaintExtents(series, context, plot, chart.Options.XAxis,
                    ChartCartesianOrientation.ValueAxis(chart, series.ValueAxis), x, secondary ? secondaryY : primaryY);
                continue;
            }
            if (series.Markers.Enabled == false) continue;
            var radius = ResolveMarkerRadius(series, context);
            var count = series.RenderPoints.Count / ObservationStride(series.Kind);
            for (var item = 0; item < count; item++) {
                if (pointSeries && !ShowMarker(chart, series, item)) continue;
                if (series.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.DifferenceArea) {
                    if (!PairedObservationMarkersEnabled(series, item)) continue;
                }
                var raw = item * ObservationStride(series.Kind);
                var point = series.RenderPoints[raw];
                var markerRadius = series.Kind == ChartSeriesKind.Bubble
                    ? bubbleScale!.Radius(series.RenderPoints[raw + 1].Y, plot, series.Markers) : radius;
                markerRadius = VisualMarkerScene.Extent(series, markerRadius);
                if (markerRadius <= 0) continue;
                var value = series.Kind == ChartSeriesKind.StackedArea ? stacks.Point(index, raw).End : point.Y;
                if (ChartCartesianOrientation.IsHorizontal(chart, series)) Include(value, point.X, markerRadius, series.ValueAxis);
                else Include(point.X, value, markerRadius, series.ValueAxis);
                if (series.Kind is ChartSeriesKind.Dumbbell or ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.DifferenceArea)
                    Include(point.X, series.RenderPoints[raw + 1].Y, markerRadius, series.ValueAxis);
            }
        }
        // References and markers share one fit: extending one side must preserve the other's pixel gutter.
        IncludeReferenceExtents(chart, context, x, primaryY, primaryAxis);
        var changed = ExpandMarkerAxis(chart.Options.XAxis, x, plot.Width, range.MinX, range.MaxX, out var minX, out var maxX);
        range.SetXBounds(minX, maxX);
        changed |= ExpandMarkerAxis(primaryAxis, primaryY, plot.Height, range.MinY, range.MaxY, out var minY, out var maxY);
        range.SetYBounds(minY, maxY);
        if (secondaryRange != null) {
            if (horizontal) {
                changed |= ExpandMarkerAxis(chart.Options.SecondaryXAxis, secondaryX, plot.Width,
                    secondaryRange.MinX, secondaryRange.MaxX, out minX, out maxX);
                secondaryRange.SetXBounds(minX, maxX); secondaryRange.SetYBounds(minY, maxY);
            } else {
                secondaryRange.SetXBounds(minX, maxX);
                changed |= ExpandMarkerAxis(chart.Options.SecondaryYAxis, secondaryY, plot.Height,
                    secondaryRange.MinY, secondaryRange.MaxY, out minY, out maxY);
                secondaryRange.SetYBounds(minY, maxY);
            }
        }
        return changed;

        void Include(double markerX, double markerY, double markerRadius, ChartAxisSide side) {
            var secondary = side == ChartAxisSide.Secondary;
            (horizontal && secondary ? secondaryX : x).Include(markerX, markerRadius,
                horizontal ? ChartCartesianOrientation.ValueAxis(chart, side) : chart.Options.XAxis);
            (horizontal || !secondary ? primaryY : secondaryY).Include(markerY, markerRadius,
                horizontal ? primaryAxis : ChartCartesianOrientation.ValueAxis(chart, side));
        }
    }

    private static void IncludeIntervalExtents(MarkerExtents extents, ChartSeries series, ChartAxis valueAxis, double radius) {
        foreach (var point in series.RenderPoints) extents.Include(point.Y, radius, valueAxis);
        foreach (var distribution in series.BoxPlotDistributions) {
            extents.Include(distribution.SourceMinimum, radius, valueAxis); extents.Include(distribution.SourceMaximum, radius, valueAxis);
        }
    }

    private sealed class MarkerExtents {
        internal VisualAxisPaintBounds Bounds { get; } = new VisualAxisPaintBounds();
        internal void Include(double value, double radius, ChartAxis axis) {
            Bounds.Include(ChartScaleTransform.Forward(value, axis), radius);
        }
    }

    private static bool ExpandMarkerAxis(ChartAxis axis, MarkerExtents markers, double pixels,
        double originalMinimum, double originalMaximum, out double minimum, out double maximum) {
        minimum = originalMinimum; maximum = originalMaximum;
        var min = ChartScaleTransform.Bound(minimum, axis, false); var max = ChartScaleTransform.Bound(maximum, axis, true);
        if (!markers.Bounds.TryExpand(min, max, pixels, axis.Minimum.HasValue, axis.Maximum.HasValue, out min, out max)) return false;
        var paddedMinimum = ChartScaleTransform.InverseBound(min, axis, false); var paddedMaximum = ChartScaleTransform.InverseBound(max, axis, true);
        if (!ChartMath.IsFinite(paddedMinimum) || !ChartMath.IsFinite(paddedMaximum)
            || axis.Scale == ChartScaleKind.Logarithmic && (!axis.IsInLogarithmicDomain(paddedMinimum) || !axis.IsInLogarithmicDomain(paddedMaximum))) return false;
        minimum = axis.Minimum.HasValue ? originalMinimum : Math.Min(originalMinimum, paddedMinimum);
        maximum = axis.Maximum.HasValue ? originalMaximum : Math.Max(originalMaximum, paddedMaximum);
        return minimum < originalMinimum || maximum > originalMaximum;
    }
}

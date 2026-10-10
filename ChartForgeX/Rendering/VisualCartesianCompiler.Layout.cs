using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static bool IsPointSeries(ChartSeriesKind kind) => kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine
        or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.Scatter;

    private static double ResolveMarkerRadius(ChartSeries series, VisualRenderContext context) => VisualMarkerScene.Radius(series, context);

    /// <summary>Contains finite-size marks by extending only automatic ends of the final pixel-space axes.</summary>
    private static bool ExpandMarkerRanges(Chart chart, VisualRenderContext context, ChartRect plot,
        ChartRange range, ChartRange? secondaryRange, ChartStackLayout stacks, ChartBubbleSizeScale? bubbleScale) {
        var x = new MarkerExtents(); var primaryY = new MarkerExtents(); var secondaryY = new MarkerExtents();
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            var pointSeries = IsPointSeries(series.Kind);
            if (!pointSeries && series.Kind is not (ChartSeriesKind.Bubble or ChartSeriesKind.ErrorBar
                or ChartSeriesKind.Dumbbell or ChartSeriesKind.Lollipop or ChartSeriesKind.Slope
                or ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea)) continue;
            if (series.Markers.Enabled == false) continue;
            var radius = ResolveMarkerRadius(series, context);
            var count = series.Points.Count / ObservationStride(series.Kind);
            for (var item = 0; item < count; item++) {
                if (pointSeries && !ShowMarker(chart, series, item)) continue;
                if (series.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea) {
                    var hasOverride = item < series.PointColors.Count && series.PointColors[item].HasValue
                        || item < series.PointFillPatterns.Count && series.PointFillPatterns[item].HasValue;
                    if (!VisualMarkerScene.Enabled(series, hasOverride || series.MarkerRadius.HasValue)) continue;
                }
                var raw = item * ObservationStride(series.Kind);
                var point = series.Points[raw];
                var markerRadius = series.Kind == ChartSeriesKind.Bubble
                    ? bubbleScale!.Radius(series.Points[raw + 1].Y, plot) : radius;
                markerRadius = VisualMarkerScene.Extent(series, markerRadius);
                if (markerRadius <= 0) continue;
                var value = series.Kind == ChartSeriesKind.StackedArea ? stacks.Point(index, raw).End : point.Y;
                Include(point.X, value, markerRadius, series.YAxis);
                if (series.Kind is ChartSeriesKind.Dumbbell or ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea)
                    Include(point.X, series.Points[raw + 1].Y, markerRadius, series.YAxis);
            }
        }
        // References and markers share one fit: extending one side must preserve the other's pixel gutter.
        IncludeReferenceExtents(chart, context, x, primaryY, chart.Options.YAxis);
        var changed = ExpandMarkerAxis(chart.Options.XAxis, x, plot.Width, range.MinX, range.MaxX, out var minX, out var maxX);
        range.SetXBounds(minX, maxX);
        changed |= ExpandMarkerAxis(chart.Options.YAxis, primaryY, plot.Height, range.MinY, range.MaxY, out var minY, out var maxY);
        range.SetYBounds(minY, maxY);
        if (secondaryRange != null) {
            secondaryRange.SetXBounds(minX, maxX);
            changed |= ExpandMarkerAxis(chart.Options.SecondaryYAxis, secondaryY, plot.Height,
                secondaryRange.MinY, secondaryRange.MaxY, out minY, out maxY);
            secondaryRange.SetYBounds(minY, maxY);
        }
        return changed;

        void Include(double markerX, double markerY, double markerRadius, ChartAxisSide side) {
            x.Include(markerX, markerRadius, chart.Options.XAxis);
            var axis = side == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            (side == ChartAxisSide.Secondary ? secondaryY : primaryY).Include(markerY, markerRadius, axis);
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
        var min = ChartScaleTransform.Forward(minimum, axis); var max = ChartScaleTransform.Forward(maximum, axis);
        if (!markers.Bounds.TryExpand(min, max, pixels, axis.Minimum.HasValue, axis.Maximum.HasValue, out min, out max)) return false;
        var paddedMinimum = ChartScaleTransform.Inverse(min, axis); var paddedMaximum = ChartScaleTransform.Inverse(max, axis);
        if (!ChartMath.IsFinite(paddedMinimum) || !ChartMath.IsFinite(paddedMaximum)
            || axis.Scale == ChartScaleKind.Logarithmic && paddedMinimum <= 0) return false;
        minimum = axis.Minimum.HasValue ? originalMinimum : Math.Min(originalMinimum, paddedMinimum);
        maximum = axis.Maximum.HasValue ? originalMaximum : Math.Max(originalMaximum, paddedMaximum);
        return minimum < originalMinimum || maximum > originalMaximum;
    }
}

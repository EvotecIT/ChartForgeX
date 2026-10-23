using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartRange {
    /// <summary>Fits one value-axis slot from existing full-data geometry without filtering or mutating source observations.</summary>
    internal void FitVisibleValueDomain(Chart chart, ChartStackLayout stacks, ChartBarCoordinateMap coordinates,
        ChartAxisSide side, ChartAxisWindow window) {
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        var axis = ChartCartesianOrientation.ValueAxis(chart, side);
        var categoryAxis = ChartCartesianOrientation.CategoryAxis(chart);
        if (horizontal && categoryAxis.Scale != ChartScaleKind.Time) categoryAxis = new ChartAxis { PreparedViewportWindow = categoryAxis.PreparedViewportWindow };
        var fullMin = horizontal ? MinX : MinY; var fullMax = horizontal ? MaxX : MaxY;
        var minimum = double.PositiveInfinity; var maximum = double.NegativeInfinity; var baseline = false;
        var categories = horizontal ? stacks.HorizontalCategories.ToArray() : coordinates.Categories.Select(value => value.Value).ToArray();
        var categoryHalfSpan = CategoryPadding(categories.ToList());
        if (categoryHalfSpan == 0) categoryHalfSpan = .5;
        var transformedMin = ChartScaleTransform.Bound(fullMin, axis, false); var transformedMax = ChartScaleTransform.Bound(fullMax, axis, true);
        var categoryStart = ChartScaleTransform.Bound(window.Minimum, categoryAxis, false);
        var categoryEnd = ChartScaleTransform.Bound(window.Maximum, categoryAxis, true, window.MaximumInclusive);
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.ValueAxis != side) continue;
            var hasBaseline = series.Kind == ChartSeriesKind.Waterfall || ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)
                || ChartSeriesKindTraits.UsesHorizontalBaseline(series.Kind);
            if (series.Kind == ChartSeriesKind.Waterfall) {
                foreach (var step in ChartWaterfallSteps.Create(series)) if (Overlaps(step.X - (categoryAxis.PreparedTimeDomain?.Unit == true ? 0 : categoryHalfSpan), step.X + (categoryAxis.PreparedTimeDomain?.Unit == true ? 0 : categoryHalfSpan))) {
                    Include(step.Start); Include(step.End); baseline = true;
                }
                continue;
            }
            if (IsContinuous(series.Kind)) {
                var anyVisible = false;
                var stride = series.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.DifferenceArea ? 2 : 1;
                for (var boundary = 0; boundary < stride; boundary++) {
                    var points = new List<ChartPoint>(); var lower = new List<ChartPoint>();
                    for (var raw = 0; raw < series.RenderPoints.Count; raw += stride) {
                        var point = series.RenderPoints[raw + boundary];
                        var value = series.Kind == ChartSeriesKind.StackedArea ? stacks.Point(seriesIndex, raw).End : point.Y;
                        var missing = point.BreakBefore || stride == 2 && series.RenderPoints[raw + 1 - boundary].BreakBefore;
                        points.Add(Project(point.X, value, missing));
                        if (series.Kind == ChartSeriesKind.StackedArea) lower.Add(Project(point.X, stacks.Point(seriesIndex, raw).Base, missing));
                    }
                    anyVisible |= IncludePath(points, series); if (lower.Count > 0) anyVisible |= IncludePath(lower, series);
                }
                if (hasBaseline && anyVisible) baseline = true;
                continue;
            }
            var observationStride = series.Kind == ChartSeriesKind.Bubble ? 2
                : series.Kind == ChartSeriesKind.ErrorBar ? 3
                : series.Kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc ? 4
                : series.Kind == ChartSeriesKind.BoxPlot ? 5
                : series.Kind is ChartSeriesKind.RangeBar or ChartSeriesKind.Dumbbell ? 2 : 1;
            for (var raw = 0; raw + observationStride <= series.RenderPoints.Count; raw += observationStride) {
                var point = series.RenderPoints[raw]; var item = raw / observationStride;
                var category = series.Kind == ChartSeriesKind.Bar ? coordinates.Resolve(seriesIndex, raw).Value : point.X;
                var first = category; var last = category;
                if (series.Kind == ChartSeriesKind.ErrorBar) { first = series.RenderPoints[raw + 1].X; last = series.RenderPoints[raw + 2].X; }
                else if (series.Kind == ChartSeriesKind.Bar && coordinates.TryResolveHistogramSlot(seriesIndex, raw, out var bins, out var bin)) {
                    first = bins.GetLowerBound(bin); last = bins.GetUpperBound(bin);
                } else if (series.Kind is ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar or ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot
                    or ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc) {
                    if (categoryAxis.PreparedTimeDomain?.Unit != true) { first -= categoryHalfSpan; last += categoryHalfSpan; }
                }
                if (!Overlaps(first, last)) continue;
                if (series.Kind is ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar) {
                    var stack = stacks.Point(seriesIndex, raw); Include(stack.Base); Include(stack.End);
                } else {
                    var count = series.Kind == ChartSeriesKind.Bubble ? 1 : observationStride;
                    for (var bound = 0; bound < count; bound++) Include(series.RenderPoints[raw + bound].Y);
                    if (series.Kind == ChartSeriesKind.BoxPlot && item < series.BoxPlotDistributions.Count) {
                        Include(series.BoxPlotDistributions[item].SourceMinimum); Include(series.BoxPlotDistributions[item].SourceMaximum);
                    }
                }
                baseline |= hasBaseline;
            }
        }
        if (double.IsInfinity(minimum)) return; // An empty visible category window retains this slot's complete resolved domain.
        if (baseline && UsesZeroBaseline(axis)) { minimum = Math.Min(minimum, 0); maximum = Math.Max(maximum, 0); }
        if (minimum == maximum) (minimum, maximum) = ExpandedSingleton(minimum, axis);
        if (axis.Scale != ChartScaleKind.Logarithmic) {
            var padding = (maximum - minimum) * .08;
            if (ChartMath.IsFinite(padding)) {
                if (!baseline || minimum < 0 || axis.Scale == ChartScaleKind.Time) minimum -= padding;
                maximum += padding;
            }
        }
        minimum = Math.Max(fullMin, minimum); maximum = Math.Min(fullMax, maximum);
        if (!ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum) || maximum <= minimum) return;
        if (horizontal) SetXBounds(minimum, maximum); else SetYBounds(minimum, maximum);
        ApplyValueBounds(axis, horizontal);

        bool Overlaps(double first, double last) => categoryAxis.PreparedTimeDomain?.Unit == true
            ? Math.Max(ChartScaleTransform.Forward(first, categoryAxis), ChartScaleTransform.Forward(last, categoryAxis)) >= categoryStart
                && Math.Min(ChartScaleTransform.Forward(first, categoryAxis), ChartScaleTransform.Forward(last, categoryAxis)) < categoryEnd
            : window.Overlaps(first, last);
        void Include(double value) {
            if (axis.Scale == ChartScaleKind.Logarithmic && value == 0) return; // Stack zero is the selected logarithmic baseline, not an observation.
            minimum = Math.Min(minimum, value); maximum = Math.Max(maximum, value);
        }
        ChartPoint Project(double category, double value, bool missing) => new(
            ChartScaleTransform.Normalize(ChartScaleTransform.Forward(category, categoryAxis), categoryStart, categoryEnd, false),
            ChartScaleTransform.Normalize(ChartScaleTransform.Forward(axis.Scale == ChartScaleKind.Logarithmic && value == 0
                ? axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? fullMax : fullMin : value, axis), transformedMin, transformedMax, false), missing);
        bool IncludePath(IReadOnlyList<ChartPoint> points, ChartSeries series) {
            var bounds = ChartPathBuilder.FromPoints(points, series.Interpolation, series.StepPosition).BoundsInXRange(0, 1);
            if (bounds == null) return false;
            Include(ChartScaleTransform.Inverse(ChartMath.InterpolateOrExtrapolateRange(transformedMin, transformedMax, bounds.Value.Top), axis));
            Include(ChartScaleTransform.Inverse(ChartMath.InterpolateOrExtrapolateRange(transformedMin, transformedMax, bounds.Value.Bottom), axis));
            return true;
        }
    }

    private static bool IsContinuous(ChartSeriesKind kind) => kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine
        or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.RangeBand
        or ChartSeriesKind.RangeArea or ChartSeriesKind.DifferenceArea or ChartSeriesKind.Slope or ChartSeriesKind.TrendLine;
}

using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // Both ordinary preparation and an explicit X window use this one axis/layout owner.
    // The full pass resolves automatic marker padding; a window reuses its numeric Y bounds.
    private sealed class CartesianAxisLayout {
        internal ChartRect Plot { get; }
        internal ChartRect LabelBounds { get; }
        internal AxisLabelCache AxisLabels { get; }
        internal bool Horizontal { get; }
        internal IReadOnlyDictionary<double, string>? CategoryCaptions { get; }
        internal IReadOnlyList<HorizontalStackTotal> HorizontalTotals { get; }
        internal IReadOnlyList<VerticalStackCaption>? VerticalTotals { get; }

        internal CartesianAxisLayout(ChartRect plot, ChartRect labelBounds, AxisLabelCache axisLabels, bool horizontal,
            IReadOnlyDictionary<double, string>? categoryCaptions, IReadOnlyList<HorizontalStackTotal> horizontalTotals,
            IReadOnlyList<VerticalStackCaption>? verticalTotals) {
            Plot = plot; LabelBounds = labelBounds; AxisLabels = axisLabels; Horizontal = horizontal;
            CategoryCaptions = categoryCaptions; HorizontalTotals = horizontalTotals; VerticalTotals = verticalTotals;
        }
    }

    private static CartesianAxisLayout? ResolveAxisLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartRect viewport, bool measureAxes, ChartRange range, ChartRange? secondaryRange,
        ChartStackLayout stacks, ChartBarCoordinateMap coordinates, ChartBubbleSizeScale? bubbleScale, VisualThemeColors colors,
        ChartAxisValueFormatter.Cache? categoryLabels, CartesianLabels labels, bool expandMarkers) {
        var dataRange = range.Copy();
        var secondaryDataRange = secondaryRange?.Copy();
        categoryLabels ??= new ChartAxisValueFormatter.Cache();
        var axisLabels = new AxisLabelCache(categoryLabels);
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        if (!horizontal) {
            axisLabels.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
            if (secondaryRange != null) axisLabels.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
        }
        IReadOnlyDictionary<double, string>? categoryCaptions = null;
        if (chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.Waterfall) {
            categoryCaptions = ChartWaterfallCategoryLabels.Resolve(chart, categoryLabels);
        } else if (chart.Series.Any(series => series.Kind is ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot)) {
            categoryCaptions = ChartCartesianCategoryLabels.Resolve(chart,
                chart.Series.Where(series => series.Kind is ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot).SelectMany(series => series.RenderPoints.Select(point => point.X)), categoryLabels);
        }
        if (categoryCaptions != null) {
            var categoryAxis = horizontal ? HorizontalAxes(chart, axisLabels).Category : chart.Options.XAxis;
            if (horizontal || chart.Series.All(series => series.Kind is ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot or ChartSeriesKind.Waterfall))
                axisLabels.SetTicks(categoryAxis, categoryCaptions.Keys.OrderBy(value => value).ToArray());
            foreach (var category in categoryCaptions) axisLabels.Set(categoryAxis, category.Key, category.Value);
        }
        RefreshCategoryDisplay(chart, context, builder, viewport, range, colors, axisLabels);
        if (measureAxes) plot = horizontal ? MeasureHorizontalPlot(chart, context, builder, viewport, range, colors, axisLabels)
            : MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, axisLabels);
        if (expandMarkers && ExpandMarkerRanges(chart, context, plot, range, secondaryRange, stacks, bubbleScale)) {
            if (!horizontal) {
                axisLabels.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
                if (secondaryRange != null) axisLabels.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
            }
            RefreshCategoryDisplay(chart, context, builder, plot, range, colors, axisLabels);
            if (measureAxes) plot = horizontal ? MeasureHorizontalPlot(chart, context, builder, viewport, range, colors, axisLabels)
                : MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, axisLabels);
        }
        if (plot.Width <= 0 || plot.Height <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.insufficient-space", "No plotting area remains after measuring the frame and axes."));
            return null;
        }
        var labelBounds = plot;
        var horizontalTotals = horizontal && chart.Options.ShowStackTotals && stacks.HasBarStacks
            ? ResolveHorizontalTotals(chart, context, builder, colors, stacks) : Array.Empty<HorizontalStackTotal>();
        var verticalTotals = !horizontal && chart.Options.ShowStackTotals && stacks.HasBarStacks
            ? ResolveVerticalTotals(chart, context, builder, colors, stacks) : null;
        // A coarser step can introduce wider captions than the original tick set.
        // Keep the same callback cache and only enlarge measured gutters while the
        // selected scale, mark padding and caption frame settle together.
        const int maximumAxisPasses = 8;
        for (var pass = 0; pass <= maximumAxisPasses; pass++) {
            plot = labelBounds;
            if (horizontalTotals.Count > 0) plot = ReserveHorizontalTotalGutters(chart, plot, horizontalTotals, context.Theme.Spacing);
            if (verticalTotals != null && verticalTotals.Count > 0) plot = ReserveVerticalTotalGutters(chart, plot, verticalTotals, context.Theme.Spacing);
            if (!horizontal) plot = ReserveVerticalBarLabelGutters(chart, context, builder, plot, range, secondaryRange, stacks, coordinates, colors, labels);
            // Always refit from source bounds, never from the prior pass's padding.
            if (expandMarkers) {
                range.SetXBounds(dataRange.MinX, dataRange.MaxX);
                range.SetYBounds(dataRange.MinY, dataRange.MaxY);
                if (secondaryRange != null && secondaryDataRange != null) {
                    secondaryRange.SetXBounds(secondaryDataRange.MinX, secondaryDataRange.MaxX);
                    secondaryRange.SetYBounds(secondaryDataRange.MinY, secondaryDataRange.MaxY);
                }
                ExpandMarkerRanges(chart, context, plot, range, secondaryRange, stacks, bubbleScale);
                if (!horizontal) {
                    axisLabels.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
                    if (secondaryRange != null) axisLabels.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
                }
            }
            RefreshCategoryDisplay(chart, context, builder, plot, range, colors, axisLabels);
            FitCartesianTicks(chart, context, builder, plot, range, secondaryRange, colors, axisLabels, horizontal);
            if (!measureAxes || pass == maximumAxisPasses) break;
            var measured = horizontal ? MeasureHorizontalPlot(chart, context, builder, viewport, range, colors, axisLabels)
                : MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, axisLabels);
            var nextBounds = InsetAxisFrame(labelBounds, measured);
            if (SameAxisFrame(labelBounds, nextBounds)) break;
            // Pathological formatter-dependent ranges still have a bounded layout:
            // the last pass uses the existing maximum axis allocation, so any newly
            // selected caption within that budget already has a complete gutter.
            if (pass == maximumAxisPasses - 1) {
                nextBounds = InsetAxisFrame(nextBounds, new ChartRect(viewport.Left + viewport.Width * .35,
                    viewport.Top + viewport.Height * .25, viewport.Width * .3, viewport.Height * .3));
            }
            labelBounds = nextBounds;
        }
        return new CartesianAxisLayout(plot, labelBounds, axisLabels, horizontal, categoryCaptions, horizontalTotals, verticalTotals);
    }

    private static ChartRect InsetAxisFrame(ChartRect current, ChartRect measured) {
        var left = Math.Max(current.Left, measured.Left); var top = Math.Max(current.Top, measured.Top);
        return new ChartRect(left, top, Math.Max(0, Math.Min(current.Right, measured.Right) - left),
            Math.Max(0, Math.Min(current.Bottom, measured.Bottom) - top));
    }

    private static bool SameAxisFrame(ChartRect left, ChartRect right) => Math.Abs(left.Left - right.Left) < .0001
        && Math.Abs(left.Top - right.Top) < .0001 && Math.Abs(left.Right - right.Right) < .0001 && Math.Abs(left.Bottom - right.Bottom) < .0001;

    internal static bool SupportsXViewport(Chart chart) => chart.Series.Count > 0 && !chart.Options.IsSparkline
        && !ChartCartesianOrientation.IsHorizontal(chart)
        && (chart.Options.XAxis.Scale is ChartScaleKind.Linear or ChartScaleKind.Time)
        && chart.Series.All(series => IsPointSeries(series.Kind) || series.Kind == ChartSeriesKind.Bubble);

    internal static void ValidateXViewport(Chart chart) {
        if (!SupportsXViewport(chart)) throw new NotSupportedException("An X viewport requires ordinary vertical line, area, scatter or bubble series with a linear or time X axis.");
        if (!chart.Series.Any(series => series.HasSourceData)) throw new InvalidOperationException("An X viewport requires source observations.");
    }
}

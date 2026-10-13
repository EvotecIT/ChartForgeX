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
        ChartStackLayout stacks, ChartBubbleSizeScale? bubbleScale, VisualThemeColors colors,
        ChartAxisValueFormatter.Cache? categoryLabels, bool expandMarkers) {
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
        if (measureAxes) plot = horizontal ? MeasureHorizontalPlot(chart, context, builder, viewport, range, colors, axisLabels)
            : MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, axisLabels);
        if (expandMarkers && ExpandMarkerRanges(chart, context, plot, range, secondaryRange, stacks, bubbleScale)) {
            if (!horizontal) {
                axisLabels.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
                if (secondaryRange != null) axisLabels.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
            }
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
        if (horizontalTotals.Count > 0) plot = ReserveHorizontalTotalGutters(chart, plot, horizontalTotals, context.Theme.Spacing);
        if (verticalTotals != null && verticalTotals.Count > 0) plot = ReserveVerticalTotalGutters(chart, plot, verticalTotals, context.Theme.Spacing);
        // Refit from the original bounds: a guide may no longer fit after total lanes.
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
        return new CartesianAxisLayout(plot, labelBounds, axisLabels, horizontal, categoryCaptions, horizontalTotals, verticalTotals);
    }

    internal static bool SupportsXViewport(Chart chart) => chart.Series.Count > 0 && !chart.Options.IsSparkline
        && !ChartCartesianOrientation.IsHorizontal(chart)
        && chart.Options.XAxis.Scale == ChartScaleKind.Linear
        && chart.Series.All(series => (series.Kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine)
            && (series.Interpolation is ChartInterpolation.Linear or ChartInterpolation.Step));

    internal static void ValidateXViewport(Chart chart) {
        if (!SupportsXViewport(chart)) throw new NotSupportedException("An X viewport requires ordinary vertical Line or StepLine series with linear X and linear or step interpolation.");
        if (!chart.Series.Any(series => series.HasSourceData)) throw new InvalidOperationException("An X viewport requires source observations.");
    }
}

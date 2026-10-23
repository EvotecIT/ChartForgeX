using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Owns physical axis routing for Cartesian families whose source X remains a category coordinate.</summary>
internal static class ChartCartesianOrientation {
    internal static bool IsHorizontal(Chart chart) => chart.Series.Any(series => IsHorizontal(chart, series));

    internal static bool IsHorizontal(Chart chart, ChartSeries series) => series.Kind == ChartSeriesKind.HorizontalBar
        || series.Kind == ChartSeriesKind.Waterfall && chart.Options.Waterfall.Orientation == ChartOrientation.Horizontal
        || series.Kind == ChartSeriesKind.RangeBar && chart.Options.RangeBar.Orientation == ChartOrientation.Horizontal
        || series.Kind == ChartSeriesKind.BoxPlot && chart.Options.BoxPlot.Orientation == ChartOrientation.Horizontal
        || IsHorizontalComparison(chart, series)
        || IsHorizontalLine(chart, series);

    internal static bool IsHorizontalComparison(Chart chart, ChartSeries series) =>
        series.Kind == ChartSeriesKind.Lollipop && chart.Options.LollipopOrientation == ChartOrientation.Horizontal
        || series.Kind == ChartSeriesKind.Dumbbell && chart.Options.DumbbellOrientation == ChartOrientation.Horizontal;

    internal static bool IsHorizontalLine(Chart chart, ChartSeries series) =>
        (series.Kind == ChartSeriesKind.Line || series.Kind == ChartSeriesKind.StepLine)
        && chart.Options.LineOrientation == ChartOrientation.Horizontal;

    internal static ChartAxis ValueAxis(Chart chart, ChartAxisSide side) => IsHorizontal(chart)
        ? side == ChartAxisSide.Secondary ? chart.Options.SecondaryXAxis : chart.Options.XAxis
        : side == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;

    internal static ChartAxis CategoryAxis(Chart chart) => IsHorizontal(chart) ? chart.Options.YAxis : chart.Options.XAxis;

    internal static bool UsesLegacyCategoryLabels(Chart chart) {
        if (!IsHorizontal(chart)) return false;
        var hasObservations = chart.Series.Any(series => series.RenderPoints.Count > 0);
        return !chart.Series.Any(series => IsHorizontalLine(chart, series) && (series.RenderPoints.Count > 0 || !hasObservations));
    }

    internal static IReadOnlyList<ChartAxisLabel> CategoryLabels(Chart chart) => UsesLegacyCategoryLabels(chart) && chart.Options.YAxis.Labels.Count == 0
        ? chart.Options.XAxis.Labels : CategoryAxis(chart).Labels;

    internal static ChartAnnotationKind AnnotationKind(ChartAnnotation annotation, bool horizontalValues) =>
        annotation.FollowsSeriesValueAxis && horizontalValues
            ? annotation.EndValue.HasValue ? ChartAnnotationKind.VerticalBand : ChartAnnotationKind.VerticalLine
            : annotation.Kind;

    internal static IEnumerable<double> Categories(ChartSeries series) => series.Kind == ChartSeriesKind.Waterfall
        ? ChartWaterfallSteps.Create(series).Select(step => step.X) : series.RenderPoints.Select(point => point.X);
}

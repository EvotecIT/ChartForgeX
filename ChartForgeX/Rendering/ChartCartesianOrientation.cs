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
        || IsHorizontalLine(chart, series);

    internal static bool IsHorizontalLine(Chart chart, ChartSeries series) =>
        (series.Kind == ChartSeriesKind.Line || series.Kind == ChartSeriesKind.StepLine)
        && chart.Options.LineOrientation == ChartOrientation.Horizontal;

    internal static ChartAxis CategoryAxis(Chart chart) => IsHorizontal(chart) ? chart.Options.YAxis : chart.Options.XAxis;

    internal static bool UsesLegacyCategoryLabels(Chart chart) => IsHorizontal(chart)
        && !chart.Series.Any(series => IsHorizontalLine(chart, series));

    internal static IReadOnlyList<ChartAxisLabel> CategoryLabels(Chart chart) => UsesLegacyCategoryLabels(chart) && chart.Options.YAxis.Labels.Count == 0
        ? chart.Options.XAxis.Labels : CategoryAxis(chart).Labels;

    internal static ChartAnnotationKind AnnotationKind(ChartAnnotation annotation, bool horizontalValues) =>
        annotation.FollowsSeriesValueAxis && horizontalValues
            ? annotation.EndValue.HasValue ? ChartAnnotationKind.VerticalBand : ChartAnnotationKind.VerticalLine
            : annotation.Kind;

    internal static IEnumerable<double> Categories(ChartSeries series) => series.Kind == ChartSeriesKind.Waterfall
        ? ChartWaterfallSteps.Create(series).Select(step => step.X) : series.Points.Select(point => point.X);
}

using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Owns physical axis routing for Cartesian families whose source X remains a category coordinate.</summary>
internal static class ChartCartesianOrientation {
    internal static bool IsHorizontal(Chart chart) => chart.Series.Any(series => IsHorizontal(chart, series));

    internal static bool IsHorizontal(Chart chart, ChartSeries series) => series.Kind == ChartSeriesKind.HorizontalBar
        || series.Kind == ChartSeriesKind.Waterfall && chart.Options.Waterfall.Orientation == ChartOrientation.Horizontal;

    internal static ChartAxis CategoryAxis(Chart chart) => IsHorizontal(chart) ? chart.Options.YAxis : chart.Options.XAxis;

    internal static IReadOnlyList<ChartAxisLabel> CategoryLabels(Chart chart) => IsHorizontal(chart) && chart.Options.YAxis.Labels.Count == 0
        ? chart.Options.XAxis.Labels : CategoryAxis(chart).Labels;

    internal static IEnumerable<double> Categories(ChartSeries series) => series.Kind == ChartSeriesKind.Waterfall
        ? ChartWaterfallSteps.Create(series).Select(step => step.X) : series.Points.Select(point => point.X);
}

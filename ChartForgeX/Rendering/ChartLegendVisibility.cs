using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Shared entry-count policy for series, slice, point and categorical legends.</summary>
internal static class ChartLegendVisibility {
    public static bool ForEntries(Chart chart, int count) =>
        chart.Options.ShowLegend && count > 0 && (count > 1 || chart.Options.HasExplicitLegend);

    public static bool ForSeries(Chart chart) {
        var count = 0;
        foreach (var series in chart.Series) if (series.ShowInLegend) count++;
        if (count == 1 && chart.Series.Count == 1 && chart.Options.ShowPointLegend
            && ChartSeriesKindTraits.SupportsPointLegend(chart.Series[0].Kind)) {
            var series = chart.Series[0];
            var tupleSize = series.Kind == ChartSeriesKind.BoxPlot ? 5
                : series.Kind == ChartSeriesKind.Candlestick || series.Kind == ChartSeriesKind.Ohlc ? 4
                : series.Kind == ChartSeriesKind.ErrorBar ? 3
                : series.Kind == ChartSeriesKind.Bubble || series.Kind == ChartSeriesKind.RangeBand
                    || series.Kind == ChartSeriesKind.RangeArea || series.Kind == ChartSeriesKind.RangeBar
                    || series.Kind == ChartSeriesKind.Dumbbell ? 2 : 1;
            count = System.Math.Max(count, series.Points.Count / tupleSize);
        }
        return ForEntries(chart, count);
    }
}

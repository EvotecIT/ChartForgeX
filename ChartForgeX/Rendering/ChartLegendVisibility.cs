using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Shared entry-count policy for series, slice, point and categorical legends.</summary>
internal static class ChartLegendVisibility {
    /// <summary>Continuous scales carry information even when the family has no categorical legend entries.</summary>
    internal static bool ForPreparedContent(Chart chart, int entryCount) {
        if (!chart.Options.ShowLegend) return false;
        if (!chart.Options.HasExplicitLegend && chart.Series.Count > 0
            && chart.Series[0].Kind is ChartSeriesKind.Gauge or ChartSeriesKind.Bullet) return false;
        foreach (var series in chart.Series) {
            if (chart.Options.ShowHeatmapScale && (series.Kind == ChartSeriesKind.CalendarHeatmap
                || (series.Kind is ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap) && !series.IsCategoricalHeatmapRow)) return true;
            if (chart.Options.ShowMapScaleLegend && series.Kind is ChartSeriesKind.DottedMap or ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap) return true;
            if (series.Kind == ChartSeriesKind.Treemap && series.ShowInLegend && chart.Options.Treemap.ShowColorScaleLegend
                && (chart.Options.Treemap.ColorScale != null || System.Linq.Enumerable.Any(series.TreemapItems, item => item.ColorValue.HasValue))) return true;
        }
        return ForEntries(chart, entryCount);
    }

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

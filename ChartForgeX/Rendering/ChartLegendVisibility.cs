using ChartForgeX.Core;
using System.Linq;

namespace ChartForgeX.Rendering;

/// <summary>Shared entry-count policy for series, slice, point and categorical legends.</summary>
internal static class ChartLegendVisibility {
    /// <summary>Continuous scales carry information even when the family has no categorical legend entries.</summary>
    internal static bool ForPreparedContent(Chart chart, int entryCount, ChartColorPresentationSet? colorScales = null) {
        if (!chart.Options.ShowLegend) return false;
        if (!chart.Options.HasExplicitLegend && chart.Series.Count > 0
            && chart.Series[0].Kind is ChartSeriesKind.Gauge or ChartSeriesKind.Bullet) return false;
        colorScales ??= new ChartColorPresentationSet(chart);
        if (chart.Options.Bubble.SizeDimension.ShowLegend && System.Linq.Enumerable.Any(chart.Series,
            series => series.Kind == ChartSeriesKind.Bubble && series.AuthoredObservationCount > 0)) return true;
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble && series.ColorScale != null
                && colorScales[index]!.Presentation.ShowLegend && series.AuthoredObservationCount > 0) return true;
            if (series.Kind == ChartSeriesKind.DottedMap && series.ColorScale != null && colorScales.MapDataset(index).Presentation.ShowLegend) return true;
            if (series.Kind == ChartSeriesKind.DottedMap && series.MapPointSize.SizeDimension.ShowLegend && series.MapPoints.Any(point => point.Weight.HasValue)) return true;
            if (colorScales.Heatmap.ShowLegend && (series.Kind == ChartSeriesKind.CalendarHeatmap
                || (series.Kind is ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap) && !series.IsCategoricalHeatmapRow)) return true;
            if (series.Kind == ChartSeriesKind.RegionMap && colorScales.MapDataset(index).Presentation.ShowLegend) return true;
            if (colorScales.Map.ShowLegend && series.Kind == ChartSeriesKind.TileMap) return true;
            if (series.Kind == ChartSeriesKind.Sankey && colorScales.Sankey.ShowLegend
                && chart.Options.Sankey.FlowColorMode == ChartSankeyFlowColorMode.Quantitative) return true;
            if (series.Kind == ChartSeriesKind.Chord && colorScales.Chord.ShowLegend
                && (chart.Options.Chord.ColorScale != null || System.Linq.Enumerable.Any(series.FlowLinks, link => link.ColorValue.HasValue))) return true;
            if (series.Kind is ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst) {
                var sunburst = series.Kind == ChartSeriesKind.Sunburst;
                var showScale = colorScales.Hierarchy.ShowLegend;
                var scale = sunburst ? chart.Options.Sunburst.ColorScale : chart.Options.Treemap.ColorScale;
                if (showScale && (scale != null || System.Linq.Enumerable.Any(series.HierarchyItems, item => item.ColorValue.HasValue))) return true;
            }
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
            count = System.Math.Max(count, series.AuthoredObservationCount);
        }
        return ForEntries(chart, count);
    }
}

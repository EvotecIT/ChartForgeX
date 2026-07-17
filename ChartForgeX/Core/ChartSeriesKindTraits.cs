using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

internal static class ChartSeriesKindTraits {
    private static readonly ChartSeriesKind[] ExclusiveKinds = {
        ChartSeriesKind.Heatmap,
        ChartSeriesKind.HexbinHeatmap,
        ChartSeriesKind.CalendarHeatmap,
        ChartSeriesKind.DottedMap,
        ChartSeriesKind.TileMap,
        ChartSeriesKind.RegionMap,
        ChartSeriesKind.Gauge,
        ChartSeriesKind.Circle,
        ChartSeriesKind.RadialBar,
        ChartSeriesKind.LayeredRadial,
        ChartSeriesKind.Bullet,
        ChartSeriesKind.Waterfall,
        ChartSeriesKind.Radar,
        ChartSeriesKind.Polar,
        ChartSeriesKind.Funnel,
        ChartSeriesKind.Treemap,
        ChartSeriesKind.Timeline,
        ChartSeriesKind.Gantt,
        ChartSeriesKind.Sankey,
        ChartSeriesKind.Tree,
        ChartSeriesKind.Sunburst,
        ChartSeriesKind.Pictorial,
        ChartSeriesKind.ProgressBar,
        ChartSeriesKind.WordCloud,
        ChartSeriesKind.Pie,
        ChartSeriesKind.Donut,
        ChartSeriesKind.PolarArea
    };

    public static bool IsExclusive(ChartSeriesKind kind) => Array.IndexOf(ExclusiveKinds, kind) >= 0;

    public static bool SupportsPointSeriesMapping(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line ||
        kind == ChartSeriesKind.StepLine ||
        kind == ChartSeriesKind.Area ||
        kind == ChartSeriesKind.StepArea ||
        kind == ChartSeriesKind.StackedArea ||
        kind == ChartSeriesKind.Scatter ||
        kind == ChartSeriesKind.Bar ||
        kind == ChartSeriesKind.HorizontalBar ||
        kind == ChartSeriesKind.Lollipop;

    public static bool UsesVerticalBaseline(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Area ||
        kind == ChartSeriesKind.StepArea ||
        kind == ChartSeriesKind.StackedArea ||
        kind == ChartSeriesKind.Bar ||
        kind == ChartSeriesKind.Lollipop;

    public static bool UsesHorizontalBaseline(ChartSeriesKind kind) => kind == ChartSeriesKind.HorizontalBar;

    public static bool RequiresSingleSeries(ChartSeriesKind kind) {
        return kind == ChartSeriesKind.Gauge ||
            kind == ChartSeriesKind.CalendarHeatmap ||
            kind == ChartSeriesKind.DottedMap ||
            kind == ChartSeriesKind.TileMap ||
            kind == ChartSeriesKind.RegionMap ||
            kind == ChartSeriesKind.Circle ||
            kind == ChartSeriesKind.RadialBar ||
            kind == ChartSeriesKind.LayeredRadial ||
            kind == ChartSeriesKind.Waterfall ||
            kind == ChartSeriesKind.Funnel ||
            kind == ChartSeriesKind.Treemap ||
            kind == ChartSeriesKind.Sankey ||
            kind == ChartSeriesKind.Tree ||
            kind == ChartSeriesKind.Sunburst ||
            kind == ChartSeriesKind.Pictorial ||
            kind == ChartSeriesKind.ProgressBar ||
            kind == ChartSeriesKind.WordCloud ||
            kind == ChartSeriesKind.Pie ||
            kind == ChartSeriesKind.Donut ||
            kind == ChartSeriesKind.PolarArea;
    }

    public static bool RequiresPositiveValues(ChartSeriesKind kind) {
        return kind == ChartSeriesKind.Funnel ||
            kind == ChartSeriesKind.Treemap ||
            kind == ChartSeriesKind.Pie ||
            kind == ChartSeriesKind.Donut ||
            kind == ChartSeriesKind.Pictorial ||
            kind == ChartSeriesKind.ProgressBar ||
            kind == ChartSeriesKind.WordCloud ||
            kind == ChartSeriesKind.PolarArea;
    }

    public static bool UsesCartesianXAxis(Chart chart) {
        if (chart.Series.Count == 0) return true;
        foreach (var series in chart.Series) {
            if (series == null || IsExclusive(series.Kind)) return false;
        }

        return true;
    }

    public static bool UsesCartesianYAxis(ChartSeries series) =>
        series.Kind != ChartSeriesKind.HorizontalBar && !IsExclusive(series.Kind);

    public static bool IsMapKind(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.CalendarHeatmap ||
        kind == ChartSeriesKind.DottedMap ||
        kind == ChartSeriesKind.RegionMap ||
        kind == ChartSeriesKind.TileMap;

    public static bool IsSpatialMapKind(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.DottedMap ||
        kind == ChartSeriesKind.RegionMap ||
        kind == ChartSeriesKind.TileMap;

    public static bool IsPieLikeKind(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Pie || kind == ChartSeriesKind.Donut;

    public static bool IsLineLikeLegendKind(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line ||
        kind == ChartSeriesKind.StepLine ||
        kind == ChartSeriesKind.Area ||
        kind == ChartSeriesKind.StepArea ||
        kind == ChartSeriesKind.StackedArea ||
        kind == ChartSeriesKind.Slope ||
        kind == ChartSeriesKind.RangeBand ||
        kind == ChartSeriesKind.RangeArea ||
        kind == ChartSeriesKind.Lollipop ||
        kind == ChartSeriesKind.Dumbbell ||
        kind == ChartSeriesKind.ErrorBar ||
        kind == ChartSeriesKind.Radar ||
        kind == ChartSeriesKind.Polar ||
        kind == ChartSeriesKind.TrendLine;

    public static bool UsesOptionalLineMarker(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line ||
        kind == ChartSeriesKind.StepLine ||
        kind == ChartSeriesKind.Area ||
        kind == ChartSeriesKind.StepArea;

    public static bool ContainsKind(Chart chart, ChartSeriesKind kind) {
        foreach (var series in chart.Series) {
            if (series != null && series.Kind == kind) return true;
        }

        return false;
    }

    public static ChartSeries? FirstSeriesOrDefault(IReadOnlyList<ChartSeries> series, ChartSeriesKind kind) {
        for (var i = 0; i < series.Count; i++) {
            if (series[i] != null && series[i].Kind == kind) return series[i];
        }

        return null;
    }
}

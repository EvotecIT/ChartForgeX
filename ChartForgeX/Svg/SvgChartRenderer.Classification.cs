using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static string BuildDescription(Chart chart) => chart.Options.Labels.Describe(DescriptionFacts(chart));

    private static ChartDescriptionFacts DescriptionFacts(Chart chart) {
        var title = chart.Title;
        if (chart.Series.Count == 0) return new ChartDescriptionFacts(ChartDescriptionKind.NoSeries, title, Array.Empty<string>(), 0);
        var calendar = ChartSeriesKindTraits.FirstSeriesOrDefault(chart.Series, ChartSeriesKind.CalendarHeatmap);
        if (calendar != null && calendar.Points.Count > 0) {
            var minDate = calendar.Points.Min(point => DateTime.FromOADate(point.X).Date);
            var maxDate = calendar.Points.Max(point => DateTime.FromOADate(point.X).Date);
            return new ChartDescriptionFacts(ChartDescriptionKind.CalendarHeatmap, title, new[] { calendar.Name }, calendar.Points.Count, firstDate: minDate, lastDate: maxDate);
        }

        var dottedMap = ChartSeriesKindTraits.FirstSeriesOrDefault(chart.Series, ChartSeriesKind.DottedMap);
        if (dottedMap != null && dottedMap.Points.Count > 0) {
            return new ChartDescriptionFacts(ChartDescriptionKind.DottedMap, title, new[] { dottedMap.Name }, dottedMap.Points.Count);
        }

        var regionMap = ChartSeriesKindTraits.FirstSeriesOrDefault(chart.Series, ChartSeriesKind.RegionMap);
        if (regionMap != null && regionMap.Points.Count > 0) {
            var definition = chart.Options.RegionMapDefinition;
            var data = MapValues(chart, regionMap);
            var missing = definition == null ? 0 : Math.Max(0, definition.Regions.Count - data.Count);
            return new ChartDescriptionFacts(ChartDescriptionKind.RegionMap, title, new[] { regionMap.Name }, data.Count, missing, mapName: definition?.Name);
        }

        var tileMap = ChartSeriesKindTraits.FirstSeriesOrDefault(chart.Series, ChartSeriesKind.TileMap);
        if (tileMap != null && tileMap.Points.Count > 0) {
            var definition = chart.Options.TileMapDefinition;
            var data = MapValues(chart, tileMap);
            var missing = definition == null ? 0 : Math.Max(0, definition.Regions.Count - data.Count);
            return new ChartDescriptionFacts(ChartDescriptionKind.TileMap, title, new[] { tileMap.Name }, data.Count, missing, mapName: definition?.Name);
        }

        var describedSeries = chart.Series.Where(series => series.Points.Count > 0 && !IsPointCalloutSeries(series)).Select(series => series.Name).ToArray();
        if (describedSeries.Length == 0) return new ChartDescriptionFacts(ChartDescriptionKind.NoPoints, title, Array.Empty<string>(), 0);
        return new ChartDescriptionFacts(ChartDescriptionKind.Series, title, describedSeries, describedSeries.Length);
    }

    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && ChartSeriesKindTraits.IsPieLikeKind(chart.Series[0].Kind);

    private static bool IsHorizontalBarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.HorizontalBar);

    private static bool IsHeatmapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Heatmap);

    private static bool IsHexbinHeatmapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.HexbinHeatmap);

    private static bool IsGaugeChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Gauge);

    private static bool IsRadialBarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.RadialBar);

    private static bool IsBulletChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Bullet);

    private static bool IsWaterfallChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Waterfall);

    private static bool IsRadarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Radar);

    private static bool IsPolarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Polar);

    private static bool IsPolarAreaChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.PolarArea);

    private static bool IsFunnelChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Funnel);

    private static bool IsTimelineChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Timeline);

    private static bool IsGanttChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Gantt);

    private static bool IsSankeyChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Sankey);

    private static bool IsTreeChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Tree);

    private static bool IsSunburstChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Sunburst);

    private static bool IsPictorialChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Pictorial);

    private static bool IsProgressBarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.ProgressBar);

    private static bool IsWordCloudChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.WordCloud);

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static bool CanUsePointLegend(ChartSeries series) => ChartSeriesKindTraits.SupportsPointLegend(series.Kind) && VisualPointCount(series) > 1;
}

using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static string BuildDescription(Chart chart) {
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "Chart" : chart.Title;
        if (chart.Series.Count == 0) return title + " with no data series.";
        var calendar = chart.Series.FirstOrDefault(series => series.Kind == ChartSeriesKind.CalendarHeatmap);
        if (calendar != null && calendar.Points.Count > 0) {
            var minDate = calendar.Points.Min(point => DateTime.FromOADate(point.X).Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var maxDate = calendar.Points.Max(point => DateTime.FromOADate(point.X).Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return title + " calendar heatmap for " + calendar.Name + " from " + minDate + " to " + maxDate + " with " + calendar.Points.Count.ToString(CultureInfo.InvariantCulture) + " dated " + (calendar.Points.Count == 1 ? "value" : "values") + ".";
        }

        var dottedMap = chart.Series.FirstOrDefault(series => series.Kind == ChartSeriesKind.DottedMap);
        if (dottedMap != null && dottedMap.Points.Count > 0) {
            return title + " dotted world map for " + dottedMap.Name + " with " + dottedMap.Points.Count.ToString(CultureInfo.InvariantCulture) + " highlighted " + (dottedMap.Points.Count == 1 ? "point" : "points") + ".";
        }

        var regionMap = chart.Series.FirstOrDefault(series => series.Kind == ChartSeriesKind.RegionMap);
        if (regionMap != null && regionMap.Points.Count > 0) {
            var definition = chart.Options.RegionMapDefinition;
            var data = MapValues(chart, regionMap);
            var missing = definition == null ? 0 : Math.Max(0, definition.Regions.Count - data.Count);
            var mapName = definition == null ? "region" : definition.Name;
            return title + " region map for " + regionMap.Name + " on " + mapName + " with " + data.Count.ToString(CultureInfo.InvariantCulture) + " filled regions and " + missing.ToString(CultureInfo.InvariantCulture) + " missing regions.";
        }

        var tileMap = chart.Series.FirstOrDefault(series => series.Kind == ChartSeriesKind.TileMap);
        if (tileMap != null && tileMap.Points.Count > 0) {
            var definition = chart.Options.TileMapDefinition;
            var data = MapValues(chart, tileMap);
            var missing = definition == null ? 0 : Math.Max(0, definition.Regions.Count - data.Count);
            var mapName = definition == null ? "tile" : definition.Name;
            return title + " tile map for " + tileMap.Name + " on " + mapName + " with " + data.Count.ToString(CultureInfo.InvariantCulture) + " filled regions and " + missing.ToString(CultureInfo.InvariantCulture) + " missing regions.";
        }

        var describedSeries = chart.Series.Where(series => series.Points.Count > 0 && !IsPointCalloutSeries(series)).ToArray();
        if (describedSeries.Length == 0) return title + " with no data points.";
        var names = string.Join(", ", describedSeries.Select(series => series.Name).ToArray());
        return title + " with " + describedSeries.Length.ToString(CultureInfo.InvariantCulture) + " data series: " + names + ".";
    }

    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && (chart.Series[0].Kind == ChartSeriesKind.Pie || chart.Series[0].Kind == ChartSeriesKind.Donut);

    private static bool IsHorizontalBarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar);

    private static bool IsHeatmapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Heatmap);

    private static bool IsHexbinHeatmapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HexbinHeatmap);

    private static bool IsGaugeChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Gauge);

    private static bool IsRadialBarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.RadialBar);

    private static bool IsBulletChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Bullet);

    private static bool IsWaterfallChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Waterfall);

    private static bool IsRadarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Radar);

    private static bool IsPolarAreaChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.PolarArea);

    private static bool IsFunnelChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Funnel);

    private static bool IsTimelineChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Timeline);

    private static bool IsGanttChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Gantt);

    private static bool IsSankeyChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Sankey);

    private static bool IsTreeChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Tree);

    private static bool IsSunburstChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Sunburst);

    private static bool IsPictorialChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Pictorial);

    private static bool IsProgressBarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.ProgressBar);

    private static bool IsWordCloudChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.WordCloud);

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static bool CanUsePointLegend(ChartSeries series) => VisualPointCount(series) > 1;
}

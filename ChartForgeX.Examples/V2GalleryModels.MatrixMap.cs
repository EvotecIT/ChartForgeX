using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart? MatrixMap(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        var chart = Categories(); var colors = VisualTheme.Graphite().Resolve(mode);
        switch (kind) {
            case ChartSeriesKind.Heatmap:
                if (variant == "options") return States(mode).WithXLabels("Identity", "Network", "Storage", "Backup")
                    .AddHeatmapCategoryRow("Primary", new ChartHeatmapCell?[] { new("pass", "Ready", "Assessment passed"), new("warning", "Review"), null, new("pass", "Ready") }, "Production")
                    .AddHeatmapCategoryRow("Secondary", new ChartHeatmapCell?[] { new("pass"), new("pass"), new("warning"), new("pass") }, "Production");
                chart.AddHeatmapRow("Primary", variant == "sparse" ? new double?[] { 92, null, null, 70, null, 41 } : new double?[] { 92, 70, 56, null, 84, 41 })
                    .AddHeatmapRow("Secondary", variant == "sparse" ? new double?[] { null, 84, null, null, 90, null } : new double?[] { 76, 84, 46, 70, 90, 62 }); break;
            case ChartSeriesKind.HexbinHeatmap:
                chart.AddHexbinHeatmapRow("Primary", new double?[] { 92, 70, 56, null, 84, 41 }).AddHexbinHeatmapRow("Secondary", new double?[] { 76, 84, 46, 70, 90, 62 }); break;
            case ChartSeriesKind.CalendarHeatmap:
                return Chart.Create().AddCalendarHeatmap("Observations", Enumerable.Range(0, variant == "sparse" ? 35 : 120)
                    .Where(index => index % 7 != 0 && index % 9 != 0).Select(index => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1).AddDays(index), index * 7 % 13)), firstDayOfWeek: DayOfWeek.Monday);
            case ChartSeriesKind.DottedMap:
                return Chart.Create().WithDataLabels().WithMapViewport(ChartMapViewport.Europe()).AddDottedMap("Locations", new[] {
                    new ChartMapPoint("Madrid", -3.7, 40.4, 18), new ChartMapPoint("Warsaw", 21, 52.2, 74), new ChartMapPoint("Oslo", 10.8, 59.9, 42)
                }).AddMapRouteBetweenPoints("Madrid to Warsaw", "Madrid", "Warsaw")
                    .AddMapRoute("Warsaw to Oslo", new[] { new ChartMapPoint("Warsaw", 21, 52.2), new ChartMapPoint("Baltic", 17, 56), new ChartMapPoint("Oslo", 10.8, 59.9) });
            case ChartSeriesKind.TileMap:
                chart = Chart.Create().AddTileMap("Observations", ChartTileMapCatalog.Get("us-states"), RegionValues()).WithMapLabels(); break;
            case ChartSeriesKind.RegionMap:
                chart = Chart.Create().AddRegionMap("Observations", ChartMapCatalog.Get("us-states"), RegionValues()).WithMapLabels(false); break;
            default: return null;
        }
        if (kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap && variant == "options")
            chart.WithMapColorScale(ChartMapColorScale.Diverging(colors.Status.Critical.Fill, colors.Surface, colors.Status.Pass.Fill, 50)
                .WithValueRange(0, 100).WithLabels("Below target", "Target", "Above target").WithNoDataColor(colors.Border))
                .WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        if (kind is ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap)
            chart.WithHeatmapScale(ChartHeatmapScale.Sequential).WithHeatmapCellGap(3).WithHeatmapCellRadius(3).WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Auto);
        return chart;
    }
    private static ChartRegionMapItem[] RegionValues() => new[] {
        new ChartRegionMapItem("CA", 82), new ChartRegionMapItem("TX", 64), new ChartRegionMapItem("NY", 91), new ChartRegionMapItem("FL", 47),
        new ChartRegionMapItem("WA", 74), new ChartRegionMapItem("IL", 58), new ChartRegionMapItem("CO", 39), new ChartRegionMapItem("NC", 66) };
    private static Chart States(VisualThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        return Chart.Create().WithStateCategories(new ChartStateCategory("pass", "Available", colors.Status.Pass.Fill),
            new ChartStateCategory("warning", "Review", colors.Status.Medium.Fill, ChartStatePattern.Hatched),
            new ChartStateCategory("critical", "Unavailable", colors.Status.Critical.Fill, ChartStatePattern.CrossHatched));
    }
}

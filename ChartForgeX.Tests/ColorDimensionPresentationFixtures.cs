using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static class ColorDimensionPresentationFixtures {
    internal static Chart Create(ChartSeriesKind kind, string scaleKind = "positioned", int width = 800, bool dark = false) {
        var chart = Chart.Create().Size(width, width == 360 ? 400 : 440).DataLabels(false)
            .Title("Color measurement").Theme(VisualTheme.Graphite()).ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light)
            .ValueFormat(ChartValueFormat.Number("0.0 'units'"))
            .Labels(labels => { labels.NoData = "Not sampled"; labels.Color = "Default color"; });
        if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.RegionMap) {
            var values = new[] { new ChartRegionMapItem("CA", 7.125), new ChartRegionMapItem("TX", 0), new ChartRegionMapItem("NY", 12.5) };
            if (kind == ChartSeriesKind.TileMap) chart.AddTileMap("Samples", ChartTileMapCatalog.Get("us-states"), values, id: new ChartSeriesId("samples"));
            else chart.AddRegionMap("Samples", ChartMapCatalog.Get("us-states"), values, id: new ChartSeriesId("samples"));
            chart.Map(m => m.ShowLabels(false));
        } else if (kind == ChartSeriesKind.CalendarHeatmap) chart.AddCalendarHeatmap("Samples", new[] {
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 7.125), new ChartCalendarHeatmapItem(new DateTime(2026, 1, 7), 0),
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 8), 12.5)
        }, id: new ChartSeriesId("samples"));
        else if (kind == ChartSeriesKind.Treemap) chart.AddSeries(ChartSeries.CreateTreemap(new ChartSeriesId("samples"), "Samples", new[] {
            new ChartHierarchyItem("measured", "Measured", value: 9, colorValue: 7.125),
            new ChartHierarchyItem("zero", "Zero", value: 4, colorValue: 0),
            new ChartHierarchyItem("missing", "Missing", value: 2)
        }));
        else if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble) {
            var values = new double?[] { 7.125, 0, null }; var y = new[] { 18d, 30, 24 };
            if (kind == ChartSeriesKind.Scatter) chart.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("samples"), "Samples", values.Select((value, index) => new ChartPoint(index + 1, y[index]).WithColorValue(value))));
            else chart.AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("samples"), "Samples", values.Select((value, index) => new ChartBubble(index + 1, y[index], (index + 1) * (index + 1) * 9).WithColorValue(value))));
            chart.XAxis(x => x.Bounds(0, 4)).YAxis(yAxis => yAxis.Bounds(0, 40)).XAxis(x => x.Title("Batch")).YAxis(yAxis => yAxis.Title("Work (units)"));
        } else if (kind == ChartSeriesKind.HexbinHeatmap) chart.AddHexbinHeatmapRow("Samples", new double?[] { 7.125, 0, null, 12.5 }, id: new ChartSeriesId("samples"));
        else chart.AddHeatmapRow("Samples", new double?[] { 7.125, 0, null, 12.5 }, id: new ChartSeriesId("samples"));
        var low = ChartColor.FromHex("#2166AC"); var high = ChartColor.FromHex("#B2182B");
        var scale = scaleKind == "legacy" ? null : scaleKind == "discrete" ? ChartColorScale.Discrete(new[] {
            new ChartColorBand(5, low, "Cool"), new ChartColorBand(null, high, "Warm")
        }) : ChartColorScale.Sequential(new[] {
            new ChartColorStop(0, low), new ChartColorStop(8, ChartColor.FromHex("#D9A441")), new ChartColorStop(20, high)
        });
        if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.RegionMap) chart.Map(m => m.ColorScale(scale));
        else if (kind == ChartSeriesKind.Treemap) chart.Treemap(options => options.ColorScale = scale);
        else if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble) chart.Series[0].ColorScale(scale);
        else chart.Heatmap(h => h.ColorScale(scale));
        return chart;
    }

    internal static ChartColorDimensionOptions Options(Chart chart, ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.TileMap or ChartSeriesKind.RegionMap => chart.Options.Map.ColorDimension,
        ChartSeriesKind.Treemap => chart.Options.Treemap.ColorDimension,
        ChartSeriesKind.Scatter or ChartSeriesKind.Bubble => chart.Series[0].ColorDimension,
        _ => chart.Options.Heatmap.ColorDimension
    };
}

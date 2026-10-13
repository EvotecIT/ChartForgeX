using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart? ColorDimensionObservations(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        var gradient = variant is "gradient-color-legend" or "compact-gradient-color-legend";
        var swatches = variant is "swatch-color-legend" or "compact-swatch-color-legend";
        if (variant is not ("color-dimension" or "compact-color-dimension") && !gradient && !swatches) return null;
        var colors = VisualTheme.Graphite().Resolve(mode);
        var scale = PositionedTemperatureScale().WithNoDataColor(colors.Border);
        var format = ChartValueFormat.Number("0.0 '°C'", CultureInfo.GetCultureInfo("pl-PL"));
        void Present(ChartColorDimensionOptions options) {
            options.Name = "Temperature"; options.ValueFormat = format;
            options.LegendStyle = gradient ? ChartColorLegendStyle.Gradient : swatches ? ChartColorLegendStyle.Swatches : ChartColorLegendStyle.Auto;
        }
        var chart = Chart.Create().ConfigureLabels(labels => labels.NoData = "Not sampled");
        if (kind == ChartSeriesKind.TileMap) {
            var codes = new[] { "CA", "TX", "NY", "FL", "WA", "IL", "CO" };
            var values = new[] { 0d, 2.5, 5, 10, 22.5, 40, 55 };
            return chart.AddTileMap("Daily mean", ChartTileMapCatalog.Get("us-states"),
                codes.Select((code, index) => new ChartRegionMapItem(code, values[index])))
                .WithMapLabels().WithMapColorScale(scale).ConfigureMapColorDimension(Present);
        }
        if (kind == ChartSeriesKind.Heatmap) {
            return chart.WithXLabels("A", "B", "C", "D", "E", "F")
                .AddHeatmapRow("Primary", PositionedColorValues.Take(6))
                .AddHeatmapRow("Secondary", PositionedColorValues.Skip(6))
                .WithHeatmapColorScale(scale).ConfigureHeatmapColorDimension(Present)
                .WithHeatmapCellGap(3).WithHeatmapCellRadius(3).WithDataLabels();
        }
        if (kind == ChartSeriesKind.CalendarHeatmap) {
            return chart.AddCalendarHeatmap("Daily mean", Enumerable.Range(0, 120)
                .Where(index => index % 7 != 0 && index % 9 != 0)
                .Select(index => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1).AddDays(index), index * 2.5 % 13)),
                firstDayOfWeek: DayOfWeek.Monday)
                .WithHeatmapColorScale(ChartColorScale.Sequential(colors.SequentialRamp).WithValueRange(0, 12)
                    .WithLabels("Cool", null, "Warm").WithNoDataColor(colors.Border))
                .ConfigureHeatmapColorDimension(Present);
        }
        if (kind == ChartSeriesKind.Treemap) {
            return chart.WithValueFormat(ChartValueFormat.Number("0 'tasks'", CultureInfo.InvariantCulture)).WithDataLabels()
                .AddTreemap("Allocated work", new[] {
                    new ChartHierarchyItem("north", "North"),
                    new ChartHierarchyItem("support", "Support", parentId: "north", value: 18, colorValue: -4),
                    new ChartHierarchyItem("research", "Research", parentId: "north", value: 12, colorValue: 11),
                    new ChartHierarchyItem("south", "South"),
                    new ChartHierarchyItem("field", "Field", parentId: "south", value: 22, colorValue: 8),
                    new ChartHierarchyItem("platform", "Platform", parentId: "south", value: 15, colorValue: 1),
                    new ChartHierarchyItem("operations", "Operations", parentId: "south", value: 15)
                }).ConfigureTreemap(options => {
                    options.Gap = 3; options.GroupPadding = 6; options.ColorScale = scale;
                    Present(options.ColorDimension);
                });
        }
        if (kind is not (ChartSeriesKind.Scatter or ChartSeriesKind.Bubble)) return null;
        var minutes = new[] { 18d, 30, 24, 44, 32 };
        var temperatures = new double?[] { -12, 0, null, 2.5, 22.5 };
        var sizes = new[] { 9d, 36, 81, 49, 25 };
        chart.WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
            .WithXAxisBounds(0, 100).WithYAxisBounds(0, 60).WithDataLabels(false)
            .WithValueFormat(ChartValueFormat.Number("0.0 'min'", CultureInfo.InvariantCulture));
        if (kind == ChartSeriesKind.Bubble) {
            chart.AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("recorded-batches"), "Recorded batches", temperatures.Select((value, index) =>
                new ChartBubble((index + 1) * 16, minutes[index], sizes[index]).WithColorValue(value))));
            if (!gradient) scale = ChartColorScale.Discrete(new[] {
                new ChartColorBand(0, colors.Palette[0], "Cold"),
                new ChartColorBand(15, colors.Status.Medium.Fill, "Mild"),
                new ChartColorBand(null, colors.Status.Critical.Fill, "Warm")
            }).WithNoDataColor(colors.Border);
        } else chart.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("recorded-batches"), "Recorded batches", temperatures.Select((value, index) =>
            new ChartPoint((index + 1) * 16, minutes[index]).WithColorValue(value))));
        chart.Series[0].WithColorScale(scale).ConfigureColorDimension(Present);
        return chart;
    }
}

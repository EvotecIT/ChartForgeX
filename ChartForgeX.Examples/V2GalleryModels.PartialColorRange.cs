using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart? PartialColorObservations(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        if (kind is not (ChartSeriesKind.TileMap or ChartSeriesKind.Scatter)
            || variant is not ("partial-color-range" or "compact-partial-color-range")) return null;
        var colors = VisualTheme.Graphite().Resolve(mode);
        var chart = Chart.Create().ConfigureLabels(labels => labels.NoData = "Not reported");
        if (kind == ChartSeriesKind.TileMap) {
            var codes = new[] { "CA", "TX", "NY", "FL", "WA", "IL" };
            var counts = new[] { 0d, 18, 42, 76, 29, 51 };
            return chart.AddTileMap("Requests", ChartTileMapCatalog.Get("us-states"),
                codes.Select((code, index) => new ChartRegionMapItem(code, counts[index])))
                .WithMapLabels().WithMapColorScale(ChartColorScale.Sequential(colors.SequentialRamp)
                    .WithValueRange(0, null).WithLabels("None", null, "Observed peak").WithNoDataColor(colors.Border))
                .ConfigureMapColorDimension(options => {
                    options.Name = "Requests"; options.ValueFormat = ChartValueFormat.Number("0", CultureInfo.InvariantCulture);
                    options.LegendStyle = ChartColorLegendStyle.Gradient;
                });
        }
        var duration = new[] { 18d, 30, 24, 44, 32 };
        var completion = new double?[] { 55, 82, null, 68, 95 };
        chart.WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
            .WithXAxisBounds(0, 100).WithYAxisBounds(0, 60).WithDataLabels(false)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("recorded-batches"), "Recorded batches", completion.Select((value, index) =>
                new ChartPoint((index + 1) * 16, duration[index]).WithColorValue(value))));
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(colors.Palette[0], colors.Status.Pass.Fill)
                .WithValueRange(null, 100).WithLabels("Observed minimum", null, "Goal").WithNoDataColor(colors.Border))
            .ConfigureColorDimension(options => {
                options.Name = "Completion"; options.ValueFormat = ChartValueFormat.Number("0'%'", CultureInfo.InvariantCulture);
                options.LegendStyle = ChartColorLegendStyle.Swatches;
            });
        return chart;
    }
}

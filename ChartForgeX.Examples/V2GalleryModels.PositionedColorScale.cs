using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static readonly double?[] PositionedColorValues = { -30, -20, -3, 0, 2.5, 5, 10, 22.5, 40, 55, null, null };

    private static Chart? PositionedColorObservations(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        if (kind is not (ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.Scatter)
            || variant is not ("positioned-color-scale" or "compact-positioned-color-scale")) return null;
        var colors = VisualTheme.Graphite().Resolve(mode);
        var scale = PositionedTemperatureScale().WithNoDataColor(colors.Border);
        if (kind == ChartSeriesKind.TileMap) {
            var codes = new[] { "CA", "TX", "NY", "FL", "WA", "IL", "CO" };
            var values = PositionedColorValues.Where(value => value >= 0).Select(value => value!.Value).ToArray();
            return Chart.Create().AddTileMap("Temperature", ChartTileMapCatalog.Get("us-states"),
                codes.Select((code, index) => new ChartRegionMapItem(code, values[index])))
                .WithMapLabels().WithMapColorScale(scale);
        }
        if (kind == ChartSeriesKind.Heatmap) {
            var matrix = Chart.Create().WithXLabels("A", "B", "C", "D", "E", "F")
                .AddHeatmapRow("Primary", PositionedColorValues.Take(6))
                .AddHeatmapRow("Secondary", PositionedColorValues.Skip(6))
                .WithHeatmapColorScale(scale).WithHeatmapCellGap(3).WithHeatmapCellRadius(3)
                .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Auto).WithDataLabels();
            matrix.Series[0].WithInteractionKey("primary-measurements");
            matrix.Series[1].WithInteractionKey("secondary-measurements");
            return matrix;
        }
        var minutes = new[] { 24d, 36, 45, 20, 32, 50, 28, 41, 18, 35, 22, 48 };
        var chart = Chart.Create().WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
            .WithXAxisBounds(0, 100).WithYAxisBounds(0, 60).WithDataLabels(false)
            .ConfigureLabels(labels => { labels.Color = "Temperature (°C)"; labels.NoData = "Not sampled"; })
            .AddScatter("Recorded batches", PositionedColorValues.Select((value, index) =>
                new ChartPoint((index + 1) * 8, minutes[index]).WithColorValue(value)));
        chart.Series[0].WithColorScale(scale).WithInteractionKey("recorded-batches")
            .ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Circle; markers.StrokeWidth = 1; });
        return chart;
    }

    private static ChartColorScale PositionedTemperatureScale() => ChartColorScale.Diverging(new[] {
        new ChartColorStop(-20, ChartColor.FromHex("#2166AC")), new ChartColorStop(-3, ChartColor.FromHex("#67A9CF"))
    }, new ChartColorStop(0, ChartColor.FromHex("#D1D5DB")), new[] {
        new ChartColorStop(5, ChartColor.FromHex("#D9A441")), new ChartColorStop(40, ChartColor.FromHex("#B2182B"))
    }).WithLabels("Cold", "Freezing", "Warm");
}

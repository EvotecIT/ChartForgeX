using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart? NumericColorObservations(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        if (kind is not (ChartSeriesKind.Scatter or ChartSeriesKind.Bubble) || variant is not ("color-scale" or "compact-color-scale")) return null;
        var colors = VisualTheme.Graphite().Resolve(mode);
        var samples = new[] {
            new { Batch = 15d, Minutes = 28d, Requests = 9d, Temperature = (double?)-12 },
            new { Batch = 30d, Minutes = 42d, Requests = 36d, Temperature = (double?)0 },
            new { Batch = 45d, Minutes = 18d, Requests = 81d, Temperature = (double?)null },
            new { Batch = 60d, Minutes = 35d, Requests = 36d, Temperature = (double?)12 },
            new { Batch = 80d, Minutes = 22d, Requests = 120d, Temperature = (double?)35 }
        };
        var chart = Chart.Create().WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
            .WithXAxisBounds(0, 100).WithYAxisBounds(0, 60).WithDataLabels(false)
            .ConfigureLabels(labels => { labels.Color = "Temperature (°C)"; labels.NoData = "Not sampled"; });
        ChartColorScale scale;
        if (kind == ChartSeriesKind.Scatter) {
            chart.AddScatter("Recorded batches", samples.Select(sample => new ChartPoint(sample.Batch, sample.Minutes).WithColorValue(sample.Temperature)));
            scale = ChartColorScale.Diverging(new[] { colors.Palette[0] }, colors.Surface,
                new[] { ChartColor.FromHex("#D9A441"), ChartColor.FromHex("#CF6A42"), colors.Status.Critical.Fill }, 0)
                .WithValueRange(-15, 30).WithLabels("Cold", "Freezing", "Warm");
        } else {
            chart.AddBubble("Recorded batches", samples.Select(sample => new ChartBubble(sample.Batch, sample.Minutes, sample.Requests).WithColorValue(sample.Temperature)))
                .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 4; bubble.MaximumRadius = 20; });
            scale = ChartColorScale.Discrete(new[] {
                new ChartColorBand(0, colors.Palette[0], "Cold"),
                new ChartColorBand(15, colors.Status.Medium.Fill, "Mild"),
                new ChartColorBand(null, colors.Status.Critical.Fill, "Warm")
            });
        }
        chart.Series[0].WithColorScale(scale.WithNoDataColor(colors.Border))
            .WithInteractionKey("recorded-batches").ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Circle; markers.StrokeWidth = 1; });
        chart.AddLine("Target duration", new[] { new ChartPoint(0, 30), new ChartPoint(100, 30) });
        chart.Series[1].ConfigureMarkers(markers => markers.Enabled = false);
        return chart;
    }
}

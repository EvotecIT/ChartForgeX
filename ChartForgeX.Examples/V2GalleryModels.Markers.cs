using System;
using System.Linq;
using ChartForgeX.Core;

public static partial class V2GalleryModels {
    private static Chart MarkerOptions(ChartSeriesKind kind) {
        if (kind == ChartSeriesKind.Scatter) {
            var chart = Chart.Create().WithXAxis("Sample position").WithYAxis("Example level");
            var shapes = Enum.GetValues<ChartMarkerShape>();
            for (var index = 0; index < shapes.Length; index++) {
                var shape = shapes[index];
                chart.AddScatter(shape.ToString(), new[] { new ChartPoint(index % 3 + 1, index / 3 + 1) });
                chart.Series[index].WithPointLabel(0, shape.ToString()).WithDataLabels()
                    .ConfigureMarkers(markers => { markers.Shape = shape; markers.Radius = 8; markers.StrokeWidth = 1; });
            }
            chart.Options.XAxis.WithBounds(0, 4);
            chart.Options.YAxis.WithBounds(0, 4);
            return chart;
        }
        if (kind == ChartSeriesKind.Bubble) {
            var chart = Chart.Create().WithXAxis("Workload batches").WithYAxis("Time (minutes)")
                .AddBubble("Standard", new[] { new ChartBubble(1, 18, 9), new ChartBubble(2, 30, 36), new ChartBubble(3, 24, 81) })
                .AddBubble("Priority", new[] { new ChartBubble(1.5, 32, 16), new ChartBubble(2.5, 20, 36), new ChartBubble(3.5, 36, 200) })
                .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 3; bubble.MaximumRadius = 24; });
            chart.Series[0].ConfigureMarkers(markers => markers.Shape = ChartMarkerShape.Circle);
            chart.Series[1].ConfigureMarkers(markers => markers.Shape = ChartMarkerShape.Diamond);
            return chart;
        }
        var radar = Categories(ChartSeriesKind.Radar)
            .AddRadarArea("Observed", new[] { 58d, 76, 69, 84, 62, 73 }.Select((value, index) => new ChartPoint(index + 1, value)))
            .AddRadarLine("Target", new[] { 75d, 85, 80, 90, 75, 85 }.Select((value, index) => new ChartPoint(index + 1, value)));
        radar.Series[0].ConfigureRadar(options => options.FillOpacity = .2)
            .ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Enabled = true; });
        radar.Series[1].ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Square; markers.Enabled = true; });
        radar.Options.YAxis.WithBounds(0, 100);
        return radar;
    }
}

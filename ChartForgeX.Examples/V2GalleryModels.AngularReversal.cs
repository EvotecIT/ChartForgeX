using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    private static Chart AngularReversal(ChartSeriesKind kind) {
        ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
        Chart chart;
        if (kind == ChartSeriesKind.Radar) {
            chart = Chart.Create().WithXLabels("North", "East", "South", "West").WithYAxisBounds(0, 100).WithDataLabels(false)
                .AddRadarArea("Observed", Points(58, 76, 69, 84)).AddRadarLine("Target", Points(75, 85, 80, 90));
            chart.Series[0].Radar.FillOpacity = .2;
            chart.Series[0].ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Radius = 5; });
            chart.Series[1].ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Square; markers.Radius = 4; });
        } else if (kind == ChartSeriesKind.Polar) {
            var angles = new[] { 0d, .55, 1.35, 2.8, 3.65, 5.4 };
            var observed = new[] { 84d, 62, 96, 58, 78, 69 }; var expected = new[] { 72d, 76, 82, 70, 74, 80 };
            chart = Chart.Create().WithYAxisBounds(0, 100).WithDataLabels(false)
                .ConfigureXAxis(axis => axis.LabelFormatter = angle => (angle * 180 / Math.PI).ToString("0", CultureInfo.InvariantCulture) + "°")
                .AddPolar("Observed", angles.Select((angle, index) => new ChartPoint(angle, observed[index])))
                .AddPolar("Expected", angles.Select((angle, index) => new ChartPoint(angle, expected[index])));
        } else {
            chart = Chart.Create().WithXLabels("North", "East", "South", "West").WithDataLabels()
                .AddPolarArea("Revenue", Points(92, 0, 58, 74));
            chart.Series[0].WithPointColor(0, ChartColor.FromHex("#2563EB")).WithPointFillPattern(2, ChartFillPattern.DiagonalForward);
        }
        return chart.ConfigureXAxis(axis => axis.WithReversal());
    }
}

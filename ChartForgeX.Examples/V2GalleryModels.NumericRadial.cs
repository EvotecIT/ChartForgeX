using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    private static Chart NumericRadial(ChartSeriesKind kind, string variant) {
        var bars = kind == ChartSeriesKind.RadialBar;
        var chart = Chart.Create().WithXLabels("North", "South", "East", "West")
            .WithRadialGeometry(new ChartRadialGeometryOptions(-90, 180, .22, .22, .15));
        ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
        void Add(string name, params double[] values) {
            if (bars) chart.AddRadialBar(name, Points(values)); else chart.AddRadialColumn(name, Points(values));
        }
        Add("Requests", 1200, 950, 680, 1050); Add("Follow-ups", 320, 410, 260, 360);
        if (variant is "options" or "compact-options") {
            foreach (var series in chart.Series) series.WithStackGroup("work").WithNormalization(100);
            chart.WithYAxisBounds(0, 100).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
            chart.Series[1].WithPointFillPattern(1, ChartFillPattern.DiagonalForward);
        } else {
            chart.WithYAxisBounds(0, 1500).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        }
        return chart;
    }
}

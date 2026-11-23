using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class PolarExamples {
    public static Chart CreateIncidentDirection() => Chart.Create()
        .Title("Incident Direction Polar")
        .Subtitle("True radian angles preserve irregular observation directions")
        .XAxis(x => x.Title("Direction"))
        .YAxis(y => y.Title("Signal strength"))
        .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
        .Size(800, 487)
        .XAxis(axis => axis.LabelFormatter = angle => (angle * 180 / System.Math.PI).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "°")
        .AddPolar("Observed", new[] {
            new ChartPoint(0, 84),
            new ChartPoint(0.55, 62),
            new ChartPoint(1.35, 96),
            new ChartPoint(2.8, 58),
            new ChartPoint(3.65, 78),
            new ChartPoint(5.4, 69)
        }, ChartColor.FromRgb(96, 165, 250))
        .AddPolar("Expected", new[] {
            new ChartPoint(0, 72),
            new ChartPoint(0.55, 76),
            new ChartPoint(1.35, 82),
            new ChartPoint(2.8, 70),
            new ChartPoint(3.65, 74),
            new ChartPoint(5.4, 80)
        }, ChartColor.FromRgb(52, 211, 153));

    public static Chart CreateZeroValueArea() {
        var chart = Chart.Create()
            .Title("Zero-Value Polar Area")
            .Subtitle("Zero segments keep point indexes stable while positive segments remain styled")
            .Theme(VisualTheme.Graphite())
            .Size(800, 449)
            .XLabels("No findings", "Reviewed", "Escalated")
            .AddPolarArea("Review mix", new[] { new ChartPoint(1, 0), new ChartPoint(2, 82), new ChartPoint(3, 18) });
        chart.Series[0].PointColor(1, ChartColor.FromRgb(15, 118, 110));
        return chart;
    }
}

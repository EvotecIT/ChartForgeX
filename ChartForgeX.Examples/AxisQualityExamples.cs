using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class AxisQualityExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var chart = Chart.Create()
            .WithTitle("Automatic Axis Label Fitting")
            .WithSubtitle("Dense long-form values thin predictably instead of colliding")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(760, 300)
            .WithLegend(false)
            .WithXAxis("Review window")
            .WithYAxis("Processed volume")
            .WithTickCount(16)
            .WithYAxisLabelDensity(ChartLabelDensity.Auto)
            .WithValueFormatter(value => "$" + value.ToString("#,0.00", CultureInfo.InvariantCulture))
            .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6", "W7", "W8")
            .AddSmoothArea("Processed", new[] { 1200000d, 1218000d, 1212000d, 1231000d, 1226000d, 1244000d, 1240000d, 1259000d }.Select((value, index) => new ChartPoint(index + 1, value)));
        chart.WithPngOutputScale(pngOutputScale);
        chart.SaveSvg(Path.Combine(output, "axis-label-fitting-showcase.svg"));
        chart.SaveHtml(Path.Combine(output, "axis-label-fitting-showcase.html"));
        chart.SavePng(Path.Combine(output, "axis-label-fitting-showcase.png"));
    }
}

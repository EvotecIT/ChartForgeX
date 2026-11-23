using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class AxisQualityExamples {
    public static void Write(string output, ChartRasterScale pngOutputScale) {
        var chart = Chart.Create()
            .Title("Automatic Axis Label Fitting")
            .Subtitle("Dense long-form values thin predictably instead of colliding")
            .Theme(VisualTheme.Graphite())
            .Size(800, 316)
            .Legend(false)
            .XAxis(x => x.Title("Review window"))
            .YAxis(y => y.Title("Processed volume"))
            .TickCount(16)
            .YAxis(y => y.LabelDensity(ChartLabelDensity.Auto))
            .FormatValuesBy(value => "$" + value.ToString("#,0.00", CultureInfo.InvariantCulture))
            .XLabels("W1", "W2", "W3", "W4", "W5", "W6", "W7", "W8")
            .AddSmoothArea("Processed", new[] { 1200000d, 1218000d, 1212000d, 1231000d, 1226000d, 1244000d, 1240000d, 1259000d }.Select((value, index) => new ChartPoint(index + 1, value)));
        chart.Raster(p => p.OutputScale((int)pngOutputScale));
        chart.SaveSvg(Path.Combine(output, "axis-label-fitting-showcase.svg"));
        chart.SaveHtml(Path.Combine(output, "axis-label-fitting-showcase.html"));
        chart.SavePng(Path.Combine(output, "axis-label-fitting-showcase.png"));
    }
}

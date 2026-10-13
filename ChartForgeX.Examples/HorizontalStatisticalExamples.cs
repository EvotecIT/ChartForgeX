using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Shows value-axis formatting and statistical guides on horizontal observations.</summary>
internal static class HorizontalStatisticalExamples {
    internal static void Write(string output, ChartPngOutputScale outputScale) {
        foreach (var width in new[] { 360, 760 }) foreach (var dark in new[] { false, true }) {
            var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 15), new ChartPoint(3, 20), new ChartPoint(4, 50) };
            var chart = Chart.Create().WithSize(width, 420).WithLegend(false)
                .WithTitle("Observed latency").WithXAxis("Latency").WithYAxis("Run")
                .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
                .AddLine("Observations", points)
                .AddMeanLine("Mean", points).AddMedianLine("Median", points)
                .AddStandardDeviationBand("Deviation", points);
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            chart.Options.XAxis.WithValueFormat(ChartValueFormat.Number("0' ms'"));
            for (var i = 1; i <= 4; i++) chart.Options.YAxis.Labels.Add(new ChartAxisLabel(i, "Run " + i));
            ExampleArtifactWriter.SaveChart(chart, output, "horizontal-statistics-" + width + (dark ? "-dark" : "-light"), outputScale);
        }
    }
}

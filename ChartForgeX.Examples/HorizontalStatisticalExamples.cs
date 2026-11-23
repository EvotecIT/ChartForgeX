using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Shows value-axis formatting and statistical guides on horizontal observations.</summary>
internal static class HorizontalStatisticalExamples {
    internal static void Write(string output, ChartRasterScale outputScale) {
        foreach (var width in new[] { 360, 760 }) foreach (var dark in new[] { false, true }) {
            var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 15), new ChartPoint(3, 20), new ChartPoint(4, 50) };
            var chart = Chart.Create().Size(width, 420).Legend(false)
                .Title("Observed latency").XAxis(x => x.Title("Latency")).YAxis(y => y.Title("Run"))
                .Theme(VisualTheme.Graphite()).ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light)
                .AddLine("Observations", points)
                .AddMeanLine("Mean", points).AddMedianLine("Median", points)
                .AddStandardDeviationBand("Deviation", points);
            chart.Options.Lines.Orientation = ChartOrientation.Horizontal;
            chart.Options.XAxis.ValueFormat(ChartValueFormat.Number("0' ms'"));
            for (var i = 1; i <= 4; i++) chart.Options.YAxis.Labels.Add(new ChartAxisLabel(i, "Run " + i));
            ExampleArtifactWriter.SaveChart(chart, output, "horizontal-statistics-" + width + (dark ? "-dark" : "-light"), outputScale);
        }
    }
}

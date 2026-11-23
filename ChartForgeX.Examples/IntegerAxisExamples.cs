using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Count charts with whole-number tick positions in vertical and horizontal layouts.</summary>
public static class IntegerAxisExamples {
    /// <summary>Creates monthly counts whose complete short labels thin to the available width.</summary>
    public static Chart CreateMonthlyCounts(int width, ThemeMode mode) => Chart.Create().Size(width, 280)
        .Theme(VisualTheme.Graphite()).ThemeMode(mode).Title("Monthly observations").Legend(false).DataLabels(false)
        .XLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")
        .YAxis(y => y.IntegerTicks()).Bars(b => b.MaxWidth(48))
        .AddBar("Count", Enumerable.Range(1, 12).Select(index => new ChartPoint(index, index % 4 + 1)).ToArray());

    /// <summary>Creates signed stacks with rounded exterior corners and square internal joins.</summary>
    public static Chart CreateSignedStack(bool horizontal, int width, ThemeMode mode) {
        var chart = Chart.Create().Size(width, 320).Theme(VisualTheme.Graphite()).ThemeMode(mode)
            .Title("Signed category adjustments").Subtitle("Square stack joins · rounded outer ends")
            .Legend(l => l.Position(ChartLegendPosition.TopLeft)).Bars(b => b.Stacked().MaxWidth(48).VisualStyle(ChartBarVisualStyle.Flat().CornerRadius(10)));
        var first = new[] { new ChartPoint(1, 3), new ChartPoint(2, -3) };
        var second = new[] { new ChartPoint(1, 2), new ChartPoint(2, -2) };
        return horizontal
            ? chart.XAxis(x => x.IntegerTicks()).AddHorizontalBar("First", first).AddHorizontalBar("Second", second)
            : chart.YAxis(y => y.IntegerTicks()).AddBar("First", first).AddBar("Second", second);
    }
    /// <summary>Creates a small count chart using the physical value axis's integer tick policy.</summary>
    public static Chart Create(int maximum, bool horizontal, int width, ThemeMode mode) {
        if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
        var chart = Chart.Create().Size(width, 300).Theme(VisualTheme.Graphite()).ThemeMode(mode)
            .Title("Items by queue").Subtitle("Whole-number counts · maximum " + maximum)
            .Legend(false).XLabels("Ready", "Running", "Complete");
        var points = new[] { new ChartPoint(1, 1), new ChartPoint(2, Math.Max(1, maximum - 1)), new ChartPoint(3, maximum) };
        return horizontal
            ? chart.XAxis(x => x.IntegerTicks().Bounds(0, maximum)).AddHorizontalBar("Items", points)
            : chart.YAxis(y => y.IntegerTicks().Bounds(0, maximum)).AddBar("Items", points);
    }

    /// <summary>Writes paired SVG, HTML and native PNG examples for small and large count domains.</summary>
    public static void Write(string output, ChartPngOutputScale scale) {
        TopologyAccentExamples.Write(output);
        foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark }) {
            var sparse = Chart.Create().Size(800, 200).Theme(VisualTheme.Graphite()).ThemeMode(mode)
                .Title("Sparse category counts").XLabels("January", "February")
                .Bars(b => b.Stacked()).YAxis(y => y.IntegerTicks()).Bars(b => b.MaxWidth(48))
                .AddBar("Passed", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) })
                .AddBar("Pending", new[] { new ChartPoint(1, 1), new ChartPoint(2, 0) });
            ExampleArtifactWriter.SaveChart(sparse, output, "category-counts-" + mode.ToString().ToLowerInvariant(), scale);
            var responsive = new HtmlResponsiveChartRenderer().RenderFragment(sparse, minimumWidth: 240, maximumWidth: 1600, printWidth: 688);
            File.WriteAllText(Path.Combine(output, "responsive-category-counts-" + mode.ToString().ToLowerInvariant() + ".html"),
                ExampleTypography.EmbedInHost("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Offline category-count layout</title>" +
                "<style>body{margin:24px;font-size:14px}main{max-width:1600px;margin:auto}.plot{overflow:auto;min-width:0}</style></head><body><main><h1>Offline category-count layout</h1><p>Resize the panel: text stays at its token size, height stays at 280 pixels, and the legend and bar slots reflow.</p><div class=\"plot\">" + responsive + "</div></main></body></html>"));
            foreach (var width in new[] { 320, 1120 })
                ExampleArtifactWriter.SaveChart(CreateMonthlyCounts(width, mode), output,
                    "monthly-counts-" + width + "-" + mode.ToString().ToLowerInvariant(), scale);
            foreach (var horizontal in new[] { false, true }) foreach (var width in new[] { 320, 1120 })
                ExampleArtifactWriter.SaveChart(CreateSignedStack(horizontal, width, mode), output,
                    "stacked-corners-" + (horizontal ? "horizontal-" : "vertical-") + width + "-" + mode.ToString().ToLowerInvariant(), scale);
        }
        foreach (var maximum in new[] { 1, 2, 3, 4, 1000000 })
            foreach (var horizontal in new[] { false, true })
                foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark }) {
                    var name = "integer-axis-" + (horizontal ? "x" : "y") + "-" + maximum + "-" + mode.ToString().ToLowerInvariant();
                    ExampleArtifactWriter.SaveChart(Create(maximum, horizontal, 640, mode), output, name, scale);
                }
    }
}

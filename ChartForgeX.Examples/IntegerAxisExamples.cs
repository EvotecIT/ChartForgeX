using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Count charts with whole-number tick positions in vertical and horizontal layouts.</summary>
public static class IntegerAxisExamples {
    /// <summary>Creates a small count chart using the physical value axis's integer tick policy.</summary>
    public static Chart Create(int maximum, bool horizontal, int width, VisualThemeMode mode) {
        if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
        var chart = Chart.Create().WithSize(width, 300).WithTheme(VisualTheme.Graphite(), mode)
            .WithTitle("Items by queue").WithSubtitle("Whole-number counts · maximum " + maximum)
            .WithLegend(false).WithXLabels("Ready", "Running", "Complete");
        var points = new[] { new ChartPoint(1, 1), new ChartPoint(2, Math.Max(1, maximum - 1)), new ChartPoint(3, maximum) };
        return horizontal
            ? chart.WithXAxisIntegerTicks().WithXAxisBounds(0, maximum).AddHorizontalBar("Items", points)
            : chart.WithYAxisIntegerTicks().WithYAxisBounds(0, maximum).AddBar("Items", points);
    }

    /// <summary>Writes paired SVG, HTML and native PNG examples for small and large count domains.</summary>
    public static void Write(string output, ChartPngOutputScale scale) {
        foreach (var maximum in new[] { 1, 2, 3, 4, 1000000 })
            foreach (var horizontal in new[] { false, true })
                foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                    var name = "integer-axis-" + (horizontal ? "x" : "y") + "-" + maximum + "-" + mode.ToString().ToLowerInvariant();
                    ExampleArtifactWriter.SaveChart(Create(maximum, horizontal, 640, mode), output, name, scale);
                }
    }
}

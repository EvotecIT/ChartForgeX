using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Shows automatic numeric captions at full and compact widths without authored tick positions.</summary>
public static class NumericAxisExamples {
    /// <summary>Creates a numeric observation axis with an independent capacity series.</summary>
    public static Chart Create(int width, VisualThemeMode mode, bool longLabels = false) {
        var chart = Chart.Create().WithSize(width, 360).WithTheme(VisualTheme.Graphite(), mode)
            .WithTitle("Service capacity").WithSubtitle("Events per minute · six observation intervals")
            .AddLine("Observed", new[] { new ChartPoint(1, 42), new ChartPoint(2, 58), new ChartPoint(3, 73),
                new ChartPoint(4, 91), new ChartPoint(5, 67), new ChartPoint(6, 82) })
            .AddLine("Capacity", new[] { new ChartPoint(1, 100), new ChartPoint(6, 100) });
        if (longLabels) chart.WithXAxisValueFormatter(value => "Observation interval " + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        return chart;
    }

    /// <summary>Writes matching SVG, HTML and native PNG examples for both palettes and caption lengths.</summary>
    public static void Write(string output, ChartPngOutputScale scale) {
        foreach (var width in new[] { 240, 600 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var longLabels in new[] { false, true }) {
                var name = "numeric-axis-" + width + "-" + mode.ToString().ToLowerInvariant() + (longLabels ? "-long" : "");
                ExampleArtifactWriter.SaveChart(Create(width, mode, longLabels), output, name, scale);
            }
    }
}

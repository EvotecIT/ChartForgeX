/// <summary>Builds the report heatmap with its original domain groups, controls and percentages.</summary>
public static class ControlCoverageRecipe {
    /// <summary>Creates the configured chart used by the legacy gallery and its standalone download.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Dark) {
        if (variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create()
            .WithTitle("Control Coverage Matrix")
            .WithSubtitle("Heatmap rows for comparing domain groups across security controls")
            .WithXAxis("Control")
            .WithYAxis("Domain group")
            .WithTheme(VisualTheme.Report(), mode)
            .WithSize(980, 560)
            .WithLegend(false)
            .WithDataLabels()
            .WithHeatmapScale(ChartHeatmapScale.Semantic)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT", "CT")
            .AddHeatmapRow("Primary domains", Points(96, 88, 74, 63, 58, 92))
            .AddHeatmapRow("Parked domains", Points(74, 62, 51, 42, 38, 66))
            .AddHeatmapRow("Regional domains", Points(82, 77, 68, 54, 49, 80))
            .AddHeatmapRow("Acquired domains", Points(58, 43, 36, 28, 25, 52));
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) =>
        values.Select((value, index) => new ChartPoint(index + 1, value));
}

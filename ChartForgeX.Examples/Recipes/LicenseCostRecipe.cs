/// <summary>Builds actual and projected license spend with currency captions.</summary>
public static class LicenseCostRecipe {
    /// <summary>Creates the configured chart used by the legacy gallery and its standalone download.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create()
            .WithTitle("License Cost Trend")
            .WithSubtitle("Long formatted y-axis labels keep their own space")
            .WithXAxis("Quarter")
            .WithYAxis("Spend")
            .WithTheme(VisualTheme.Report(), mode)
            .WithSize(760, 460)
            .WithValueFormatter(value => "$" + value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
            .WithXLabels("Q1", "Q2", "Q3", "Q4")
            .AddColumnAreaCombo("Actual", Points(820000, 970000, 1010000, 1210000), "Projected", Points(860000, 940000, 1050000, 1160000), ChartColor.FromRgb(14, 165, 233), ChartColor.FromRgb(37, 99, 235));
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) =>
        values.Select((value, index) => new ChartPoint(index + 1, value));
}

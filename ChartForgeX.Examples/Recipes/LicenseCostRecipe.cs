/// <summary>Builds actual and projected license spend with currency captions.</summary>
public static class LicenseCostRecipe {
    /// <summary>Creates the configured chart used by the legacy gallery and its standalone download.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create()
            .Title("License Cost Trend")
            .Subtitle("Long formatted y-axis labels keep their own space")
            .XAxis(x => x.Title("Quarter"))
            .YAxis(y => y.Title("Spend"))
            .Theme(VisualTheme.Graphite()).ThemeMode(mode)
            .Size(800, 484)
            .FormatValuesBy(value => "$" + value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
            .XLabels("Q1", "Q2", "Q3", "Q4")
            .AddBarAreaCombo("Actual", Points(820000, 970000, 1010000, 1210000), "Projected", Points(860000, 940000, 1050000, 1160000), ChartColor.FromRgb(14, 165, 233), ChartColor.FromRgb(37, 99, 235));
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) =>
        values.Select((value, index) => new ChartPoint(index + 1, value));
}

/// <summary>Groups numeric hexbin measurements into named color bands.</summary>
public static class HexbinHeatmapColorScaleRecipe {
    /// <summary>Creates low, medium and high measurement bands.</summary>
    public static Chart Create(string variant = "color-scale", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("color-scale" or "compact-color-scale")) throw new ArgumentOutOfRangeException(nameof(variant));
        var colors = VisualTheme.Graphite().Resolve(mode);
        return Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat").WithDataLabels()
            .AddHexbinHeatmapRow("Primary", new double?[] { 0, 30, 40, null, 80, 100 }, id: new ChartSeriesId("primary-measurements"))
            .AddHexbinHeatmapRow("Secondary", new double?[] { 15, 40, 60, 80, null, 120 }, id: new ChartSeriesId("secondary-measurements"))
            .WithHeatmapColorScale(ChartColorScale.Discrete(new[] {
                new ChartColorBand(40, colors.SequentialRamp[0], "Low"),
                new ChartColorBand(80, colors.SequentialRamp[colors.SequentialRamp.Count / 2], "Medium"),
                new ChartColorBand(null, colors.SequentialRamp[colors.SequentialRamp.Count - 1], "High")
            })).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

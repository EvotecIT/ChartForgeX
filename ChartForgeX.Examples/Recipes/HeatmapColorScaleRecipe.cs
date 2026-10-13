/// <summary>Shows positive and negative measurements on a diverging heatmap scale.</summary>
public static class HeatmapColorScaleRecipe {
    /// <summary>Creates the selected numeric color-scale example.</summary>
    public static Chart Create(string variant = "color-scale", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("color-scale" or "compact-color-scale")) throw new ArgumentOutOfRangeException(nameof(variant));
        var colors = VisualTheme.Graphite().Resolve(mode);
        var scale = variant == "color-scale"
            ? ChartColorScale.Diverging(new[] { colors.Status.Critical.Fill }, colors.Surface,
                new[] { colors.SequentialRamp[2], colors.SequentialRamp[3], colors.SequentialRamp[4] }, 0)
            : ChartColorScale.Diverging(colors.Status.Critical.Fill, colors.Surface, colors.Palette[0], 0);
        return Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat").WithDataLabels()
            .AddHeatmapRow("Primary", new double?[] { -20, -10, 0, null, 10, 20 }, id: new ChartSeriesId("primary-measurements"))
            .AddHeatmapRow("Secondary", new double?[] { -15, 0, 5, 15, null, 25 }, id: new ChartSeriesId("secondary-measurements"))
            .WithHeatmapColorScale(scale.WithValueRange(-20, 20).WithLabels("Below target", "Target", "Above target"))
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

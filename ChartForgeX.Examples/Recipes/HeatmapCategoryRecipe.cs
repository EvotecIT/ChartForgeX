/// <summary>Shows assessment states as categorical heatmap cells.</summary>
public static class HeatmapCategoryRecipe {
    /// <summary>Creates status rows with explicit category colors and a production group.</summary>
    public static Chart Create(string variant = "options", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "options") throw new ArgumentOutOfRangeException(nameof(variant));
        var colors = VisualTheme.Graphite().Resolve(mode);
        var primary = ChartSeries.CreateHeatmapCategoryRow(new ChartSeriesId("primary"), "Primary", new ChartHeatmapCell?[] {
            new("pass", "Ready", "Assessment passed"), new("warning", "Review"), null, new("pass", "Ready")
        }, "Production");
        var secondary = ChartSeries.CreateHeatmapCategoryRow(new ChartSeriesId("secondary"), "Secondary", new ChartHeatmapCell?[] {
            new("pass"), new("pass"), new("warning"), new("pass")
        }, "Production");
        return Chart.Create().WithStateCategories(new ChartStateCategory("pass", "Available", colors.Status.Pass.Fill),
                new ChartStateCategory("warning", "Review", colors.Status.Medium.Fill, ChartStatePattern.Hatched),
                new ChartStateCategory("critical", "Unavailable", colors.Status.Critical.Fill, ChartStatePattern.CrossHatched))
            .WithXLabels("Identity", "Network", "Storage", "Backup")
            .AddSeries(primary).AddSeries(secondary)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

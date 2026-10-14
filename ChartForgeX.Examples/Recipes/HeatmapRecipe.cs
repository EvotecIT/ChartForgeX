/// <summary>Shows optional coverage measurements across service areas.</summary>
public static class HeatmapRecipe {
    /// <summary>Creates numeric coverage rows, preserving empty cell positions.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "compact-options")) throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create().WithXLabels("Identity", "Network", "Storage", "Backup", "Access", "Recovery")
            .AddHeatmapRow("Primary", variant == "sparse" ? new double?[] { 92, null, null, 70, null, 41 } : new double?[] { 92, 70, 56, null, 84, 41 },
                id: new ChartSeriesId("primary-measurements"))
            .AddHeatmapRow("Secondary", variant == "sparse" ? new double?[] { null, 84, null, null, 90, null } : new double?[] { 76, 84, 46, 70, 90, 62 },
                id: new ChartSeriesId("secondary-measurements"))
            .WithHeatmapScale(ChartHeatmapScale.Sequential).WithHeatmapCellGap(3).WithHeatmapCellRadius(3)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Auto)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

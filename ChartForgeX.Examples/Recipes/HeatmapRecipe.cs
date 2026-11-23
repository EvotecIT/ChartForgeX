/// <summary>Shows monthly coverage measurements across service areas.</summary>
public static class HeatmapRecipe {
    /// <summary>Creates numeric coverage rows; the sparse variant preserves empty cell positions.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "compact-options")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (variant == "sparse") return Chart.Create().XLabels("Identity", "Network", "Storage", "Backup", "Access", "Recovery")
            .AddHeatmapRow("Primary", new double?[] { 92, null, null, 70, null, 41 }, id: new ChartSeriesId("primary-measurements"))
            .AddHeatmapRow("Secondary", new double?[] { null, 84, null, null, 90, null }, id: new ChartSeriesId("secondary-measurements"))
            .Heatmap(h => h.Scale(ChartHeatmapScale.Sequential).CellGap(3).CellRadius(3).ValueTextMode(ChartHeatmapValueTextMode.Auto));

        var areas = new[] { "Identity", "Network", "Storage", "Backup", "Access", "Recovery" };
        var sites = variant == "wide"
            ? new[] { "London", "Frankfurt", "Warsaw", "New York", "Singapore", "Sydney" }
            : new[] { "London", "Warsaw", "New York", "Sydney" };
        // Each service area has its own baseline; each site is a little ahead or behind, with uneven cells.
        var baselines = new[] { 91d, 84, 72, 63, 88, 55 };
        var siteOffsets = new[] { 6d, 3, -2, 4, -6, -9 };
        var chart = Chart.Create().XLabels(areas);
        for (var row = 0; row < sites.Length; row++) {
            var site = Array.IndexOf(new[] { "London", "Frankfurt", "Warsaw", "New York", "Singapore", "Sydney" }, sites[row]);
            var values = areas.Select((_, column) => (double?)Math.Min(99, Math.Round(
                baselines[column] + siteOffsets[site] + Math.Sin(column * 1.9 + site * 1.3) * 6))).ToArray();
            chart.AddHeatmapRow(sites[row], values, id: new ChartSeriesId(sites[row].Replace(" ", "-").ToLowerInvariant() + "-coverage"));
        }
        return chart.Heatmap(heatmap => heatmap.ColorDimension(dimension => dimension.Name = "Coverage")).Heatmap(h => h.Scale(ChartHeatmapScale.Sequential).CellGap(3).CellRadius(3).ValueTextMode(ChartHeatmapValueTextMode.Auto))
            .ValueFormat(ChartValueFormat.Number("0'%'", CultureInfo.InvariantCulture));
    }
}

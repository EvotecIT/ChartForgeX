/// <summary>Deterministic placement of weighted service-observation terms.</summary>
public static class WordCloudRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) =>
        Chart.Create().AddWordCloud("Topics", new[] {
            new ChartWordCloudItem("Availability", 20), new ChartWordCloudItem("Identity", 16), new ChartWordCloudItem("Network", 14),
            new ChartWordCloudItem("Storage", 12), new ChartWordCloudItem("Recovery", 10), new ChartWordCloudItem("Capacity", 8),
            new ChartWordCloudItem("Services", 6), new ChartWordCloudItem("Validation", 4)
        }).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
}

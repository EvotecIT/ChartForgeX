/// <summary>Plots processing time against workload, including a sparse observation example.</summary>
public static class ScatterRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse")) throw new ArgumentOutOfRangeException(nameof(variant));
        return ProcessingTime(variant).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static Chart ProcessingTime(string variant) {
        var positions = variant == "sparse" ? new[] { 1, 3, 6 } : Enumerable.Range(1, 6);
        var observations = positions.Select(index => new ChartPoint(index, 20 + index * 7 % 29, variant == "sparse" && index == 6));
        var processing = ChartSeries.CreateScatter(new ChartSeriesId("processing-time"), "Processing time", observations);
        return Chart.Create().WithXLabels("1", "2", "3", "4", "5", "6")
            .WithXAxis("Workload batches").WithYAxis("Time (minutes)").AddSeries(processing);
    }
}

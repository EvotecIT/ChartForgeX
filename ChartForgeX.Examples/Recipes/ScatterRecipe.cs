/// <summary>Plots processing time against batch size for two job queues, including a sparse observation example.</summary>
public static class ScatterRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse")) throw new ArgumentOutOfRangeException(nameof(variant));
        return variant == "sparse" ? Sparse() : ProcessingTime(variant == "compact" ? 14 : 28);
    }

    private static Chart ProcessingTime(int count) {
        // Processing time grows with batch size; priority jobs run on reserved capacity and finish faster.
        var standard = ChartSeries.CreateScatter(new ChartSeriesId("standard"), "Standard queue", Batches(count, 0, 9, .42));
        var priority = ChartSeries.CreateScatter(new ChartSeriesId("priority"), "Priority queue", Batches(count, 3, 4, .24));
        return Chart.Create().WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
            .AddSeries(standard).AddSeries(priority);
    }

    private static IEnumerable<ChartPoint> Batches(int count, int seed, double overhead, double perItem) {
        for (var index = 0; index < count; index++) {
            // Golden-ratio steps spread the batch sizes evenly without a visible pattern.
            var size = Math.Round(8 + (index * .618034 + seed * .29) % 1 * 112);
            // A deterministic wobble stands in for queue contention.
            var noise = Math.Sin((index + 1) * 2.3 + seed) * 4 + Math.Cos((index + 2) * 1.7 + seed) * 2.5;
            yield return new ChartPoint(size, Math.Round(overhead + size * perItem + noise, 1));
        }
    }

    private static Chart Sparse() {
        var observations = new[] { 1, 3, 6 }.Select(index => new ChartPoint(index, 20 + index * 7 % 29, index == 6));
        var processing = ChartSeries.CreateScatter(new ChartSeriesId("processing-time"), "Processing time", observations);
        return Chart.Create().WithXLabels("1", "2", "3", "4", "5", "6")
            .WithXAxis("Workload batches").WithYAxis("Time (minutes)").AddSeries(processing);
    }
}

/// <summary>Plots weekly request volume as bubble size against batch size and processing time for three service tiers.</summary>
public static class BubbleRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse")) throw new ArgumentOutOfRangeException(nameof(variant));
        return variant == "sparse" ? Sparse() : RequestVolume();
    }

    // Batch size in items, median processing time in minutes and requests handled during the week.
    private static Chart RequestVolume() => Chart.Create()
        .WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
        .AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("standard"), "Standard", new[] {
            new ChartBubble(40, 18, 320), new ChartBubble(80, 26, 540), new ChartBubble(120, 31, 410),
            new ChartBubble(160, 39, 620), new ChartBubble(220, 47, 380), new ChartBubble(280, 58, 260) }))
        .AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("express"), "Express", new[] {
            new ChartBubble(30, 9, 210), new ChartBubble(60, 12, 360), new ChartBubble(90, 16, 450),
            new ChartBubble(140, 21, 300), new ChartBubble(190, 27, 240), new ChartBubble(240, 33, 150) }))
        .AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("priority"), "Priority", new[] {
            new ChartBubble(20, 5, 90), new ChartBubble(45, 7, 140), new ChartBubble(70, 10, 180),
            new ChartBubble(110, 13, 120), new ChartBubble(150, 17, 80) }));

    private static Chart Sparse() {
        var requests = ChartSeries.CreateBubble(new ChartSeriesId("requests"), "Requests",
            Enumerable.Range(1, 2).Select(index => new ChartBubble(index, 20 + index * 5, index * index + 1)));
        return Chart.Create().WithXLabels("1", "2", "3", "4", "5", "6")
            .WithXAxis("Workload batches").WithYAxis("Time (minutes)").AddSeries(requests);
    }
}

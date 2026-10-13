/// <summary>Plots request volume as bubble size against workload and processing time.</summary>
public static class BubbleRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse")) throw new ArgumentOutOfRangeException(nameof(variant));
        return RequestVolume(variant).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static Chart RequestVolume(string variant) {
        var count = variant == "sparse" ? 2 : 5;
        var requests = ChartSeries.CreateBubble(new ChartSeriesId("requests"), "Requests",
            Enumerable.Range(1, count).Select(index => new ChartBubble(index, 20 + index * 5, index * index + 1)));
        return Chart.Create().WithXLabels("1", "2", "3", "4", "5", "6")
            .WithXAxis("Workload batches").WithYAxis("Time (minutes)").AddSeries(requests);
    }
}

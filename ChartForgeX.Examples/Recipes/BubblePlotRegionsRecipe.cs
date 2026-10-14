/// <summary>Shows work queues against bounded target and review regions.</summary>
public static class BubblePlotRegionsRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "plot-regions", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("plot-regions" or "compact-plot-regions")) throw new ArgumentOutOfRangeException(nameof(variant));
        return PlotRegions().WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static Chart PlotRegions() {
        var queues = ChartSeries.CreateBubble(new ChartSeriesId("work-queues"), "Work queues", new[] {
            new ChartBubble(1.2, 8.2, 12), new ChartBubble(3.2, 5.1, 36), new ChartBubble(5.9, 4.2, 20),
            new ChartBubble(8.1, 3.8, 49), new ChartBubble(8.7, 7.9, 16)
        });
        return Chart.Create().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).WithDataLabels(false)
            .WithXAxis("Delay (hours)").WithYAxis("Available capacity (score)").AddSeries(queues)
            .ConfigureBubble(options => { options.MinimumRadius = 5; options.MaximumRadius = 14; })
            .AddPlotRegion(new(ChartColor.FromHex("#5487b8"), xMinimum: 2, xMaximum: 7, yMinimum: 3, yMaximum: 7,
                label: "Target", opacity: .16))
            .AddPlotRegion(new(ChartColor.FromHex("#ba6d67"), xMinimum: 7, yMaximum: 3, label: "Review", opacity: .18));
    }
}

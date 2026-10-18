/// <summary>Compares standard and priority observations with independent bubble markers.</summary>
public static class BubbleMarkerOptionsRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "options", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("options" or "compact-options")) throw new ArgumentOutOfRangeException(nameof(variant));
        return MarkerOptions().WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static Chart MarkerOptions() {
        var standard = ChartSeries.CreateBubble(new ChartSeriesId("standard"), "Standard", new[] {
            new ChartBubble(1, 18, 9), new ChartBubble(2, 30, 36), new ChartBubble(3, 24, 81)
        }).ConfigureMarkers(markers => markers.Shape = ChartMarkerShape.Circle);
        var priority = ChartSeries.CreateBubble(new ChartSeriesId("priority"), "Priority", new[] {
            new ChartBubble(1.5, 32, 16), new ChartBubble(2.5, 20, 36), new ChartBubble(3.5, 36, 200)
        }).ConfigureMarkers(markers => markers.Shape = ChartMarkerShape.Diamond);
        return Chart.Create().WithXAxis("Workload batches").WithYAxis("Time (minutes)")
            .AddSeries(standard).AddSeries(priority)
            .ConfigureBubble(bubble => {
                bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 0; bubble.MaximumRadius = 24;
                bubble.SizeEncoding = ChartBubbleSizeEncoding.Area;
                bubble.SizeDimension.Name = "Requests"; bubble.SizeDimension.ShowLegend = true;
            });
    }
}

/// <summary>Builds domestic and international delivery times with their 10th to 90th percentile range.</summary>
public static class ErrorBarRecipe {
    /// <summary>Creates the selected error-bar example from typed measurements.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().WithXLabels("Same day", "Premium", "Express", "Standard", "Return", "Bulk").WithYAxis("Delivery time (hours)");
        if (variant == "sparse") {
            return chart.AddSeries(ChartSeries.CreateErrorBar(new ChartSeriesId("domestic"), "Domestic", new[] {
                new ChartErrorBar(1, 7, yBounds: new ChartErrorBounds(5, 10)), new ChartErrorBar(2, 14, yBounds: new ChartErrorBounds(10, 20)) }));
        }
        // Median hours per service; each pair sits either side of its category. Same-day delivery is domestic only.
        var domestic = ChartSeries.CreateErrorBar(new ChartSeriesId("domestic"), "Domestic", new[] {
            new ChartErrorBar(.86, 7, yBounds: new ChartErrorBounds(5, 10)), new ChartErrorBar(1.86, 14, yBounds: new ChartErrorBounds(10, 20)),
            new ChartErrorBar(2.86, 26, yBounds: new ChartErrorBounds(19, 35)), new ChartErrorBar(3.86, 52, yBounds: new ChartErrorBounds(38, 70)),
            new ChartErrorBar(4.86, 61, yBounds: new ChartErrorBounds(44, 86)), new ChartErrorBar(5.86, 78, yBounds: new ChartErrorBounds(58, 104)) });
        var international = ChartSeries.CreateErrorBar(new ChartSeriesId("international"), "International", new[] {
            new ChartErrorBar(2.14, 33, yBounds: new ChartErrorBounds(24, 46)), new ChartErrorBar(3.14, 54, yBounds: new ChartErrorBounds(40, 74)),
            new ChartErrorBar(4.14, 118, yBounds: new ChartErrorBounds(86, 160)), new ChartErrorBar(5.14, 132, yBounds: new ChartErrorBounds(96, 180)),
            new ChartErrorBar(6.14, 160, yBounds: new ChartErrorBounds(120, 214)) });
        // Delivery takes no negative time, so the scale starts at zero rather than below the shortest range.
        chart.Options.YAxis.Minimum = 0;
        return chart.AddSeries(domestic).AddSeries(international);
    }
}

/// <summary>Delivery-stage completion values rendered as independent progress rings.</summary>
public static class ProgressRingRecipe {
    /// <summary>Creates stage completion rings; the options variant adjusts their radius and stroke.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var points = (variant == "sparse" ? new[] { 1, 3, 6 } : Enumerable.Range(1, 6))
            .Select(index => new ChartPoint(index, 20 + index * 7 % 29, variant == "sparse" && index == 6));
        var completion = new ChartSeries(new ChartSeriesId("completion"), "Completion", ChartSeriesKind.ProgressRing, points);
        var chart = Chart.Create().WithXLabels("Received", "Qualified", "Reviewed", "Verified", "Approved", "Complete")
            .AddSeries(completion).WithProgressRingCenterLabel()
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
        if (variant == "options") chart.WithRadialProgressRadiusScale(.85).WithRadialProgressStrokeScale(1.2);
        return chart;
    }
}

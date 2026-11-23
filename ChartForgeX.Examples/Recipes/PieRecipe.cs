/// <summary>Revenue shares with optional slice labels, an offset slice and a fill pattern.</summary>
public static class PieRecipe {
    /// <summary>Creates revenue by service; the options variant labels and separates selected slices.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var revenue = new ChartSeries(new ChartSeriesId("revenue"), "Revenue", ChartSeriesKind.Pie, Observations(variant));
        var chart = Chart.Create().XLabels("Subscriptions", "Consulting", "Support", "Licenses", "Training", "Other")
            .AddSeries(revenue);
        if (variant == "options") {
            chart.DataLabels().Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.Label));
            revenue.PointSliceOffset(0, .12).PointFillPattern(1, ChartFillPattern.Crosshatch);
        }
        return chart;
    }

    private static ChartPoint[] Observations(string variant) => (variant == "sparse" ? new[] { 1, 3, 6 } : Enumerable.Range(1, 6))
        .Select(index => new ChartPoint(index, 20 + index * 7 % 29)).ToArray();
}

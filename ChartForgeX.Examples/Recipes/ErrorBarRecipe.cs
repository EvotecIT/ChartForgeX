/// <summary>Builds delivery-time observations with lower and upper uncertainty limits.</summary>
public static class ErrorBarRecipe {
    /// <summary>Creates the selected error-bar example from typed measurements.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var count = variant == "sparse" ? 2 : 5;
        var measurements = Enumerable.Range(1, count)
            .Select(index => new ChartErrorBar(index, 30 + index * 2, yBounds: new ChartErrorBounds(20 + index, 42 + index * 3)));
        var delivery = ChartSeries.CreateErrorBar(new ChartSeriesId("delivery-time"), "Delivery time", measurements);
        return Chart.Create().WithXLabels("Standard", "Express", "Premium", "Bulk", "Return").AddSeries(delivery)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

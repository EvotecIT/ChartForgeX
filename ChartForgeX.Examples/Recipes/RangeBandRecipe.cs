/// <summary>Builds an expected temperature band from typed lower and upper limits.</summary>
public static class RangeBandRecipe {
    /// <summary>Creates the selected range-band example.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var count = variant == "sparse" ? 2 : 5;
        var ranges = Enumerable.Range(1, count).Select(index => new ChartRangeBand(index, 20 + index * 2, 40 + index * 4));
        var expected = ChartSeries.CreateRangeBand(new ChartSeriesId("expected-range"), "Expected range", ranges);
        return Chart.Create().WithXLabels("08:00", "10:00", "12:00", "14:00", "16:00").AddSeries(expected)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

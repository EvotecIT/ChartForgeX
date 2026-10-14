/// <summary>Builds an expected operating range with smooth or stepped area boundaries.</summary>
public static class RangeAreaRecipe {
    /// <summary>Creates the selected range-area example from typed lower and upper limits.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create();
        if (variant is "options" or "compact-options") {
            var expected = ChartSeries.CreateRangeArea(new ChartSeriesId("expected-range"), "Expected range", new[] {
                new ChartRangeBand(1, 15, 30), new ChartRangeBand(2, 25, 50), new ChartRangeBand(3, 20, 40),
                new ChartRangeBand(4, 40, 65), new ChartRangeBand(5, 30, 55)
            }).WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
            chart.WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri").WithYAxis("Expected level").AddSeries(expected);
        }
        else {
            var count = variant == "sparse" ? 2 : 5;
            var ranges = Enumerable.Range(1, count).Select(index => new ChartRangeBand(index, 20 + index * 2, 40 + index * 4));
            var expected = ChartSeries.CreateRangeArea(new ChartSeriesId("expected-range"), "Expected range", ranges);
            chart.WithXLabels("08:00", "10:00", "12:00", "14:00", "16:00").AddSeries(expected);
        }
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

/// <summary>Builds an expected operating range with smooth or stepped area boundaries.</summary>
public static class RangeAreaRecipe {
    /// <summary>Creates the selected range-area example from typed lower and upper limits.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "singleton" or "compact-singleton" or "independent-boundaries" or "compact-independent-boundaries"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create();
        if (variant is "independent-boundaries" or "compact-independent-boundaries") {
            var ranges = ChartRangeBands.FromXY(new[] { 1d, 2, 3, 4, 5, 6, 7, 8 },
                new double?[] { 12, 22, 16, null, 25, 15, 20, 18 }, new double?[] { 32, 46, 38, 40, 48, 41, 52, 44 });
            var expected = ChartSeries.CreateRangeArea(new ChartSeriesId("expected-range"), "Expected range", ranges)
                .BoundaryInterpolation(new(ChartInterpolation.Smooth), new(ChartInterpolation.Step, ChartStepPosition.Middle))
                .ObservationId(3, new ChartObservationId("resumed-range"));
            chart.Title("Smooth lower and held upper limits").XAxis(x => x.Title("Observation")).YAxis(y => y.Title("Expected level")).AddSeries(expected);
        }
        else if (variant is "singleton" or "compact-singleton") {
            chart.Title("One observed range").AddSeries(ChartSeries.CreateRangeArea(new ChartSeriesId("expected-range"), "Expected range",
                new[] { new ChartRangeBand(0, 0, 7) }).ObservationId(0, new ChartObservationId("only-range")));
        }
        else if (variant is "options" or "compact-options") {
            var expected = ChartSeries.CreateRangeArea(new ChartSeriesId("expected-range"), "Expected range", new[] {
                new ChartRangeBand(1, 15, 30), new ChartRangeBand(2, 25, 50), new ChartRangeBand(3, 20, 40),
                new ChartRangeBand(4, 40, 65), new ChartRangeBand(5, 30, 55)
            }).Interpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
            chart.XLabels("Mon", "Tue", "Wed", "Thu", "Fri").YAxis(y => y.Title("Expected level")).AddSeries(expected);
        }
        else {
            var count = variant == "sparse" ? 2 : 5;
            var ranges = Enumerable.Range(1, count).Select(index => new ChartRangeBand(index, 20 + index * 2, 40 + index * 4));
            var expected = ChartSeries.CreateRangeArea(new ChartSeriesId("expected-range"), "Expected range", ranges);
            chart.XLabels("08:00", "10:00", "12:00", "14:00", "16:00").AddSeries(expected);
        }
        return chart;
    }
}

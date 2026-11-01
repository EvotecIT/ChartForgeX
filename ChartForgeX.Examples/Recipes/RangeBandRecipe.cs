/// <summary>Builds an expected temperature band from typed lower and upper limits.</summary>
public static class RangeBandRecipe {
    /// <summary>Creates the selected range-band example.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "singleton" or "compact-singleton" or "dense" or "compact-dense"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        if (variant is "singleton" or "compact-singleton") return Chart.Create().WithTitle("One observed range")
            .AddSeries(ChartSeries.CreateRangeBand(new ChartSeriesId("expected-range"), "Expected range", new[] { new ChartRangeBand(0, 0, 7) })
                .WithObservationId(0, new ChartObservationId("only-range")));
        if (variant is "dense" or "compact-dense") return Chart.Create().WithTitle("Estimated uncertainty").WithXAxis("Observation").WithYAxis("Expected level")
            .AddSeries(ChartSeries.CreateRangeBand(new ChartSeriesId("expected-range"), "Expected range",
                Enumerable.Range(0, 32).Select(index => new ChartRangeBand(index,
                    index % 7 == 0 ? 0 : Math.Sin(index * .5) * 9,
                    index % 7 == 0 ? 0 : Math.Sin(index * .5) * 9 + 4, index == 16))));
        if (variant == "sparse") return Chart.Create().WithXLabels("08:00", "10:00")
            .AddSeries(ChartSeries.CreateRangeBand(new ChartSeriesId("expected-range"), "Expected range",
                Enumerable.Range(1, 2).Select(index => new ChartRangeBand(index, 20 + index * 2, 40 + index * 4))))
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        // Hourly server-room temperature: the expected band follows the daily cycle and the reading leaves it once.
        var hours = variant.StartsWith("compact", StringComparison.Ordinal) ? 12 : 24;
        var step = 24 / hours;
        var middle = Enumerable.Range(0, hours).Select(index => 21.5 + 2.6 * Math.Sin((index * step - 9) * Math.PI / 12)).ToArray();
        var expected = ChartSeries.CreateRangeBand(new ChartSeriesId("expected-range"), "Expected range",
            middle.Select((value, index) => new ChartRangeBand(index + 1, Math.Round(value - 1.4, 1), Math.Round(value + 1.4, 1))));
        var observed = ChartSeries.CreateLine(new ChartSeriesId("observed"), "Observed",
            middle.Select((value, index) => new ChartPoint(index + 1,
                Math.Round(value + Math.Sin(index * 2.3) * .8 + (index * step is >= 14 and <= 16 ? 2.6 : 0), 1))));
        return Chart.Create().WithXLabels(Enumerable.Range(0, hours).Select(index => (index * step).ToString("00", CultureInfo.InvariantCulture) + ":00").ToArray())
            .WithYAxis("Temperature (°C)").AddSeries(expected).AddSeries(observed)
            .WithValueFormat(ChartValueFormat.Number("0.0", CultureInfo.InvariantCulture));
    }
}

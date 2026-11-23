/// <summary>Updates a bounded live series by observation identity while preserving existing point presentation.</summary>
public static class KeyedPointChangesRecipe {
    /// <summary>Creates the resulting rolling window after a keyed append, replacement and removal.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var readings = ChartSeries.CreateLine(new ChartSeriesId("readings"), "Readings",
            new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) })
            .ObservationIds(new[] { new ChartObservationId("a"), new ChartObservationId("b"), new ChartObservationId("c") })
            .PointLabel(1, "Retained observation");
        var chart = Chart.Create().Theme(VisualTheme.Graphite()).ThemeMode(mode)
            .Title("A rolling window with stable observations")
            .XAxis(x => x.Title("Sample")).YAxis(y => y.Title("Reading")).AddSeries(readings);
        readings.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Remove(new ChartObservationId("a")),
            ChartObservationChange<ChartPoint>.Replace(new ChartObservationId("b"), new ChartPoint(2, 25)),
            ChartObservationChange<ChartPoint>.Append(new ChartObservationId("d"), new ChartPoint(4, 40))
        }, maximumObservations: 3);
        return chart;
    }
}

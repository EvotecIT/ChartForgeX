/// <summary>Identified progress rows retain their own measurement domain and source units.</summary>
public static class IndependentProgressRecipe {
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var rows = ChartSeries.CreateProgressBars(new ChartSeriesId("delivery"), "Delivery", new[] {
            new ChartProgressItem("Reviewed fraction", .72, 0, 1),
            new ChartProgressItem("Verified jobs", 1250, 0, 2000),
            new ChartProgressItem("Temperature change", -8, -20, 20)
        }).ObservationId(0, new ChartObservationId("reviewed"))
            .ObservationId(1, new ChartObservationId("verified"))
            .ObservationId(2, new ChartObservationId("temperature"));
        return Chart.Create().Size(variant == "compact" ? 390 : 760, 340).Title("Progress in each row's units")
            .Theme(VisualTheme.Graphite()).ThemeMode(mode).Legend(false).AddSeries(rows);
    }
}

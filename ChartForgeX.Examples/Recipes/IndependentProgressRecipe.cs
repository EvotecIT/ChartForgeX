/// <summary>Identified progress rows retain their own measurement domain and source units.</summary>
public static class IndependentProgressRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var rows = ChartSeries.CreateProgressBars(new ChartSeriesId("delivery"), "Delivery", new[] {
            new ChartProgressItem("Reviewed fraction", .72, 0, 1),
            new ChartProgressItem("Verified jobs", 1250, 0, 2000),
            new ChartProgressItem("Temperature change", -8, -20, 20)
        }).WithObservationId(0, new ChartObservationId("reviewed"))
            .WithObservationId(1, new ChartObservationId("verified"))
            .WithObservationId(2, new ChartObservationId("temperature"));
        return Chart.Create().WithSize(variant == "compact" ? 390 : 760, 340).WithTitle("Progress in each row's units")
            .WithTheme(VisualTheme.Graphite(), mode).WithLegend(false).AddSeries(rows)
            .WithValueFormat(ChartValueFormat.Number("0.##"));
    }
}

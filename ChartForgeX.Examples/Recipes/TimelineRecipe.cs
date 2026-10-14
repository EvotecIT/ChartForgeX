/// <summary>Three date intervals with stable task identities and an explicitly configured time axis.</summary>
public static class TimelineRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var discovery = new ChartSeries(new ChartSeriesId("discovery"), "Discovery", ChartSeriesKind.Timeline,
            new[] { new ChartPoint(start.ToOADate(), start.AddDays(3).ToOADate()) });
        var implementation = new ChartSeries(new ChartSeriesId("implementation"), "Implementation", ChartSeriesKind.Timeline,
            new[] { new ChartPoint(start.AddDays(2).ToOADate(), start.AddDays(7).ToOADate()) });
        var validation = new ChartSeries(new ChartSeriesId("validation"), "Validation", ChartSeriesKind.Timeline,
            new[] { new ChartPoint(start.AddDays(6).ToOADate(), start.AddDays(9).ToOADate()) });
        return Chart.Create().AddSeries(discovery).AddSeries(implementation).AddSeries(validation)
            .ConfigureXAxis(axis => {
                axis.Scale = ChartScaleKind.Time;
                axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("MMM d", CultureInfo.InvariantCulture));
            }).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

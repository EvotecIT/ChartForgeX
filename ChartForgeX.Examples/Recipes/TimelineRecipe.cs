/// <summary>Overlapping project activities in March 2026 with stable task identities and an explicitly configured time axis.</summary>
public static class TimelineRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create();
        // Each activity starts and ends a whole number of days after the kick-off on Monday 2 March.
        foreach (var (id, name, from, to) in new[] {
            ("discovery", "Discovery", 0, 3), ("design", "Service design", 2, 7), ("implementation", "Implementation", 4, 15),
            ("migration", "Data migration", 9, 16), ("validation", "Validation", 14, 19), ("training", "User training", 16, 21),
            ("rollout", "Rollout", 19, 23) })
            chart.AddSeries(new ChartSeries(new ChartSeriesId(id), name, ChartSeriesKind.Timeline,
                new[] { new ChartPoint(start.AddDays(from).ToOADate(), start.AddDays(to).ToOADate()) }));
        return chart.ConfigureXAxis(axis => {
            axis.Scale = ChartScaleKind.Time;
            axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("MMM d", CultureInfo.InvariantCulture));
        });
    }
}

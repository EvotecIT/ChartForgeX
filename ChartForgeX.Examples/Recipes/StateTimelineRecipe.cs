/// <summary>Availability intervals retain state keys, details and lane grouping.</summary>
public static class StateTimelineRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var primary = ChartSeries.CreateStateTimelineLane(new ChartSeriesId("primary"), "Primary", new[] {
            new ChartStateTimelineSegment(start, start.AddHours(3), "pass"),
            new ChartStateTimelineSegment(start.AddHours(3), start.AddHours(5), "warning", "Increased response time"),
            new ChartStateTimelineSegment(start.AddHours(6), start.AddHours(10), "pass")
        }, group: "Production");
        var secondary = ChartSeries.CreateStateTimelineLane(new ChartSeriesId("secondary"), "Secondary", new[] {
            new ChartStateTimelineSegment(start, start.AddHours(6), "pass"),
            new ChartStateTimelineSegment(start.AddHours(7), start.AddHours(10), "critical", "No response")
        }, group: "Production");
        return Chart.Create().WithStateCategories(new ChartStateCategory("pass", "Available", colors.Status.Pass.Fill),
            new ChartStateCategory("warning", "Review", colors.Status.Medium.Fill, ChartStatePattern.Hatched),
            new ChartStateCategory("critical", "Unavailable", colors.Status.Critical.Fill, ChartStatePattern.CrossHatched))
            .AddSeries(primary).AddSeries(secondary).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

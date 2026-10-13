/// <summary>Overlapping and open incidents share a deliberate current instant.</summary>
public static class GanttLaneRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var primary = ChartSeries.CreateGanttLane(new ChartSeriesId("primary"), "Primary", new[] {
            new ChartGanttLaneItem(start, start.AddHours(5), "warning", "Investigation", "Degraded response time"),
            new ChartGanttLaneItem(start.AddHours(3), start.AddHours(7), "critical", "Recovery"),
            new ChartGanttLaneItem(start.AddHours(9), null, "warning", "Monitoring")
        }, "Production");
        var secondary = ChartSeries.CreateGanttLane(new ChartSeriesId("secondary"), "Secondary", new[] {
            new ChartGanttLaneItem(start.AddHours(2), start.AddHours(6), "warning", "Maintenance")
        }, "Production");
        return Chart.Create().WithStateCategories(new ChartStateCategory("pass", "Available", colors.Status.Pass.Fill),
            new ChartStateCategory("warning", "Review", colors.Status.Medium.Fill, ChartStatePattern.Hatched),
            new ChartStateCategory("critical", "Unavailable", colors.Status.Critical.Fill, ChartStatePattern.CrossHatched))
            .WithGanttLaneNow(start.AddHours(12)).AddSeries(primary).AddSeries(secondary)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

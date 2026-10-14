/// <summary>Tasks, progress and a milestone connected through chart-owned dependency handles.</summary>
public static class GanttRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var start = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var discovery = ChartSeries.CreateGanttTask(new ChartSeriesId("discovery"), "Discovery", start, start.AddDays(3), .9);
        var implementation = ChartSeries.CreateGanttTask(new ChartSeriesId("implementation"), "Implementation", start.AddDays(3), start.AddDays(7), .55);
        var validation = ChartSeries.CreateGanttTask(new ChartSeriesId("validation"), "Validation", start.AddDays(7), start.AddDays(9), .1);
        var delivery = ChartSeries.CreateGanttMilestone(new ChartSeriesId("delivery"), "Delivery", start.AddDays(9));
        return Chart.Create().AddSeries(discovery).AddSeries(implementation).AddSeries(validation).AddSeries(delivery)
            .AddGanttDependency(discovery, implementation).AddGanttDependency(implementation, validation).AddGanttDependency(validation, delivery)
            .ConfigureXAxis(axis => {
                axis.Scale = ChartScaleKind.Time;
                axis.ValueFormat = ChartValueFormat.Custom(value => DateTime.FromOADate(value).ToString("MMM d", CultureInfo.InvariantCulture));
            }).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

/// <summary>Formats daily temperature independently from the calendar's numeric observations.</summary>
public static class CalendarHeatmapColorDimensionRecipe {
    /// <summary>Creates a temperature calendar with Polish number formatting and a named color dimension.</summary>
    public static Chart Create(string variant = "color-dimension", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("color-dimension" or "compact-color-dimension")) throw new ArgumentOutOfRangeException(nameof(variant));
        var colors = VisualTheme.Graphite().Resolve(mode);
        return Chart.Create().ConfigureLabels(labels => labels.NoData = "Not sampled")
            .ConfigureCalendar(options => options.FirstDayOfWeek = DayOfWeek.Monday)
            .AddCalendarHeatmap("Daily mean", Enumerable.Range(0, 120)
                .Where(index => index % 7 != 0 && index % 9 != 0)
                .Select(index => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1).AddDays(index), index * 2.5 % 13)),
                id: new ChartSeriesId("daily-mean"))
            .WithHeatmapColorScale(ChartColorScale.Sequential(colors.SequentialRamp).WithValueRange(0, 12)
                .WithLabels("Cool", null, "Warm").WithNoDataColor(colors.Border))
            .ConfigureHeatmapColorDimension(options => {
                options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Number("0.0 '°C'", CultureInfo.GetCultureInfo("pl-PL"));
                options.LegendStyle = ChartColorLegendStyle.Auto;
            });
    }
}

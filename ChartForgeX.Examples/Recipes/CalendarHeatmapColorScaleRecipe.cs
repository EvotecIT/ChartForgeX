/// <summary>Colors dated activity from quiet to busy on a fixed numeric scale.</summary>
public static class CalendarHeatmapColorScaleRecipe {
    /// <summary>Creates a Monday-first calendar with an explicit activity range and missing-data color.</summary>
    public static Chart Create(string variant = "color-scale", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("color-scale" or "compact-color-scale")) throw new ArgumentOutOfRangeException(nameof(variant));
        var colors = VisualTheme.Graphite().Resolve(mode);
        return Chart.Create().ConfigureCalendar(options => options.FirstDayOfWeek = DayOfWeek.Monday)
            .AddCalendarHeatmap("Daily observations", Enumerable.Range(0, 120)
                .Where(index => index % 7 != 0 && index % 9 != 0)
                .Select(index => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1).AddDays(index), index * 7 % 13)),
                id: new ChartSeriesId("daily-observations"))
            .WithHeatmapColorScale(ChartColorScale.Sequential(colors.SequentialRamp).WithValueRange(0, 12)
                .WithLabels("Quiet", null, "Busy").WithNoDataColor(colors.Border))
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

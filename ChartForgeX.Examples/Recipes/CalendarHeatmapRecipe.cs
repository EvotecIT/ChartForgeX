/// <summary>Shows dated activity in a calendar heatmap with Monday-first weeks.</summary>
public static class CalendarHeatmapRecipe {
    /// <summary>Creates four months of observations, or a shorter sparse window.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options")) throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create().AddCalendarHeatmap("Observations", Enumerable.Range(0, variant == "sparse" ? 35 : 120)
                .Where(index => index % 7 != 0 && index % 9 != 0)
                .Select(index => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1).AddDays(index), index * 7 % 13)),
                firstDayOfWeek: DayOfWeek.Monday, id: new ChartSeriesId("daily-observations"))
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

/// <summary>Builds a filled area showing weekly capacity use.</summary>
public static class AreaRecipe {
    /// <summary>Creates the selected area example, including the warning-pattern option.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var points = Enumerable.Range(1, 6).Where(index => variant != "sparse" || index is 1 or 3 or 6)
            .Select(index => new ChartPoint(index, 20 + index * 7 % 29, variant == "sparse" && index == 6));
        var capacity = ChartSeries.CreateArea(new ChartSeriesId("used-capacity"), "Used capacity", points);
        var chart = Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat").AddSeries(capacity);
        if (variant == "options") {
            capacity.WithFillPattern(ChartFillPattern.Crosshatch).WithState(ChartSeriesState.Warning);
            chart.WithDataLabels();
        }
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

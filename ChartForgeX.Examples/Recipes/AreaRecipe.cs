/// <summary>Builds a filled area showing weekly capacity use.</summary>
public static class AreaRecipe {
    /// <summary>Creates the selected area example, including the warning-pattern option.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        if (variant == "sparse") {
            var sparse = Enumerable.Range(1, 6).Where(index => index is 1 or 3 or 6)
                .Select(index => new ChartPoint(index, 20 + index * 7 % 29, index == 6));
            return Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
                .AddSeries(ChartSeries.CreateArea(new ChartSeriesId("used-capacity"), "Used capacity", sparse));
        }
        // Two weeks of peak cluster capacity in use (%): busy weekdays, quiet weekends and a release on day 10.
        var points = Enumerable.Range(0, 14).Select(index => new ChartPoint(index + 1,
            Math.Round((index % 7 >= 5 ? 34 : 58) + index * .9 + Math.Sin(index * 1.9) * 6 + (index == 9 ? 14 : 0))));
        var capacity = ChartSeries.CreateArea(new ChartSeriesId("used-capacity"), "Used capacity", points);
        var chart = Chart.Create().WithXLabels(Enumerable.Range(0, 14).Select(index => (index + 2) + " Nov").ToArray())
            .WithYAxis("Capacity used (%)").WithYAxisBounds(0, 100).AddSeries(capacity);
        if (variant == "options") {
            capacity.WithFillPattern(ChartFillPattern.Crosshatch).WithState(ChartSeriesState.Warning);
            chart.WithDataLabels();
        }
        return chart;
    }
}

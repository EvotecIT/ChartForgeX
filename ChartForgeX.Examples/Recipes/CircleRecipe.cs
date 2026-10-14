/// <summary>A single completed-assessment measure on its full circular scale.</summary>
public static class CircleRecipe {
    /// <summary>Creates the completed assessment value on a zero-to-one-hundred scale.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var completed = ChartSeries.CreateCircle(new ChartSeriesId("completed"), "Completed", 76);
        return Chart.Create().WithXLabels("North", "South", "East", "West", "Central", "Online").AddSeries(completed)
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}

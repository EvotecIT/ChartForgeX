/// <summary>A single completed-assessment measure on its full circular scale.</summary>
public static class CircleRecipe {
    /// <summary>Creates the completed assessment value on a zero-to-one-hundred scale.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var completed = ChartSeries.CreateCircle(new ChartSeriesId("completed"), "Completed", 76);
        return Chart.Create().XLabels("North", "South", "East", "West", "Central", "Online").AddSeries(completed);
    }
}

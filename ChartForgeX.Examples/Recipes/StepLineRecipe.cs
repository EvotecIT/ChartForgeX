/// <summary>Builds a stepped line showing requests waiting in a queue.</summary>
public static class StepLineRecipe {
    /// <summary>Creates the selected stepped-line example.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var points = Enumerable.Range(1, 6).Where(index => variant != "sparse" || index is 1 or 3 or 6)
            .Select(index => new ChartPoint(index, 20 + index * 7 % 29, variant == "sparse" && index == 6));
        var queued = new ChartSeries(new ChartSeriesId("queued"), "Queued", ChartSeriesKind.StepLine, points);
        return Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat").AddSeries(queued)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

/// <summary>Builds a stepped area showing reserved processing capacity.</summary>
public static class StepAreaRecipe {
    /// <summary>Creates the selected stepped-area example.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var points = Enumerable.Range(1, 6).Where(index => variant != "sparse" || index is 1 or 3 or 6)
            .Select(index => new ChartPoint(index, 20 + index * 7 % 29, variant == "sparse" && index == 6));
        var reserved = new ChartSeries(new ChartSeriesId("reserved-capacity"), "Reserved capacity", ChartSeriesKind.StepArea, points);
        return Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat").AddSeries(reserved)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

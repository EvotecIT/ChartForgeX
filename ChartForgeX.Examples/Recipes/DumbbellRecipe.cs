/// <summary>Builds a before-and-after comparison of service capacity.</summary>
public static class DumbbellRecipe {
    /// <summary>Creates the selected dumbbell example from typed endpoint pairs.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var count = variant == "sparse" ? 2 : 5;
        var changes = Enumerable.Range(1, count).Select(index => new ChartDumbbell(index, 20 + index * 2, 40 + index * 3));
        var change = ChartSeries.CreateDumbbell(new ChartSeriesId("change"), "Change", changes);
        if (variant != "sparse") change.WithPointLabel(2, "Premium regional service capacity").WithDataLabels();
        return Chart.Create().WithXLabels("Standard", "Express", "Premium", "Bulk", "Return").AddSeries(change)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

/// <summary>Builds a two-position capacity comparison for three teams.</summary>
public static class SlopeRecipe {
    /// <summary>Creates the selected slope example with explicit endpoint labels.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var teamA = new ChartSeries(new ChartSeriesId("team-a"), "Team A", ChartSeriesKind.Slope, new[] { new ChartPoint(1, 28), new ChartPoint(2, 64) });
        var teamB = new ChartSeries(new ChartSeriesId("team-b"), "Team B", ChartSeriesKind.Slope, new[] { new ChartPoint(1, 52), new ChartPoint(2, 43) });
        var teamC = new ChartSeries(new ChartSeriesId("team-c"), "Team C", ChartSeriesKind.Slope, new[] { new ChartPoint(1, 38), new ChartPoint(2, 57) });
        return Chart.Create().WithXLabels("Before", "After").AddSeries(teamA).AddSeries(teamB).AddSeries(teamC)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}

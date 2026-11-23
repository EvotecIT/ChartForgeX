/// <summary>Observed service levels compared with expected values or a marked target line.</summary>
public static class RadarRecipe {
    /// <summary>Creates shared radar categories with marker options or reversed angular order.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "angular-reversal" or "compact-angular-reversal"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        if (variant is "angular-reversal" or "compact-angular-reversal") return Reversed();
        if (variant is "options" or "compact-options") return Markers();
        var observed = new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.Radar, Observations(variant));
        var expected = new ChartSeries(new ChartSeriesId("expected"), "Expected", ChartSeriesKind.Radar, Observations(variant == "sparse" ? "wide" : variant, 12));
        observed.Radar(options => options.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Gap);
        return Chart.Create().XLabels("Speed", "Coverage", "Capacity", "Quality", "Recovery", "Support")
            .AddSeries(observed).AddSeries(expected);
    }

    private static Chart Markers() {
        var observed = new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.Radar, Points(58, 76, 69, 84, 62, 73))
            .Radar(options => options.FillOpacity = .2)
            .Markers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Enabled = true; });
        var target = new ChartSeries(new ChartSeriesId("target"), "Target", ChartSeriesKind.Radar, Points(75, 85, 80, 90, 75, 85))
            .Radar(options => options.Form = ChartLineAreaForm.Line)
            .Markers(markers => { markers.Shape = ChartMarkerShape.Square; markers.Enabled = true; });
        return Chart.Create().XLabels("Speed", "Coverage", "Capacity", "Quality", "Recovery", "Support")
            .YAxis(y => y.Bounds(0, 100)).AddSeries(observed).AddSeries(target);
    }

    private static Chart Reversed() {
        var observed = new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.Radar, Points(58, 76, 69, 84))
            .Radar(options => options.FillOpacity = .2)
            .Markers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Radius = 5; });
        var target = new ChartSeries(new ChartSeriesId("target"), "Target", ChartSeriesKind.Radar, Points(75, 85, 80, 90))
            .Radar(options => options.Form = ChartLineAreaForm.Line)
            .Markers(markers => { markers.Shape = ChartMarkerShape.Square; markers.Radius = 4; });
        return Chart.Create().XLabels("North", "East", "South", "West").YAxis(y => y.Bounds(0, 100)).DataLabels(false)
            .AddSeries(observed).AddSeries(target).XAxis(axis => axis.Reversed());
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
    private static ChartPoint[] Observations(string variant, double offset = 0) => (variant == "sparse" ? new[] { 1, 3, 6 } : Enumerable.Range(1, 6))
        .Select(index => new ChartPoint(index, 20 + offset + index * 7 % 29)).ToArray();
}

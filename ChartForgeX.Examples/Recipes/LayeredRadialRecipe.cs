/// <summary>Three independently sized radial layers for reviewed, verified and complete work.</summary>
public static class LayeredRadialRecipe {
    /// <summary>Creates three completion layers with different radii, sweeps and separators.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var completion = ChartSeries.CreateLayeredRadial(new ChartSeriesId("completion"), "Completion", new[] {
            new ChartRadialLayer("Reviewed", 76) { RadiusRatio = 1, StrokeRatio = .13, SweepAngleDegrees = 300, StartAngleDegrees = -60 },
            new ChartRadialLayer("Verified", 58) { RadiusRatio = .7, StrokeRatio = .13, SweepAngleDegrees = 240, StartAngleDegrees = -30, SeparatorCount = 8 },
            new ChartRadialLayer("Complete", 42) { RadiusRatio = .4, StrokeRatio = .13, SweepAngleDegrees = 360 }
        });
        return Chart.Create().WithXLabels("North", "South", "East", "West", "Central", "Online").AddSeries(completion)
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}

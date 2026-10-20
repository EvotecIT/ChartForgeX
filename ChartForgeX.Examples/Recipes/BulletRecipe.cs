/// <summary>Reviewed and verified work compared with separate targets and qualitative ranges.</summary>
public static class BulletRecipe {
    /// <summary>Creates two measured values, each with its own target and range boundaries.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "dense" or "compact-dense"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        if (variant is "dense" or "compact-dense") {
            var values = new[] { 0d, 2611.111, 3722.222, 4833.333, 5944.444, 7055.555, 8166.666, 12345.6789 };
            var chart = Chart.Create().WithTitle("Service completion against target").WithDataLabels();
            for (var index = 0; index < values.Length; index++) chart.AddBullet("Regional service " + (index + 1), values[index], 9500.125,
                0, 10000, new[] { 4000d, 8000 });
            return chart.WithValueFormat(ChartValueFormat.Number("0.0000", System.Globalization.CultureInfo.InvariantCulture));
        }
        var reviewed = ChartSeries.CreateBullet(new ChartSeriesId("reviewed"), "Reviewed", 76, 85, rangeEnds: new[] { 50d, 80d });
        var verified = ChartSeries.CreateBullet(new ChartSeriesId("verified"), "Verified", 58, 75, rangeEnds: new[] { 40d, 70d });
        return Chart.Create().WithXLabels("North", "South", "East", "West", "Central", "Online").AddSeries(reviewed).AddSeries(verified)
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}

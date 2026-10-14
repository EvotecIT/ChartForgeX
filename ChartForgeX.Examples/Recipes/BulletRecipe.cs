/// <summary>Reviewed and verified work compared with separate targets and qualitative ranges.</summary>
public static class BulletRecipe {
    /// <summary>Creates two measured values, each with its own target and range boundaries.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var reviewed = ChartSeries.CreateBullet(new ChartSeriesId("reviewed"), "Reviewed", 76, 85, rangeEnds: new[] { 50d, 80d });
        var verified = ChartSeries.CreateBullet(new ChartSeriesId("verified"), "Verified", 58, 75, rangeEnds: new[] { 40d, 70d });
        return Chart.Create().WithXLabels("North", "South", "East", "West", "Central", "Online").AddSeries(reviewed).AddSeries(verified)
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}

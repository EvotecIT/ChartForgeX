/// <summary>Regional review rates compared with separate targets and qualitative ranges.</summary>
public static class BulletRecipe {
    /// <summary>Creates one measured value per region, each with its own target and range boundaries.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "dense" or "compact-dense"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        if (variant is "dense" or "compact-dense") {
            var values = new[] { 0d, 2611.111, 3722.222, 4833.333, 5944.444, 7055.555, 8166.666, 12345.6789 };
            var chart = Chart.Create().WithTitle("Service completion against target").WithDataLabels();
            for (var index = 0; index < values.Length; index++) chart.AddBullet("Regional service " + (index + 1), values[index], 9500.125,
                0, 10000, new[] { 4000d, 8000 });
            return chart;
        }
        // Share of each region's October cases reviewed within two working days, against its own target;
        // the bands mark poor and acceptable results for that region.
        var regions = new (string Id, string Name, double Reviewed, double Target, double Poor, double Acceptable)[] {
            ("north", "North", 86, 90, 60, 80), ("south", "South", 74, 85, 55, 75), ("east", "East", 91, 90, 60, 80),
            ("west", "West", 68, 80, 50, 70), ("central", "Central", 79, 85, 55, 75), ("online", "Online", 95, 92, 70, 85)
        };
        var review = Chart.Create().WithValueFormat(ChartValueFormat.Number("0'%'", CultureInfo.InvariantCulture));
        foreach (var region in variant == "sparse" ? regions.Take(2) : regions)
            review.AddSeries(ChartSeries.CreateBullet(new ChartSeriesId(region.Id), region.Name, region.Reviewed, region.Target,
                rangeEnds: new[] { region.Poor, region.Acceptable }));
        return review;
    }
}

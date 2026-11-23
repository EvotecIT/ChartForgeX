/// <summary>Request counts and resolution rates on independent radial value scales.</summary>
public static class RadialBarScalesRecipe {
    /// <summary>Creates count and percentage measures from a rightward start ray.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "dual-axes" or "compact-dual-axes")) throw new ArgumentOutOfRangeException(nameof(variant));
        var requests = new ChartSeries(new ChartSeriesId("requests"), "Requests", ChartSeriesKind.RadialBar, new[] { new ChartPoint(1, 800) });
        var resolved = new ChartSeries(new ChartSeriesId("resolved"), "Resolved (%)", ChartSeriesKind.RadialBar, new[] { new ChartPoint(1, 85) })
            .SecondaryAxis();
        var chart = Chart.Create().XLabels("Overall").DataLabels(false)
            .RadialGeometry(new ChartRadialGeometryOptions(0, 270, .2, .25, .1)).AddSeries(requests).AddSeries(resolved)
            .YAxis(y => y.Bounds(0, 1000)).SecondaryYAxis(s => s.Bounds(0, 100).Title("Resolved (%)").LabelBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%"));
        chart.Options.YAxis.TickCount = 5;
        chart.Options.SecondaryYAxis.TickCount = 5;
        return chart;
    }
}

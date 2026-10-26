/// <summary>Uses real typed intervals and native paired boundaries without an adapter dependency.</summary>
public static class NativeRangeComponentRecipe {
    /// <summary>Creates authored fixed-axis range observations with retained disconnected segments and caller-owned fonts.</summary>
    public static Chart Create(double lower, double upper, bool area, int width, VisualThemeMode mode, string family) {
        var chart = Chart.Create().WithTitle("Native interval values").WithSize(width, 420).WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(family).WithDataLabels(true).WithLegend(true);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var values = new[] { new ChartRangeBand(.5, key == "delivery" ? lower : 35, key == "delivery" ? upper : 70), new ChartRangeBand(1.5, 25, 55), new ChartRangeBand(3, 30, 65, true) };
            var series = area ? ChartSeries.CreateRangeArea(new ChartSeriesId(key), key, values) : ChartSeries.CreateRangeBand(new ChartSeriesId(key), key, values);
            series.WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later"), new ChartObservationId("disconnected") }); chart.AddSeries(series);
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
}

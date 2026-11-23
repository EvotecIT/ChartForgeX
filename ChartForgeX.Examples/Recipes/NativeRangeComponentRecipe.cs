/// <summary>Uses real typed intervals and native paired boundaries without an adapter dependency.</summary>
public static class NativeRangeComponentRecipe {
    /// <summary>Creates authored fixed-axis range observations with retained disconnected segments and caller-owned fonts.</summary>
    public static Chart Create(double lower, double upper, bool area, int width, ThemeMode mode, string family) {
        var chart = Chart.Create().Title("Native interval values").Size(width, 420).Theme(VisualTheme.Graphite()).ThemeMode(mode).FontFamily(family).DataLabels(true).Legend(true);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var values = new[] { new ChartRangeBand(.5, key == "delivery" ? lower : 35, key == "delivery" ? upper : 70), new ChartRangeBand(1.5, 25, 55), new ChartRangeBand(3, 30, 65, true) };
            var series = area ? ChartSeries.CreateRangeArea(new ChartSeriesId(key), key, values) : ChartSeries.CreateRangeBand(new ChartSeriesId(key), key, values);
            series.ObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later"), new ChartObservationId("disconnected") }); chart.AddSeries(series);
        }
        chart.XAxis(axis => { axis.Min = 0; axis.Max = 4; }); chart.YAxis(axis => { axis.Min = 0; axis.Max = 100; }); return chart;
    }
}

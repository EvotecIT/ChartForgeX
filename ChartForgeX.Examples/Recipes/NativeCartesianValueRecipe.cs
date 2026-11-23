/// <summary>Authored fixed-category Line/Bar readings for detached native value presentation.</summary>
public static class NativeCartesianValueRecipe {
    public static Chart Create(bool bar = false, double value = 20, int width = 760, ThemeMode mode = ThemeMode.Light) {
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, value), new ChartPoint(3, 60) };
        var series = bar ? ChartSeries.CreateBar(new ChartSeriesId("delivery"), "Delivery", points) : ChartSeries.CreateLine(new ChartSeriesId("delivery"), "Delivery", points);
        series.ObservationId(0, new ChartObservationId("opened")).ObservationId(1, new ChartObservationId("reviewed")).ObservationId(2, new ChartObservationId("released"));
        return Chart.Create().AddSeries(series).Size(width, 420).Theme(VisualTheme.Graphite()).ThemeMode(mode).Title("Reviewed work")
            .Legend(false).DataLabels(false).XLabels("Opened", "Reviewed", "Released").XAxis(x => x.Bounds(.5, 3.5)).YAxis(y => y.Bounds(-100, 100))
            .Lines(l => l.MarkerMode(ChartLineMarkerMode.All)).Bars(b => b.VisualStyle(ChartBarVisualStyle.Solid()));
    }
}

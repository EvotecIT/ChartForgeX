/// <summary>Authored fixed-category Line/Bar readings for detached native value presentation.</summary>
public static class NativeCartesianValueRecipe {
    public static Chart Create(bool bar = false, double value = 20, int width = 760, VisualThemeMode mode = VisualThemeMode.Light) {
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, value), new ChartPoint(3, 60) };
        var series = bar ? ChartSeries.CreateBar(new ChartSeriesId("delivery"), "Delivery", points) : ChartSeries.CreateLine(new ChartSeriesId("delivery"), "Delivery", points);
        series.WithObservationId(0, new ChartObservationId("opened")).WithObservationId(1, new ChartObservationId("reviewed")).WithObservationId(2, new ChartObservationId("released"));
        return Chart.Create().AddSeries(series).WithSize(width, 420).WithTheme(VisualTheme.Graphite(), mode).WithTitle("Reviewed work")
            .WithLegend(false).WithDataLabels(false).WithXLabels("Opened", "Reviewed", "Released").WithXAxisBounds(.5, 3.5).WithYAxisBounds(-100, 100)
            .WithLineMarkers(ChartLineMarkerMode.All).WithBarVisualStyle(ChartBarVisualStyle.Solid());
    }
}

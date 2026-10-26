/// <summary>Uses real typed observations and a fixed native value/size domain without an HTML dependency.</summary>
public static class NativeBubbleComponentRecipe {
    /// <summary>Creates two independently identified Bubble bands with caller-owned typography.</summary>
    public static Chart Create(double y, double size, bool area, bool reversed, int width, VisualThemeMode mode, string family) {
        var chart = Chart.Create().WithTitle("Authored Bubble Y and Size").WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(family).WithDataLabels(true).WithLegend(true);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var series = ChartSeries.CreateBubble(new ChartSeriesId(key), key, new[] {
                new ChartBubble(1, key == "delivery" ? y : 65, key == "delivery" ? size : 35).WithColorValue(7),
                new ChartBubble(3, 45, 25).WithColorValue(11) })
                .WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") });
            series.ConfigureMarkers(markers => markers.Shape = area ? ChartMarkerShape.Diamond : ChartMarkerShape.Circle);
            chart.AddSeries(series);
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; });
        chart.ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.MinimumRadius = 6; options.MaximumRadius = 18;
            options.SizeEncoding = area ? ChartBubbleSizeEncoding.Area : ChartBubbleSizeEncoding.SquareRootRadius; options.Reversed = reversed;
            options.SizeDimension.Name = "Effort"; });
        return chart;
    }
}

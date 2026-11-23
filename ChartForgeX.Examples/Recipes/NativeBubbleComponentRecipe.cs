/// <summary>Uses real typed observations and a fixed native value/size domain without an HTML dependency.</summary>
public static class NativeBubbleComponentRecipe {
    /// <summary>Creates two independently identified Bubble bands with caller-owned typography.</summary>
    public static Chart Create(double y, double size, bool area, bool reversed, int width, ThemeMode mode, string family) {
        var chart = Chart.Create().Title("Authored Bubble Y and Size").Size(width, 420)
            .Theme(VisualTheme.Graphite()).ThemeMode(mode).FontFamily(family).DataLabels(true).Legend(true);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var series = ChartSeries.CreateBubble(new ChartSeriesId(key), key, new[] {
                new ChartBubble(1, key == "delivery" ? y : 65, key == "delivery" ? size : 35).WithColorValue(7),
                new ChartBubble(3, 45, 25).WithColorValue(11) })
                .ObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") });
            series.Markers(markers => markers.Shape = area ? ChartMarkerShape.Diamond : ChartMarkerShape.Circle);
            chart.AddSeries(series);
        }
        chart.XAxis(axis => { axis.Min = 0; axis.Max = 4; }); chart.YAxis(axis => { axis.Min = 0; axis.Max = 100; });
        chart.Bubble(options => { options.SizeDomain(0, 100); options.MinimumRadius = 6; options.MaximumRadius = 18;
            options.SizeEncoding = area ? ChartBubbleSizeEncoding.Area : ChartBubbleSizeEncoding.SquareRootRadius; options.Reversed = reversed;
            options.SizeDimension.Name = "Effort"; });
        return chart;
    }
}

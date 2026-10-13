/// <summary>Authored requests and utilization retain outside observations while selecting X 2–8 for rendering.</summary>
public static class LineViewportRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var compact = variant.StartsWith("compact", StringComparison.Ordinal);
        var observed = new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.Line,
            Points(88, 44, 34, 56, 46, 64, 42, 58, 50, 38, 82)).ConfigureMarkers(markers => markers.Radius = 3);

        var chart = Chart.Create().WithXAxis("Sample").WithYAxis("Requests").WithXAxisBounds(0, 10).WithDataLabels().AddSeries(observed);
        if (!compact) {
            observed.WithDataLabels(false);
            var utilization = new ChartSeries(new ChartSeriesId("utilization"), "Utilization", ChartSeriesKind.Line,
                Points(96, 72, 48, 61, 55, 76, 49, 67, 59, 84, 100))
                .UseSecondaryYAxis().ConfigureMarkers(markers => markers.Radius = 3);

            chart.AddSeries(utilization).WithSecondaryYAxis("Utilization (%)");
            chart.Options.SecondaryYAxis.TickCount = 4;
        }
        chart.Options.XAxis.WithReversal(false);
        chart.Options.XAxis.TickCount = 4; chart.Options.YAxis.TickCount = 4;
        return chart;
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) =>
        new ChartPoint(index == 1 ? 1.99 : index == 9 ? 8.01 : index, value, index == 6)).ToArray();
}

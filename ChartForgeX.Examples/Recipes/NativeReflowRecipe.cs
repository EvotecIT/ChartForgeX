/// <summary>Finite authored observation/group visibility using native layout and statistics.</summary>
internal static class NativeReflowRecipe {
    internal static Chart Create(string kind, int width, VisualThemeMode mode) {
        ChartSeries series; var id = new ChartSeriesId("delivery");
        if (kind == "raw") {
            series = ChartSeries.CreateBoxPlot(id, "Delivery", new[] { new ChartBoxPlotSampleGroup(1, new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }),
                new ChartBoxPlotSampleGroup(2, new double[] { 2, 3, 4, 5, 6 }), new ChartBoxPlotSampleGroup(3, new double[] { 4, 5, 6, 7, 8 }) });
            series.WithBoxPlotGroupIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") });
        } else if (kind == "boxes") series = ChartSeries.CreateBoxPlot(id, "Delivery", new[] { new ChartBoxPlot(1, 0, 2, 4, 6, 8), new ChartBoxPlot(2, 2, 3, 4, 5, 6), new ChartBoxPlot(3, 4, 5, 6, 7, 8) });
        else {
            var points = new[] { new ChartPoint(1, 20), new ChartPoint(2, 80), new ChartPoint(3, 40) };
            series = kind == "bar" ? ChartSeries.CreateBar(id, "Delivery", points) : kind == "line" ? ChartSeries.CreateLine(id, "Delivery", points) : throw new ArgumentOutOfRangeException(nameof(kind));
        }
        if (kind != "raw") series.WithObservationIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") });
        var chart = Chart.Create().WithTitle("Native visibility").WithSubtitle("Hide an authored source observation or group")
            .WithSize(width, 420).WithTheme(VisualTheme.Graphite(), mode).WithLegend(true).WithDataLabels(true).AddSeries(series);
        chart.Options.ShowPointLegend = true; return chart;
    }
    internal static ChartVisibilityView View(string kind, bool hidden) => new(hidden ? new[] {
        kind == "raw" ? ChartVisibilityTarget.BoxPlotGroup(new ChartSeriesId("delivery"), new ChartObservationId("current"))
            : ChartVisibilityTarget.Observation(new ChartSeriesId("delivery"), new ChartObservationId("current")) } : Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow);
}

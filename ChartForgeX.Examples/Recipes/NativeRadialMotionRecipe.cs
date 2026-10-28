/// <summary>Positive native Pie/Donut value frames and captured full-source Other visibility.</summary>
internal static class NativeRadialMotionRecipe {
    internal static Chart Create(bool donut, bool other, double value, int width, VisualThemeMode mode) {
        var values = other ? new[] { value, 25d, 10d, 4d, 1d } : new[] { value, 25d, 10d };
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", donut ? ChartSeriesKind.Donut : ChartSeriesKind.Pie,
            values.Select((reading, index) => new ChartPoint(index + 1, reading)))
            .WithObservationIds(values.Select((_, index) => new ChartObservationId(index == 0 ? "primary" : "part-" + index)));
        var chart = Chart.Create().WithTitle("Native radial presentation").WithSubtitle(other ? "Other retains its full source contributors" : "Positive values use complete native frames")
            .WithSize(width, 420).WithTheme(VisualTheme.Graphite(), mode).WithLegend(true).WithDataLabels(true).WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).WithXLabels("Subscriptions", "Consulting", "Support", "Training", "Other services").AddSeries(series);
        chart.Options.MaximumPieSlices = other ? 3 : 8; return chart;
    }
    internal static ChartVisibilityView View(bool hidden) => new(hidden ? new[] { ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts")) } : Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow);
}

/// <summary>Positive native Pie/Donut value frames and captured full-source Other visibility.</summary>
internal static class NativeRadialMotionRecipe {
    internal static Chart Create(bool donut, bool other, double value, int width, ThemeMode mode) {
        var values = other ? new[] { value, 25d, 10d, 4d, 1d } : new[] { value, 25d, 10d };
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", donut ? ChartSeriesKind.Donut : ChartSeriesKind.Pie,
            values.Select((reading, index) => new ChartPoint(index + 1, reading)))
            .ObservationIds(values.Select((_, index) => new ChartObservationId(index == 0 ? "primary" : "part-" + index)));
        var chart = Chart.Create().Title("Native radial presentation").Subtitle(other ? "Other retains its full source contributors" : "Positive values use complete native frames")
            .Size(width, 420).Theme(VisualTheme.Graphite()).ThemeMode(mode).Legend(true).DataLabels(true).DataLabels(d => d.Placement(ChartDataLabelPlacement.Outside))
            .Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)).XLabels("Subscriptions", "Consulting", "Support", "Training", "Other services").AddSeries(series);
        chart.Options.Pie.MaxSlices = other ? 3 : 8; return chart;
    }
    internal static ChartVisibilityView View(bool hidden) => new(hidden ? new[] { ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts")) } : Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow);
}

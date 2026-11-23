/// <summary>Complete native values retain actual area, stack, caption and source-observation ownership.</summary>
public static class NativeCompleteCartesianValueRecipe {
    internal static readonly string[] Variants = { "line-labels", "bar-labels", "step-line", "area", "step-area", "stacked-bar", "normalized-bar", "stacked-area-gap", "stacked-area-zero" };
    /// <summary>Creates one fixed-axis authored example; callers provide their actual retained font family.</summary>
    public static Chart Create(string variant, double reading, int width, ThemeMode mode, string family) {
        if (!Variants.Contains(variant, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(variant));
        var kind = variant is "bar-labels" or "stacked-bar" or "normalized-bar" ? ChartSeriesKind.Bar
            : variant == "step-line" ? ChartSeriesKind.StepLine : variant == "area" ? ChartSeriesKind.Area
            : variant == "step-area" ? ChartSeriesKind.StepArea : variant.StartsWith("stacked-area", StringComparison.Ordinal) ? ChartSeriesKind.StackedArea : ChartSeriesKind.Line;
        var values = new[] { new ChartPoint(0, 20), new ChartPoint(1, reading), new ChartPoint(2, 40) };
        var series = new ChartSeries(new ChartSeriesId("delivery"), "Delivery", kind, values)
            .ObservationIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") });
        var chart = Chart.Create().Title("Captured native area, stacks and labels").Size(width, 420)
            .Theme(VisualTheme.Graphite()).ThemeMode(mode).FontFamily(family).DataLabels(true).Legend(true)
            .Lines(l => l.MarkerMode(ChartLineMarkerMode.All)).AddSeries(series);
        chart.XAxis(axis => { axis.Min = -.5; axis.Max = 2.5; });
        chart.YAxis(axis => { axis.Min = 0; axis.Max = 160; });
        if (variant.StartsWith("stacked-", StringComparison.Ordinal) || variant == "normalized-bar") {
            var sparse = variant.EndsWith("-gap", StringComparison.Ordinal) || variant.EndsWith("-zero", StringComparison.Ordinal);
            var points = sparse ? new[] { new ChartPoint(0, 10), new ChartPoint(2, 20) }
                : new[] { new ChartPoint(0, 10), new ChartPoint(1, 15), new ChartPoint(2, 20) };
            var sibling = new ChartSeries(new ChartSeriesId("comparison"), "Comparison", kind, points)
                .ObservationIds(points.Select((_, index) => new ChartObservationId("comparison-" + index)));
            series.StackGroup("delivery-stack"); sibling.StackGroup("delivery-stack");
            if (variant == "normalized-bar") { series.NormalizedTo(100); sibling.NormalizedTo(100); }
            if (kind == ChartSeriesKind.StackedArea) {
                var missing = variant.EndsWith("-zero", StringComparison.Ordinal) ? ChartStackedAreaMissingDataPolicy.Zero : ChartStackedAreaMissingDataPolicy.Gap;
                series.StackedAreaMissingData(missing); sibling.StackedAreaMissingData(missing);
            } else chart.Options.Bars.ShowTotals = true;
            chart.AddSeries(sibling);
        }
        return chart;
    }
}

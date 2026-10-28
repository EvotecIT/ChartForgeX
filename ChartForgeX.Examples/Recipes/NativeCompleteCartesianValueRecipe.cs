/// <summary>Complete native values retain actual area, stack, caption and source-observation ownership.</summary>
public static class NativeCompleteCartesianValueRecipe {
    internal static readonly string[] Variants = { "line-labels", "bar-labels", "step-line", "area", "step-area", "stacked-bar", "normalized-bar", "stacked-area-gap", "stacked-area-zero" };
    /// <summary>Creates one fixed-axis authored example; callers provide their actual retained font family.</summary>
    public static Chart Create(string variant, double reading, int width, VisualThemeMode mode, string family) {
        if (!Variants.Contains(variant, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(variant));
        var kind = variant is "bar-labels" or "stacked-bar" or "normalized-bar" ? ChartSeriesKind.Bar
            : variant == "step-line" ? ChartSeriesKind.StepLine : variant == "area" ? ChartSeriesKind.Area
            : variant == "step-area" ? ChartSeriesKind.StepArea : variant.StartsWith("stacked-area", StringComparison.Ordinal) ? ChartSeriesKind.StackedArea : ChartSeriesKind.Line;
        var values = new[] { new ChartPoint(0, 20), new ChartPoint(1, reading), new ChartPoint(2, 40) };
        var series = new ChartSeries(new ChartSeriesId("delivery"), "Delivery", kind, values)
            .WithObservationIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") });
        var chart = Chart.Create().WithTitle("Captured native area, stacks and labels").WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(family).WithDataLabels(true).WithLegend(true)
            .WithLineMarkers(ChartLineMarkerMode.All).AddSeries(series);
        chart.ConfigureXAxis(axis => { axis.Minimum = -.5; axis.Maximum = 2.5; });
        chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 160; });
        if (variant.StartsWith("stacked-", StringComparison.Ordinal) || variant == "normalized-bar") {
            var sparse = variant.EndsWith("-gap", StringComparison.Ordinal) || variant.EndsWith("-zero", StringComparison.Ordinal);
            var points = sparse ? new[] { new ChartPoint(0, 10), new ChartPoint(2, 20) }
                : new[] { new ChartPoint(0, 10), new ChartPoint(1, 15), new ChartPoint(2, 20) };
            var sibling = new ChartSeries(new ChartSeriesId("comparison"), "Comparison", kind, points)
                .WithObservationIds(points.Select((_, index) => new ChartObservationId("comparison-" + index)));
            series.WithStackGroup("delivery-stack"); sibling.WithStackGroup("delivery-stack");
            if (variant == "normalized-bar") { series.WithNormalization(100); sibling.WithNormalization(100); }
            if (kind == ChartSeriesKind.StackedArea) {
                var missing = variant.EndsWith("-zero", StringComparison.Ordinal) ? ChartStackedAreaMissingDataPolicy.Zero : ChartStackedAreaMissingDataPolicy.Gap;
                series.WithStackedAreaMissingData(missing); sibling.WithStackedAreaMissingData(missing);
            } else chart.Options.ShowStackTotals = true;
            chart.AddSeries(sibling);
        }
        return chart;
    }
}

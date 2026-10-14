/// <summary>Colors completion values with an observed lower bound and a fixed goal.</summary>
public static class ScatterPartialColorRangeRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "partial-color-range", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("partial-color-range" or "compact-partial-color-range")) throw new ArgumentOutOfRangeException(nameof(variant));
        return PartialColorRange(mode).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static Chart PartialColorRange(VisualThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        var duration = new[] { 18d, 30, 24, 44, 32 };
        var completion = new double?[] { 55, 82, null, 68, 95 };
        var recorded = ChartSeries.CreateScatter(new ChartSeriesId("recorded-batches"), "Recorded batches", completion.Select((value, index) =>
            new ChartPoint((index + 1) * 16, duration[index]).WithColorValue(value)));
        recorded.WithColorScale(ChartColorScale.Sequential(colors.Palette[0], colors.Status.Pass.Fill)
                .WithValueRange(null, 100).WithLabels("Observed minimum", null, "Goal").WithNoDataColor(colors.Border))
            .ConfigureColorDimension(options => {
                options.Name = "Completion"; options.ValueFormat = ChartValueFormat.Number("0'%'", CultureInfo.InvariantCulture);
                options.LegendStyle = ChartColorLegendStyle.Swatches;
            });
        return NumericAxes().ConfigureLabels(labels => labels.NoData = "Not reported").AddSeries(recorded);
    }

    private static Chart NumericAxes() => Chart.Create().WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
        .WithXAxisBounds(0, 100).WithYAxisBounds(0, 60).WithDataLabels(false);
}

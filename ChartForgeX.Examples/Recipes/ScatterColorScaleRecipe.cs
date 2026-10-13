/// <summary>Colors processing observations by temperature and overlays a target duration.</summary>
public static class ScatterColorScaleRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "color-scale", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("color-scale" or "compact-color-scale")) throw new ArgumentOutOfRangeException(nameof(variant));
        return NumericColorScale(mode).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static Chart NumericColorScale(VisualThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        var samples = new[] {
            new { Batch = 15d, Minutes = 28d, Temperature = (double?)-12 },
            new { Batch = 30d, Minutes = 42d, Temperature = (double?)0 },
            new { Batch = 45d, Minutes = 18d, Temperature = (double?)null },
            new { Batch = 60d, Minutes = 35d, Temperature = (double?)12 },
            new { Batch = 80d, Minutes = 22d, Temperature = (double?)35 }
        };
        var recorded = ChartSeries.CreateScatter(new ChartSeriesId("recorded-batches"), "Recorded batches",
            samples.Select(sample => new ChartPoint(sample.Batch, sample.Minutes).WithColorValue(sample.Temperature)));
        recorded.WithColorScale(ChartColorScale.Diverging(new[] { colors.Palette[0] }, colors.Surface,
                new[] { ChartColor.FromHex("#D9A441"), ChartColor.FromHex("#CF6A42"), colors.Status.Critical.Fill }, 0)
                .WithValueRange(-15, 30).WithLabels("Cold", "Freezing", "Warm").WithNoDataColor(colors.Border))
            .ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Circle; markers.StrokeWidth = 1; });
        var target = ChartSeries.CreateLine(new ChartSeriesId("target-duration"), "Target duration", new[] { new ChartPoint(0, 30), new ChartPoint(100, 30) })
            .ConfigureMarkers(markers => markers.Enabled = false);
        return NumericAxes().ConfigureLabels(labels => { labels.Color = "Temperature (°C)"; labels.NoData = "Not sampled"; })
            .AddSeries(recorded).AddSeries(target);
    }

    private static Chart NumericAxes() => Chart.Create().WithXAxis("Batch size (items)").WithYAxis("Processing time (minutes)")
        .WithXAxisBounds(0, 100).WithYAxisBounds(0, 60).WithDataLabels(false);
}

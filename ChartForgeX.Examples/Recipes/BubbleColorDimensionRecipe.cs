/// <summary>Formats bubble temperature separately from processing time and request volume.</summary>
public static class BubbleColorDimensionRecipe {
    /// <summary>Creates the selected example without choosing a renderer or export backend.</summary>
    public static Chart Create(string variant = "color-dimension", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("color-dimension" or "compact-color-dimension")) throw new ArgumentOutOfRangeException(nameof(variant));
        return ColorDimension(mode);
    }

    private static Chart ColorDimension(ThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        var minutes = new[] { 18d, 30, 24, 44, 32 };
        var temperatures = new double?[] { -12, 0, null, 2.5, 22.5 };
        var sizes = new[] { 9d, 36, 81, 49, 25 };
        var recorded = ChartSeries.CreateBubble(new ChartSeriesId("recorded-batches"), "Recorded batches", temperatures.Select((value, index) =>
            new ChartBubble((index + 1) * 16, minutes[index], sizes[index]).WithColorValue(value)));
        recorded.ColorScale(ChartColorScale.Discrete(new[] {
                new ChartColorBand(0, colors.Palette[0], "Cold"),
                new ChartColorBand(15, colors.Status.Medium.Fill, "Mild"),
                new ChartColorBand(null, colors.Status.Critical.Fill, "Warm")
            }).WithNoDataColor(colors.Border)).ColorDimension(options => {
                options.Name = "Temperature";
                options.ValueFormat = ChartValueFormat.Number("#,0.### '°C'", CultureInfo.InvariantCulture);
                options.LegendStyle = ChartColorLegendStyle.Auto;
            });
        return NumericAxes().Labels(labels => labels.NoData = "Not sampled")
            .ValueFormat(ChartValueFormat.Number("0.0 'min'", CultureInfo.InvariantCulture)).AddSeries(recorded);
    }

    private static Chart NumericAxes() => Chart.Create().XAxis(x => x.Title("Batch size (items)")).YAxis(y => y.Title("Processing time (minutes)"))
        .XAxis(x => x.Bounds(0, 100)).YAxis(y => y.Bounds(0, 60)).DataLabels(false);
}

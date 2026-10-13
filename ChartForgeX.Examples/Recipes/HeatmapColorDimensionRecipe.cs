/// <summary>Formats heatmap temperatures with independent color-dimension captions and legends.</summary>
public static class HeatmapColorDimensionRecipe {
    /// <summary>Creates a temperature dimension with automatic, gradient or swatch legend presentation.</summary>
    public static Chart Create(string variant = "color-dimension", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("color-dimension" or "compact-color-dimension" or "gradient-color-legend" or "compact-gradient-color-legend"
            or "swatch-color-legend" or "compact-swatch-color-legend")) throw new ArgumentOutOfRangeException(nameof(variant));
        var colors = VisualTheme.Graphite().Resolve(mode);
        var gradient = variant is "gradient-color-legend" or "compact-gradient-color-legend";
        var swatches = variant is "swatch-color-legend" or "compact-swatch-color-legend";
        var values = new double?[] { -30, -20, -3, 0, 2.5, 5, 10, 22.5, 40, 55, null, null };
        return Chart.Create().ConfigureLabels(labels => labels.NoData = "Not sampled").WithXLabels("A", "B", "C", "D", "E", "F")
            .AddHeatmapRow("Primary", values.Take(6), id: new ChartSeriesId("primary-measurements"))
            .AddHeatmapRow("Secondary", values.Skip(6), id: new ChartSeriesId("secondary-measurements"))
            .WithHeatmapColorScale(TemperatureScale().WithNoDataColor(colors.Border))
            .ConfigureHeatmapColorDimension(options => {
                options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Number("0.0 '°C'", CultureInfo.GetCultureInfo("pl-PL"));
                options.LegendStyle = gradient ? ChartColorLegendStyle.Gradient : swatches ? ChartColorLegendStyle.Swatches : ChartColorLegendStyle.Auto;
            }).WithHeatmapCellGap(3).WithHeatmapCellRadius(3).WithDataLabels();
    }

    private static ChartColorScale TemperatureScale() => ChartColorScale.Diverging(new[] {
        new ChartColorStop(-20, ChartColor.FromHex("#2166AC")), new ChartColorStop(-3, ChartColor.FromHex("#67A9CF"))
    }, new ChartColorStop(0, ChartColor.FromHex("#D1D5DB")), new[] {
        new ChartColorStop(5, ChartColor.FromHex("#D9A441")), new ChartColorStop(40, ChartColor.FromHex("#B2182B"))
    }).WithLabels("Cold", "Freezing", "Warm");
}

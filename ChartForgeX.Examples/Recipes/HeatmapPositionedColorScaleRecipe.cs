/// <summary>Maps heatmap temperatures to explicitly positioned color stops.</summary>
public static class HeatmapPositionedColorScaleRecipe {
    /// <summary>Creates temperature rows with fixed color-stop positions and masked cells.</summary>
    public static Chart Create(string variant = "positioned-color-scale", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("positioned-color-scale" or "compact-positioned-color-scale")) throw new ArgumentOutOfRangeException(nameof(variant));
        var colors = VisualTheme.Graphite().Resolve(mode);
        var values = new double?[] { -30, -20, -3, 0, 2.5, 5, 10, 22.5, 40, 55, null, null };
        return Chart.Create().WithXLabels("A", "B", "C", "D", "E", "F")
            .AddHeatmapRow("Primary", values.Take(6), id: new ChartSeriesId("primary-measurements"))
            .AddHeatmapRow("Secondary", values.Skip(6), id: new ChartSeriesId("secondary-measurements"))
            .WithHeatmapColorScale(TemperatureScale().WithNoDataColor(colors.Border))
            .WithHeatmapCellGap(3).WithHeatmapCellRadius(3)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Auto).WithDataLabels()
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }

    private static ChartColorScale TemperatureScale() => ChartColorScale.Diverging(new[] {
        new ChartColorStop(-20, ChartColor.FromHex("#2166AC")), new ChartColorStop(-3, ChartColor.FromHex("#67A9CF"))
    }, new ChartColorStop(0, ChartColor.FromHex("#D1D5DB")), new[] {
        new ChartColorStop(5, ChartColor.FromHex("#D9A441")), new ChartColorStop(40, ChartColor.FromHex("#B2182B"))
    }).WithLabels("Cold", "Freezing", "Warm");
}

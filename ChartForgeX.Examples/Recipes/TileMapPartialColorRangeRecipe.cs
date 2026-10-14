/// <summary>Creates a tile map with a fixed zero and an observed upper color bound.</summary>
public static class TileMapPartialColorRangeRecipe {
    /// <summary>Creates the source model for a gallery variant without choosing an export backend.</summary>
    public static Chart Create(string variant = "partial-color-range", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("partial-color-range" or "compact-partial-color-range"))
            throw new ArgumentOutOfRangeException(nameof(variant));

        var colors = VisualTheme.Graphite().Resolve(mode);
        var codes = new[] { "CA", "TX", "NY", "FL", "WA", "IL" };
        var counts = new[] { 0d, 18, 42, 76, 29, 51 };
        return Chart.Create().ConfigureLabels(labels => labels.NoData = "Not reported")
            .AddTileMap("Requests", ChartTileMapCatalog.Get("us-states"),
                codes.Select((code, index) => new ChartRegionMapItem(code, counts[index])), id: new ChartSeriesId("state-requests"))
            .WithMapLabels()
            .WithMapColorScale(ChartColorScale.Sequential(colors.SequentialRamp)
                .WithValueRange(0, null).WithLabels("None", null, "Observed peak").WithNoDataColor(colors.Border))
            .ConfigureMapColorDimension(options => {
                options.Name = "Requests";
                options.ValueFormat = ChartValueFormat.Number("0", System.Globalization.CultureInfo.InvariantCulture);
                options.LegendStyle = ChartColorLegendStyle.Gradient;
            })
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}

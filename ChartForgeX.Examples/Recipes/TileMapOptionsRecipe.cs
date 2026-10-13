/// <summary>Creates a tile map with a diverging coverage scale beside the map.</summary>
public static class TileMapOptionsRecipe {
    /// <summary>Creates the source model for a gallery variant without choosing an export backend.</summary>
    public static Chart Create(string variant = "options", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "options") throw new ArgumentOutOfRangeException(nameof(variant));

        var colors = VisualTheme.Graphite().Resolve(mode);
        return Chart.Create().AddTileMap("Observations", ChartTileMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 82), new ChartRegionMapItem("TX", 64),
                new ChartRegionMapItem("NY", 91), new ChartRegionMapItem("FL", 47),
                new ChartRegionMapItem("WA", 74), new ChartRegionMapItem("IL", 58),
                new ChartRegionMapItem("CO", 39), new ChartRegionMapItem("NC", 66)
            }, id: new ChartSeriesId("state-observations"))
            .WithMapLabels()
            .WithMapColorScale(ChartColorScale.Diverging(colors.Status.Critical.Fill, colors.Surface, colors.Status.Pass.Fill, 50)
                .WithValueRange(0, 100).WithLabels("Below target", "Target", "Above target").WithNoDataColor(colors.Border))
            .WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right)
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}

/// <summary>Creates a tile map showing service coverage by state.</summary>
public static class TileMapRecipe {
    /// <summary>Creates the source model for a gallery variant without choosing an export backend.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));

        return Chart.Create().AddTileMap("Observations", ChartTileMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 82), new ChartRegionMapItem("TX", 64),
                new ChartRegionMapItem("NY", 91), new ChartRegionMapItem("FL", 47),
                new ChartRegionMapItem("WA", 74), new ChartRegionMapItem("IL", 58),
                new ChartRegionMapItem("CO", 39), new ChartRegionMapItem("NC", 66)
            }, id: new ChartSeriesId("state-observations"))
            .WithMapLabels()
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}

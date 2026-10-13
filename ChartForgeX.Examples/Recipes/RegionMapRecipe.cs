/// <summary>Creates a geographic map of regional service coverage.</summary>
public static class RegionMapRecipe {
    /// <summary>Creates the source model for a gallery variant without choosing an export backend.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));

        var chart = Chart.Create().AddRegionMap("Observations", ChartMapCatalog.Get("us-states"), RegionValues(),
            id: new ChartSeriesId("regional-observations")).WithMapLabels(false);
        if (variant == "options") {
            var colors = VisualTheme.Graphite().Resolve(mode);
            chart.WithMapColorScale(ChartColorScale.Diverging(colors.Status.Critical.Fill, colors.Surface, colors.Status.Pass.Fill, 50)
                    .WithValueRange(0, 100).WithLabels("Below target", "Target", "Above target").WithNoDataColor(colors.Border))
                .WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        }
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }

    private static ChartRegionMapItem[] RegionValues() => new[] {
        new ChartRegionMapItem("CA", 82), new ChartRegionMapItem("TX", 64),
        new ChartRegionMapItem("NY", 91), new ChartRegionMapItem("FL", 47),
        new ChartRegionMapItem("WA", 74), new ChartRegionMapItem("IL", 58),
        new ChartRegionMapItem("CO", 39), new ChartRegionMapItem("NC", 66)
    };
}

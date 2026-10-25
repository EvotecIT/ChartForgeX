/// <summary>Composes arbitrary regions in one numeric frame, or geographic datasets with an independent tile inset.</summary>
public static class MapCompositionDepthRecipe {
    /// <summary>Creates complete compact/wide examples. The arbitrary variants do not imply geographic registration.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "arbitrary" or "compact-arbitrary")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800;
        if (variant.EndsWith("arbitrary", StringComparison.Ordinal)) {
            var west = new ChartMapDefinition("west-plan", "West plan", new ChartRect(100, 200, 100, 100), new[] {
                new ChartMapRegion("W", "West work area", "M100 200H200V300H100Z")
            });
            var east = new ChartMapDefinition("east-plan", "East plan", new ChartRect(200, 200, 100, 100), new[] {
                new ChartMapRegion("E", "East work area", "M200 200H300V300H200Z")
            });
            return Chart.Create().WithSize(width, 520).WithTheme(VisualTheme.Graphite(), mode).WithTitle("Two work areas, one coordinate frame")
                .WithSubtitle("Independent values and keys; shared authored plan coordinates").WithLegend().WithMapLabels()
                .AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("west"), "West coverage", west, new[] { new ChartRegionMapItem("W", 82) })
                    .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#2563EB")).WithValueRange(0, 100)))
                .AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("east"), "East coverage", east, new[] { new ChartRegionMapItem("E", 64) })
                    .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#CCFBF1"), ChartColor.FromHex("#0D9488")).WithValueRange(0, 100)));
        }
        var region = new ChartMapDefinition("service-zones", "Service zones", new ChartRect(-10, -65, 40, 30), new[] {
            new ChartMapRegion("W", "West zone", "M-10 -35H5V-50H-10Z"),
            new ChartMapRegion("N", "North zone", "M5 -50H30V-65H5Z")
        }).WithGeographicProjection(ChartMapGeoJsonProjection.Equirectangular);
        var coverage = ChartSeries.CreateRegionMap(new ChartSeriesId("coverage"), "Zone coverage", region, new[] {
            new ChartRegionMapItem("W", 92), new ChartRegionMapItem("N", 88)
        }).WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#2563EB")).WithValueRange(0, 100));
        var depots = ChartSeries.CreateDottedMap(new ChartSeriesId("depots"), "Depots", new[] {
            new ChartMapPoint("Madrid", -3.7, 40.4, 18).WithColorValue(-5), new ChartMapPoint("Warsaw", 21, 52.2, 74).WithColorValue(20)
        }).WithObservationIds(new[] { new ChartObservationId("madrid"), new ChartObservationId("warsaw") })
            .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#CCFBF1"), ChartColor.FromHex("#0D9488")).WithValueRange(-10, 30))
            .ReplaceMapConnectors(new[] { new ChartMapConnector("Depot route", -3.7, 40.4, 21, 52.2)
                .WithFeature(new ChartMapFeature("route-1", "{\"service\":\"regional\",\"active\":true}")) });
        var chart = Chart.Create().WithSize(width, 600).WithTheme(VisualTheme.Graphite(), mode).WithLegend().WithMapLabels().WithDataLabels()
            .WithTitle("Geographic service zones and tile summary").WithSubtitle("The tile inset is an independent layout, not a geographic overlay")
            .AddSeries(coverage).AddSeries(depots);
        var tiles = new ChartTileMapDefinition("service-summary", "Service summary", new[] {
            new ChartTileMapRegion("W", "West", 0, 0), new ChartTileMapRegion("C", "Central", 1, 0), new ChartTileMapRegion("N", "North", 2, 0)
        });
        var inset = ChartSeries.CreateTileMap(new ChartSeriesId("summary"), "Summary coverage", tiles, new[] {
            new ChartRegionMapItem("W", 92), new ChartRegionMapItem("C", 74), new ChartRegionMapItem("N", 88)
        }).WithMapInset(new ChartRect(.02, .73, .48, .24))
            .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#FEF3C7"), ChartColor.FromHex("#D97706")).WithValueRange(0, 100));
        inset.ConfigureColorDimension(options => { options.Name = "Tile coverage"; options.ValueFormat = ChartValueFormat.Number("0 '%'", CultureInfo.InvariantCulture); });
        chart.AddSeries(inset);
        var depot = chart.Series[1];
        depot.UpdateMapPoint(0, depot.MapPoints[0].WithFeature(new ChartMapFeature("depot-madrid", "{\"operator\":\"Example Logistics\",\"priority\":2,\"active\":true}")));
        return chart;
    }
}

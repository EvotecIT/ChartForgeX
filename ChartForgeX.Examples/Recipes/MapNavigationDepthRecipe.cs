/// <summary>Shows opt-in source-preserving grid badges, shared callouts and route endpoint clearance.</summary>
public static class MapNavigationDepthRecipe {
    /// <summary>Creates the same complete native scene at compact/wide sizes in either theme.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant == "compact" ? 390 : 800;
        var definition = new ChartMapDefinition("service-zones", "Service zones", new ChartRect(-10, -65, 40, 30), new[] {
            new ChartMapRegion("W", "West service zone", "M-10 -35H5V-50H-10Z", new ChartPoint(-2.5, -42.5)),
            new ChartMapRegion("N", "North service zone", "M5 -50H30V-65H5Z", new ChartPoint(17.5, -57.5))
        }).WithGeographicProjection(ChartMapGeoJsonProjection.Equirectangular);
        var coverage = ChartSeries.CreateRegionMap(new ChartSeriesId("zones"), "Coverage", definition, new[] {
            new ChartRegionMapItem("W", 92), new ChartRegionMapItem("N", 88)
        }).WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#2563EB")).WithValueRange(0, 100));
        var points = new[] {
            new ChartMapPoint("West gateway", -3.7, 40.4, 10).WithColorValue(-5),
            new ChartMapPoint("North intake", 21, 52.2, 20).WithColorValue(0).WithFeature(new ChartMapFeature("intake", "{\"kind\":\"intake\"}")),
            new ChartMapPoint("North processing", 21, 52.2, 30).WithColorValue(15).WithFeature(new ChartMapFeature("processing", "{\"kind\":\"processing\"}")),
            new ChartMapPoint("North dispatch", 21, 52.2, 40).WithColorValue(25).WithFeature(new ChartMapFeature("dispatch", "{\"kind\":\"dispatch\"}")),
            new ChartMapPoint("Outside this viewport", 120, 0, 80).WithColorValue(80)
        };
        var sites = ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Source sites", points)
            .WithObservationIds(new[] { new ChartObservationId("west"), new ChartObservationId("intake"), new ChartObservationId("processing"), new ChartObservationId("dispatch"), new ChartObservationId("outside") })
            .WithMapClustering(new ChartMapClustering(48, 2, 14))
            .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#CCFBF1"), ChartColor.FromHex("#0D9488")).WithValueRange(-10, 80))
            .ReplaceMapConnectors(new[] { new ChartMapConnector("Gateway to northern services", -3.7, 40.4, 21, 52.2).WithEndpointClearance(4) });
        sites.ConfigureMapPointSize(options => { options.MinimumRadius = 3; options.MaximumRadius = 9; });
        var chart = Chart.Create().WithSize(width, 600).WithTheme(VisualTheme.Graphite(), mode).WithLegend().WithMapLabels().WithDataLabels()
            .WithTitle("Source sites, shared geography").WithSubtitle("Grid badges count sites; source weights, colors and identities remain available")
            .WithMapViewport(new ChartMapViewport("Service window", -10, 30, 35, 65)).AddSeries(coverage).AddSeries(sites);
        chart.Options.MapCallouts = new ChartMapCallouts(48, ChartMapRegionLabel.Name);
        return chart;
    }
}

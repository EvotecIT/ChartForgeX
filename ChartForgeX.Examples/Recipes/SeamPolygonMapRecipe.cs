/// <summary>Imports complete dateline rings into an explicit matching native map window, preserving raw feature snapshots.</summary>
public static class SeamPolygonMapRecipe {
    /// <summary>Gets the complete fictional source geometry, including a seam-crossing hole, disconnected islands and an outside feature.</summary>
    public const string SourceGeoJson = """
        {"type":"FeatureCollection","features":[
          {"type":"Feature","id":123456789012345678901,"properties":{"code":"I","name":"Service island","Name":"Original case","capacity":1e+2,"zero":0.00,"nested":{"enabled":true,"note":"<source>&"}},"geometry":{"type":"Polygon","coordinates":[[[175.00,-25,1e+1],[-175,-25,10],[-175,-5,10],[175,-5,10],[175.00,-25,1e+1]],[[178,-20],[-178,-20],[-178,-10],[178,-10],[178,-20]]]}},
          {"type":"Feature","id":"islands-source","properties":{"code":"M","name":"Disconnected island peers"},"geometry":{"type":"MultiPolygon","coordinates":[[[[171,0],[174,0],[174,6],[171,6],[171,0]]],[[[-174,-29],[-171,-29],[-171,-26],[-174,-26],[-174,-29]]]]}},
          {"type":"Feature","id":"outside-source","properties":{"code":"O","name":"Outside this view"},"geometry":{"type":"Polygon","coordinates":[[[130,-15],[140,-15],[140,-5],[130,-5],[130,-15]]]}}
        ]}
        """;

    /// <summary>Creates compact/wide light/dark scenes with either existing geographic projection.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "mercator" or "compact-mercator")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800;
        var projection = variant.EndsWith("mercator", StringComparison.Ordinal) ? ChartMapGeoJsonProjection.WebMercator : ChartMapGeoJsonProjection.Equirectangular;
        var viewport = ChartMapViewport.AcrossAntimeridian("Pacific polygon window", 170, -170, -30, 10, projection);
        var definition = ChartMapGeoJson.ToMapDefinition("pacific-regions", "Pacific regions", SourceGeoJson,
            new ChartMapGeoJsonOptions { Projection = projection, PolygonViewport = viewport });
        var regions = ChartSeries.CreateRegionMap(new ChartSeriesId("regions"), "Service coverage", definition, new[] {
            new ChartRegionMapItem("I", 80), new ChartRegionMapItem("M", 55), new ChartRegionMapItem("O", 20)
        }).WithObservationIds(new[] { new ChartObservationId("island-observation"), new ChartObservationId("peers-observation"), new ChartObservationId("outside-observation") })
            .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#2563EB")).WithValueRange(0, 100));
        var ports = ChartSeries.CreateDottedMap(new ChartSeriesId("ports"), "Ports", new[] {
            new ChartMapPoint("Western port", 176, -23), new ChartMapPoint("Eastern port", -176, -7)
        }).WithObservationIds(new[] { new ChartObservationId("west-port"), new ChartObservationId("east-port") })
            .WithMarkerRadius(4).ReplaceMapConnectors(new[] { new ChartMapConnector("Service route", 176, -23, -176, -7).WithEndpointClearance(2) });
        var chart = Chart.Create().WithSize(width, 540).WithTheme(VisualTheme.Graphite(), mode).WithLegend().WithMapLabels().WithDataLabels()
            .WithTitle("Dateline polygons retain their source").WithSubtitle("Complete rings and holes; the view clips paint without filtering features")
            .WithMapViewport(viewport).AddSeries(regions).AddSeries(ports);
        chart.Options.MapCallouts = new ChartMapCallouts(36, ChartMapRegionLabel.Name);
        return chart;
    }
}

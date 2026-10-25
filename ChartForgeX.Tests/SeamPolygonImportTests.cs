using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SeamPolygonImportTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("mercator", VisualThemeMode.Light)]
    [InlineData("mercator", VisualThemeMode.Dark)]
    [InlineData("compact-mercator", VisualThemeMode.Light)]
    [InlineData("compact-mercator", VisualThemeMode.Dark)]
    public void CompleteRingsFeaturesAndObservationIdentitiesSurviveNativePaintClipping(string variant, VisualThemeMode mode) {
        var chart = SeamPolygonMapRecipe.Create(variant, mode); var definition = chart.Series[0].RegionMapDefinition!;
        var prepared = MapLabelClusterDepthTests.Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        MapLabelClusterDepthTests.Capture(prepared, "seam-polygons-" + variant + "-" + mode);
        Assert.Equal(chart.Options.MapViewport.MinimumLongitude, definition.PolygonViewport!.Value.MinimumLongitude);
        Assert.Equal(chart.Options.MapViewport.GeographicProjection, definition.GeographicProjection);
        Assert.Equal(3, definition.Regions.Count);
        var island = definition.Regions.Single(region => region.Code == "I"); var feature = Assert.IsType<ChartMapFeature>(island.Feature);
        Assert.Equal("123456789012345678901", feature.IdJson); Assert.Equal(island.GeoJsonGeometry, island.WithFeature(null).GeoJsonGeometry);
        var properties = JsonDocument.Parse(feature.PropertiesJson!).RootElement;
        Assert.Equal("Service island", properties.GetProperty("name").GetString()); Assert.Equal("Original case", properties.GetProperty("Name").GetString());
        Assert.Equal("1e+2", properties.GetProperty("capacity").GetRawText()); Assert.Equal("0.00", properties.GetProperty("zero").GetRawText());
        Assert.Equal("<source>&", properties.GetProperty("nested").GetProperty("note").GetString());
        var raw = JsonDocument.Parse(island.GeoJsonGeometry!).RootElement;
        Assert.Equal(2, raw.GetProperty("coordinates").GetArrayLength()); Assert.Equal("175.00", raw.GetProperty("coordinates")[0][0][0].GetRawText());
        Assert.Equal("1e+1", raw.GetProperty("coordinates")[0][0][2].GetRawText()); Assert.Equal(-175, raw.GetProperty("coordinates")[0][1][0].GetDouble());
        var groups = MapLabelClusterDepthTests.Groups(prepared, "region-map-region-source"); Assert.Equal(3, groups.Length);
        Assert.Equal("true", groups.Single(group => group.Metadata["data-cfx-region"] == "I").Metadata["data-cfx-visible"]);
        Assert.Equal("false", groups.Single(group => group.Metadata["data-cfx-region"] == "O").Metadata["data-cfx-visible"]);
        Assert.Equal(island.GeoJsonGeometry, groups.Single(group => group.Metadata["data-cfx-region"] == "I").Metadata["data-cfx-geojson-geometry"]);
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "region-map-region"));
        var source = MapCompositionDepthTests.Source(prepared, "region-map-region-source", "regions", "island-observation");
        Assert.Equal(feature.IdJson, (string?)source.Attribute("data-cfx-feature-id-json"));
        Assert.NotEqual((string?)source.Attribute("data-cfx-feature-id"), (string?)source.Attribute("data-cfx-observation-id"));
        Assert.Equal(new[] { "80", "55", "20" }, groups.Select(group => group.Metadata["data-cfx-value"]));
        chart.Series[0].UpdateRegionValue(0, new ChartRegionMapItem("I", 95)); var updated = MapLabelClusterDepthTests.Prepare(chart);
        Assert.Equal("95", MapLabelClusterDepthTests.Groups(updated, "region-map-region-source")[0].Metadata["data-cfx-value"]);
        Assert.Same(feature, chart.Series[0].RegionMapDefinition!.Regions[0].Feature);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        MapCompositionDepthTests.AssertScriptFreeAndDimensions(prepared, variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800, 540);
    }

    [Fact]
    public void GeometryAndFeatureBudgetsAreReservedBeforePeriodicOutputExpansionWhileCoordinateFiltersRemainExplicit() {
        var view = ChartMapViewport.AcrossAntimeridian("Almost world", -170, -180, -30, 10);
        const string json = """{"type":"Feature","id":1.00,"properties":{"code":"A"},"geometry":{"type":"Polygon","coordinates":[[[165,-25],[-165,-25],[-165,-5],[165,-5],[165,-25]]]}}""";
        var options = new ChartMapGeoJsonOptions { PolygonViewport = view, MaximumObservations = 9 };
        Assert.Contains("MaximumObservations", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapDefinition("copies", "Copies", json, options)).Message);
        options.MaximumObservations = 10;
        var definition = ChartMapGeoJson.ToMapDefinition("copies", "Copies", json, options);
        var ring = Assert.Single(definition.Regions); Assert.Equal(2, ring.Path.Count(character => character == 'M'));
        Assert.Equal(1, JsonDocument.Parse(ring.GeoJsonGeometry!).RootElement.GetProperty("coordinates").GetArrayLength());
        options.MaximumExpandedFeatureCharacters = ring.Feature!.IdJson!.Length + ring.Feature.PropertiesJson!.Length + ring.GeoJsonGeometry!.Length - 1;
        Assert.Contains("MaximumExpandedFeatureCharacters", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapDefinition("copies", "Copies", json, options)).Message);
        options.MaximumExpandedFeatureCharacters++; Assert.Equal(ring.Path, ChartMapGeoJson.ToMapDefinition("copies", "Copies", json, options).Regions[0].Path);
        const string filtered = """{"type":"FeatureCollection","features":[{"type":"Feature","properties":{"code":"OUT","payload":"No output allocation for this filtered feature"},"geometry":{"type":"Polygon","coordinates":[[[0,50],[10,50],[10,60],[0,60],[0,50]]]}},{"type":"Feature","properties":{"code":"IN"},"geometry":{"type":"Polygon","coordinates":[[[175,-25],[-175,-25],[-175,-5],[175,-5],[175,-25]]]}}]}""";
        var filteredOptions = new ChartMapGeoJsonOptions { PolygonViewport = ChartMapViewport.AcrossAntimeridian("Pacific", 170, -170, -30, 10), MaximumObservations = 5 };
        filteredOptions.WithCoordinateBounds(-180, 180, -30, 10);
        Assert.Equal("IN", Assert.Single(ChartMapGeoJson.ToMapDefinition("filtered", "Filtered", filtered, filteredOptions).Regions).Code);
    }

    [Theory]
    [InlineData("[[0,0],[120,0],[-120,10],[0,0]]", "wind")]
    [InlineData("[[175,0],[-175,0],[-175,10],[174,0]]", "close")]
    [InlineData("[[175,0],[-175,0],[175,0]]", "four")]
    public void AmbiguousWindingAndUnclosedRingsFailBeforeAnyDefinitionIsReturned(string positions, string message) {
        var json = "{\"type\":\"Feature\",\"geometry\":{\"type\":\"Polygon\",\"coordinates\":[" + positions + "]}}";
        var error = Assert.ThrowsAny<Exception>(() => ChartMapGeoJson.ToMapDefinition("invalid", "Invalid", json, new ChartMapGeoJsonOptions {
            PolygonViewport = ChartMapViewport.AcrossAntimeridian("Pacific", 170, -170, -30, 20)
        }));
        Assert.True(error is ArgumentException or NotSupportedException); Assert.Contains(message, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EquivalentRawSeamEndpointsCloseWithoutRewritingTheirCapturedBearings() {
        const string json = """{"type":"Feature","properties":{"code":"A"},"geometry":{"type":"Polygon","coordinates":[[[180,-20],[-175,-20],[-175,-10],[180,-10],[-180,-20]]]}}""";
        var viewport = ChartMapViewport.AcrossAntimeridian("Seam closure", 170, -170, -30, 10);
        var definition = ChartMapGeoJson.ToMapDefinition("closure", "Closure", json, new ChartMapGeoJsonOptions { PolygonViewport = viewport });
        var region = Assert.Single(definition.Regions); var positions = JsonDocument.Parse(region.GeoJsonGeometry!).RootElement.GetProperty("coordinates")[0];
        Assert.Equal(180, positions[0][0].GetDouble()); Assert.Equal(-180, positions[4][0].GetDouble());
        var contour = Assert.Single(ChartMapPathParser.ParseSubpaths(region.Path));
        Assert.Equal(contour.Points[0], contour.Points[contour.Points.Count - 1]);
        Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapDefinition("wrong-projection", "Wrong projection", json,
            new ChartMapGeoJsonOptions { PolygonViewport = viewport, Projection = ChartMapGeoJsonProjection.WebMercator }));
    }

    [Fact]
    public void ExistingDefaultImportAndManualMetadataClearingRetainOriginalPathsAndPixels() {
        const string json = """{"type":"Feature","id":"source","properties":{"code":"A","n":1.00},"geometry":{"type":"Polygon","coordinates":[[[1,1],[11,1],[11,11],[1,11],[1,1]]]}}""";
        var ordinary = ChartMapGeoJson.ToMapDefinition("ordinary", "Ordinary", json);
        Assert.Equal("M1 -1L11 -1L11 -11L1 -11L1 -1Z", ordinary.Regions[0].Path); Assert.Null(ordinary.PolygonViewport);
        Assert.Null(ordinary.Regions[0].Feature); Assert.Null(ordinary.Regions[0].GeoJsonGeometry);
        var baseline = Chart.Create().WithSize(480, 320).WithLegend(false).AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("ordinary"), "Ordinary", ordinary, new[] { new ChartRegionMapItem("A", 1) }));
        var before = MapLabelClusterDepthTests.Prepare(baseline); var original = ordinary.Regions[0];
        var changed = original.WithFeature(new ChartMapFeature("inert", "{\"color\":\"not paint\"}"));
        var decorated = new ChartMapDefinition(ordinary.Id, ordinary.Name, ordinary.Bounds, new[] { changed });
        var chart = Chart.Create().WithSize(480, 320).WithLegend(false).AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("ordinary"), "Ordinary", decorated, new[] { new ChartRegionMapItem("A", 1) }));
        Assert.Equal(before.ToPng(), MapLabelClusterDepthTests.Prepare(chart).ToPng());
        Assert.Equal(original.Path, changed.WithFeature(null).Path); Assert.Null(changed.WithFeature(null).Feature);
        var cleared = new ChartMapDefinition(ordinary.Id, ordinary.Name, ordinary.Bounds, new[] { changed.WithFeature(null) });
        var reset = Chart.Create().WithSize(480, 320).WithLegend(false).AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("ordinary"), "Ordinary", cleared, new[] { new ChartRegionMapItem("A", 1) }));
        Assert.Equal(before.ToSvg(), MapLabelClusterDepthTests.Prepare(reset).ToSvg());
    }

    [Fact]
    public void CountOnlyWriterUsesTheSameEscapesAndOriginalNumberSpellingsAsMaterializedSourceJson() {
        var raw = "\u0001\u007f\ufdd0\ufffe" + char.ConvertFromUtf32(0x10FFFF) + "<source>&\"北";
        var json = "{\"number\":1e+2,\"zero\":-0.00,\"Name\":true,\"name\":null,\"nested\":[" + JsonSerializer.Serialize(raw) + ",9007199254740993]}";
        var value = GeoJsonValue.Parse(json, StringComparer.Ordinal, new GeoJsonReadLimits(100, 100, 100).KeepNumberSpelling().RejectDuplicates());
        var count = new VisualArtifactInterchangeJsonWriter(writeText: false, escapeMarkupUnsafeCharacters: true);
        var text = new VisualArtifactInterchangeJsonWriter(escapeMarkupUnsafeCharacters: true);
        count.Value(value); text.Value(value);
        Assert.Equal(text.ToString().Length, text.CharactersWritten); Assert.Equal(text.CharactersWritten, count.CharactersWritten); Assert.Equal(text.ValuesWritten, count.ValuesWritten);
        Assert.Throws<InvalidOperationException>(() => count.ToString());
        var restored = JsonDocument.Parse(text.ToString()).RootElement;
        Assert.Equal("1e+2", restored.GetProperty("number").GetRawText()); Assert.Equal("-0.00", restored.GetProperty("zero").GetRawText());
        Assert.Equal(raw, restored.GetProperty("nested")[0].GetString()); Assert.Equal("9007199254740993", restored.GetProperty("nested")[1].GetRawText());
    }
}

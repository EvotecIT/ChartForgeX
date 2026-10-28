using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapFeatureMetadataTests {
    private const string Features = """
        {"type":"FeatureCollection","features":[
          {"type":"Feature","id":123456789012345678901,"properties":{"name":"Shared ports","Name":"Case peer","zero":0.00,"decimal":1e-12,"truth":true,"empty":null,"nested":{"A":[1,false,{"x":"<script>&\"北"}],"a":2}},"geometry":{"type":"MultiPoint","coordinates":[[4,52],[21,52]]}},
          {"type":"Feature","id":"123456789012345678901","properties":{"name":"Shared routes","tags":["water","land"]},"geometry":{"type":"MultiLineString","coordinates":[[[4,52],[10,53],[21,52]],[[21,52],[10,60]]]}}
        ]}
        """;

    [Fact]
    public void ImportedFeatureSnapshotsKeepTypedIdsNestedValuesCaseAndMultipartAssociation() {
        var points = ChartMapGeoJson.ToMapPoints(Features); var routes = ChartMapGeoJson.ToMapConnectors(Features);
        var feature = Assert.IsType<ChartMapFeature>(points[0].Feature);
        Assert.Same(feature, points[1].Feature); Assert.Same(routes[0].Feature, routes[1].Feature);
        Assert.Equal("123456789012345678901", feature.Id); Assert.Equal("123456789012345678901", feature.IdJson);
        Assert.Equal("\"123456789012345678901\"", routes[0].Feature!.IdJson);
        var properties = JsonDocument.Parse(feature.PropertiesJson!).RootElement;
        Assert.Equal("Shared ports", properties.GetProperty("name").GetString()); Assert.Equal("Case peer", properties.GetProperty("Name").GetString());
        Assert.Equal("0.00", properties.GetProperty("zero").GetRawText()); Assert.Equal("1e-12", properties.GetProperty("decimal").GetRawText());
        Assert.True(properties.GetProperty("truth").GetBoolean()); Assert.Equal(JsonValueKind.Null, properties.GetProperty("empty").ValueKind);
        Assert.Equal("<script>&\"北", properties.GetProperty("nested").GetProperty("A")[2].GetProperty("x").GetString());
        Assert.Equal(2, properties.GetProperty("nested").GetProperty("a").GetInt32());
        Assert.Empty(ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Sites", points).ObservationIds);
        Assert.All(routes.SelectMany(route => route.RoutePoints), point => Assert.Null(point.Feature));
    }

    [Fact]
    public void FeatureCopiesAndPublicUpdatesRetainFactsButNeverSubstituteObservationIdentityOrChangePixels() {
        var source = ChartMapGeoJson.ToMapPoints(Features);
        var route = ChartMapGeoJson.ToMapConnectors(Features)[0];
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("ports"), "Ports", source)
            .WithObservationIds(new[] { new ChartObservationId("west"), new ChartObservationId("east") }).ReplaceMapConnectors(new[] { route });
        series.UpdateMapPoint(0, series.MapPoints[0].WithColorValue(-5));
        Assert.Same(source[0].Feature, series.MapPoints[0].Feature); Assert.Same(route.Feature, series.MapConnectors[0].Feature);
        Assert.Equal("west", series.ObservationIds[0].Value);
        var chart = Chart.Create().WithSize(640, 420).WithLegend(false).WithMapViewport(ChartMapViewport.Europe()).AddSeries(series);
        var prepared = MapCompositionDepthTests.Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var target = MapCompositionDepthTests.Source(prepared, "dotted-map-point-source", "ports", "west");
        Assert.Equal(source[0].Feature!.PropertiesJson, (string?)target.Attribute("data-cfx-feature-properties"));
        Assert.Equal("123456789012345678901", (string?)target.Attribute("data-cfx-feature-id-json"));
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), node => node.Name.LocalName == "script");
        var connector = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "dotted-map-connector-source");
        Assert.Equal(route.Feature!.IdJson, connector.Metadata["data-cfx-feature-id-json"]);
        series.UpdateMapPoint(0, series.MapPoints[0].WithFeature(null)); series.UpdateMapPoint(1, series.MapPoints[1].WithFeature(null));
        series.ReplaceMapConnectors(new[] { route.WithFeature(null) });
        var without = MapCompositionDepthTests.Prepare(chart);
        Assert.Equal(png, without.ToPng()); Assert.DoesNotContain("data-cfx-feature-", without.ToSvg());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        MapCompositionDepthTests.Capture(prepared, "map-feature-snapshot");
    }

    [Fact]
    public void JsonOnlyScalarsRoundTripThroughNativeSvgMetadataWithoutBecomingMarkup() {
        var value = "\u0001\u007f\ufffe" + char.ConvertFromUtf32(0x10FFFF);
        var json = JsonSerializer.Serialize(new { value });
        var feature = new ChartMapFeature(value, json);
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("controls"), "Controls", new[] { new ChartMapPoint("Point", 0, 0).WithFeature(feature) })
            .WithObservationId(0, new ChartObservationId("source"));
        var prepared = MapCompositionDepthTests.Prepare(Chart.Create().AddSeries(series));
        var node = MapCompositionDepthTests.Source(prepared, "dotted-map-point-source", "controls", "source");
        Assert.Null(node.Attribute("data-cfx-feature-id"));
        Assert.Equal(value, JsonDocument.Parse(node.Attribute("data-cfx-feature-id-json")!.Value).RootElement.GetString());
        Assert.Equal(value, JsonDocument.Parse(node.Attribute("data-cfx-feature-properties")!.Value).RootElement.GetProperty("value").GetString());
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void PointLegendRetainsFeatureFactsWithoutCollapsingSharedFeatureObservationIds() {
        var points = ChartMapGeoJson.ToMapPoints(Features);
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("ports"), "Ports", points)
            .WithObservationIds(new[] { new ChartObservationId("west"), new ChartObservationId("east") });
        var chart = Chart.Create().WithSize(640, 420).WithLegend().WithPointLegend().AddSeries(series);
        var legends = XDocument.Parse(MapCompositionDepthTests.Prepare(chart).ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "legend-entry" && node.Attribute("data-cfx-feature-id") != null).ToArray();
        Assert.Equal(2, legends.Length);
        Assert.All(legends, legend => Assert.Equal(points[0].Feature!.PropertiesJson, (string?)legend.Attribute("data-cfx-feature-properties")));
        Assert.Equal(new[] { "east", "west" }, legends.Select(node => (string?)node.Attribute("data-cfx-observation-id")).OrderBy(value => value));
    }

    [Fact]
    public void AbsentNullAndStringIdPropertiesRetainTheirMeaningWithoutFallbackIds() {
        var absent = Assert.Single(ChartMapGeoJson.ToMapPoints("""{"type":"Feature","geometry":{"type":"Point","coordinates":[0,0]}}""")).Feature!;
        Assert.Null(absent.Id); Assert.Null(absent.IdJson); Assert.Null(absent.PropertiesJson);
        var explicitNull = Assert.Single(ChartMapGeoJson.ToMapPoints("""{"type":"Feature","id":null,"properties":null,"geometry":{"type":"Point","coordinates":[0,0]}}""")).Feature!;
        Assert.Equal("null", explicitNull.IdJson); Assert.Equal("null", explicitNull.PropertiesJson);
        var manual = new ChartMapFeature("01", "{\"n\":1.00,\"N\":false}");
        Assert.Equal("\"01\"", manual.IdJson); Assert.Equal("{\"n\":1.00,\"N\":false}", manual.PropertiesJson);
        Assert.Equal("R1", Assert.Single(ChartMapGeoJson.ToMapPoints("""{"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[0,0]}}""")).Label);
        Assert.Null(absent.IdJson);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"x\":1,\"x\":2}")]
    [InlineData("{\"n\":1e999}")]
    [InlineData("{\"unclosed\":")]
    public void ManualMetadataUsesExistingStrictJsonBoundary(string json) => Assert.ThrowsAny<ArgumentException>(() => new ChartMapFeature("id", json));

    [Fact]
    public void ManualMetadataRetainsExistingSizeAndRecursiveDepthLimits() {
        Assert.Throws<ArgumentException>(() => new ChartMapFeature("id", new string(' ', 8 * 1024 * 1024 + 1)));
        var depth = new string('[', 65) + "0" + new string(']', 65);
        Assert.Throws<ArgumentException>(() => new ChartMapFeature("id", "{\"deep\":" + depth + "}"));
    }

    [Fact]
    public void MetadataExpansionBudgetCountsEachPointOrLineAfterFilteringAndNotEachWaypoint() {
        var points = ChartMapGeoJson.ToMapPoints(Features);
        var feature = points[0].Feature!;
        var characters = feature.IdJson!.Length + feature.PropertiesJson!.Length;
        var options = new ChartMapGeoJsonOptions { MaximumExpandedFeatureCharacters = characters * 2 - 1 };
        Assert.Contains("MaximumExpandedFeatureCharacters", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapPoints(Features, options)).Message);
        options.MaximumExpandedFeatureCharacters = characters * 2;
        Assert.Equal(2, ChartMapGeoJson.ToMapPoints(Features, options).Count);
        const string oneLine = """{"type":"Feature","id":"route","properties":{"name":"Route"},"geometry":{"type":"LineString","coordinates":[[0,0],[1,1],[2,2]]}}""";
        var line = Assert.Single(ChartMapGeoJson.ToMapConnectors(oneLine));
        var exact = line.Feature!.IdJson!.Length + line.Feature.PropertiesJson!.Length;
        options.MaximumExpandedFeatureCharacters = exact;
        Assert.Equal(3, Assert.Single(ChartMapGeoJson.ToMapConnectors(oneLine, options)).RoutePoints.Count);
        options.MaximumExpandedFeatureCharacters = 1; options.WithCoordinateBounds(50, 60, 50, 60);
        Assert.Empty(ChartMapGeoJson.ToMapConnectors(oneLine, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaximumExpandedFeatureCharacters = 0);
    }
}

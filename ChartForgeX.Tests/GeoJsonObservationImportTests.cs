using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GeoJsonObservationImportTests {
    private const string Features = """
        {"type":"FeatureCollection","features":[
          {"type":"Feature","id":"sites","properties":{"name":"Ports","load":0,"delay":-8,"area":"north"},"geometry":{"type":"MultiPoint","coordinates":[[4.4792,51.9244],[21.0122,52.2297]]}},
          {"type":"Feature","id":"single","properties":{"name":"Oslo","load":25,"delay":15,"area":"north"},"geometry":{"type":"Point","coordinates":[10.7522,59.9139,12]}},
          {"type":"Feature","id":"routes","properties":{"name":"Shipping","area":"north"},"geometry":{"type":"MultiLineString","coordinates":[[[4.4792,51.9244],[8,54],[10.7522,59.9139]],[[10.7522,59.9139],[21.0122,52.2297]]]}},
          {"type":"Feature","id":"outside","properties":{"name":"Singapore","load":10,"area":"south"},"geometry":{"type":"Point","coordinates":[103.8198,1.3521]}}
        ]}
        """;

    [Fact]
    public void PointAndRouteImportsShareFiltersAndRetainRawCoordinatesValuesAndSourceOrder() {
        var options = new ChartMapGeoJsonOptions { WeightPropertyName = "load", ColorValuePropertyName = "delay", Projection = ChartMapGeoJsonProjection.WebMercator };
        options.IncludeFeaturePropertyValues("area", "north");
        var points = ChartMapGeoJson.ToMapPoints(Features, options);
        var routes = ChartMapGeoJson.ToMapConnectors(Features, options);
        Assert.Equal(new[] { "Ports [1]", "Ports [2]", "Oslo" }, points.Select(point => point.Label));
        Assert.Equal(new[] { 4.4792, 21.0122, 10.7522 }, points.Select(point => point.Longitude));
        Assert.Equal(new[] { 51.9244, 52.2297, 59.9139 }, points.Select(point => point.Latitude));
        Assert.Equal(new double?[] { 0, 0, 25 }, points.Select(point => point.Weight));
        Assert.Equal(new double?[] { -8, -8, 15 }, points.Select(point => point.ColorValue));
        Assert.Equal(new[] { "Shipping [1]", "Shipping [2]" }, routes.Select(route => route.Label));
        Assert.Equal(new[] { 4.4792, 8, 10.7522 }, routes[0].RoutePoints.Select(point => point.Longitude));
        Assert.Equal(59.9139, routes[0].ToLatitude);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartMapPoint>)points)[0] = new ChartMapPoint("Changed", 0, 0));
        Assert.Throws<NotSupportedException>(() => ((IList<ChartMapConnector>)routes).Clear());
        var chart = Chart.Create().WithSize(560, 340).WithLegend(false).WithDataLabels()
            .WithMapViewport(ChartMapViewport.Europe()).AddDottedMap("Imported ports", points);
        chart.Options.MapConnectors.AddRange(routes);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "dotted-map-point-source"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "dotted-map-connector-source"));
        Assert.Contains("Ports [1]", prepared.ToSvg());
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(560, image.Width); Assert.Equal(340, image.Height);
        Capture(prepared, "geojson-ports-routes");
    }

    [Fact]
    public void CoordinateFilteringRetainsCompleteLineGeometryAndMissingPointValuesStayUnweighted() {
        const string input = """
            {"type":"FeatureCollection","features":[
              {"type":"Feature","id":"site","properties":{},"geometry":{"type":"Point","coordinates":[5,5]}},
              {"type":"Feature","id":"line","properties":{},"geometry":{"type":"LineString","coordinates":[[-5,5],[5,5],[15,5]]}}
            ]}
            """;
        var options = new ChartMapGeoJsonOptions { WeightPropertyName = "load" };
        options.WithCoordinateBounds(0, 10, 0, 10);
        var point = Assert.Single(ChartMapGeoJson.ToMapPoints(input, options));
        Assert.Equal("site", point.Label); Assert.Null(point.Weight);
        var route = Assert.Single(ChartMapGeoJson.ToMapConnectors(input, options));
        Assert.Equal("line", route.Label);
        Assert.Equal(new[] { -5d, 5, 15 }, route.RoutePoints.Select(value => value.Longitude));
        options.IncludeFeaturesIntersectingCoordinateBounds = false;
        Assert.Empty(ChartMapGeoJson.ToMapConnectors(input, options));
        Assert.Single(ChartMapGeoJson.ToMapPoints(input, options));
    }

    [Theory]
    [InlineData("Point", "[181,0]", null)]
    [InlineData("Point", "[0,-91]", null)]
    [InlineData("Point", "[0]", null)]
    [InlineData("Point", "[0,0]", "-1")]
    [InlineData("Point", "[0,0]", "\"large\"")]
    [InlineData("LineString", "[[0,0]]", null)]
    [InlineData("LineString", "[[0,0],[181,0]]", null)]
    public void MalformedPositionsWeightsAndRoutesFailAtTheImportBoundary(string geometry, string coordinates, string? value) {
        var input = "{\"type\":\"Feature\",\"id\":\"bad\",\"properties\":{" + (value == null ? "" : "\"load\":" + value)
            + "},\"geometry\":{\"type\":\"" + geometry + "\",\"coordinates\":" + coordinates + "}}";
        var options = new ChartMapGeoJsonOptions { WeightPropertyName = "load" };
        if (geometry == "Point") Assert.ThrowsAny<ArgumentException>(() => ChartMapGeoJson.ToMapPoints(input, options));
        else Assert.ThrowsAny<ArgumentException>(() => ChartMapGeoJson.ToMapConnectors(input, options));
    }

    [Fact]
    public void UnsupportedGeometryIsExplicitAndAnEmptyImportSupportsTheExistingNoDataLifecycle() {
        const string input = """{"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[0,1],[0,0]]]},"properties":{}}""";
        Assert.Empty(ChartMapGeoJson.ToMapPoints(input)); Assert.Empty(ChartMapGeoJson.ToMapConnectors(input));
        var options = new ChartMapGeoJsonOptions { SkipUnsupportedGeometries = false };
        Assert.Contains("Polygon", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapPoints(input, options)).Message);
        Assert.Contains("Polygon", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapConnectors(input, options)).Message);
        var chart = Chart.Create().AddDottedMap("Sites", ChartMapGeoJson.ToMapPoints(input));
        Assert.Equal(0, chart.Series[0].ObservationCount);
        chart.Series[0].ReplaceMapPoints(ChartMapGeoJson.ToMapPoints(Features));
        Assert.Equal(4, chart.Series[0].ObservationCount);
    }

    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
    }
}

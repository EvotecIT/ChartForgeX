using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GeographicMapCompositionTests {
    private const int CoordinatePrecision = 4;
    private const string Geometry = """
        {"type":"FeatureCollection","features":[
          {"type":"Feature","properties":{"code":"A","name":"West"},"geometry":{"type":"Polygon","coordinates":[[[-10,40],[0,40],[0,50],[-10,50],[-10,40]]]}},
          {"type":"Feature","properties":{"code":"B","name":"North"},"geometry":{"type":"Polygon","coordinates":[[[0,50],[10,50],[10,60],[0,60],[0,50]]]}},
          {"type":"Feature","properties":{"code":"C","name":"East"},"geometry":{"type":"Polygon","coordinates":[[[10,40],[20,40],[20,50],[10,50],[10,40]]]}}
        ]}
        """;

    [Theory]
    [InlineData(ChartMapGeoJsonProjection.Equirectangular, 390, VisualThemeMode.Light)]
    [InlineData(ChartMapGeoJsonProjection.Equirectangular, 800, VisualThemeMode.Dark)]
    [InlineData(ChartMapGeoJsonProjection.WebMercator, 390, VisualThemeMode.Dark)]
    [InlineData(ChartMapGeoJsonProjection.WebMercator, 800, VisualThemeMode.Light)]
    public void IndependentRegionAndWeightedSiteDatasetsShareProjectionKeysAndDetachedIdentity(
        ChartMapGeoJsonProjection projection, int width, VisualThemeMode mode) {
        var chart = Fixture(projection, width, mode);
        var regions = chart.Series[0]; var sites = chart.Series[1];
        var prepared = Prepare(chart);
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "geographic-map");
        Assert.Equal(projection == ChartMapGeoJsonProjection.WebMercator ? "web-mercator" : "equirectangular", source.Metadata["data-cfx-projection"]);
        var keys = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "map-color-scale-source").ToArray();
        Assert.Equal(new[] { "Coverage", "Delay" }, keys.Select(key => key.Metadata["data-cfx-label-color"]));
        var markers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray();
        Assert.Equal(2, markers.Length); Assert.True(markers[1].Rx > markers[0].Rx);
        var polygons = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "region-map-region").ToArray();
        Assert.Equal(3, polygons.Length);
        // Imported paths round to their authored precision; points retain raw coordinates. The shared frame may amplify at most half one source-coordinate unit.
        var logicalPixelsPerSourceUnit = Math.Abs(polygons[1].Commands[1].X - polygons[1].Commands[0].X) / 10;
        var quantizationBound = Math.Pow(10, -CoordinatePrecision) / 2 * logicalPixelsPerSourceUnit + 1e-8;
        Assert.Contains(polygons[1].Commands, command => Math.Abs(command.X - markers[0].Cx) <= quantizationBound && Math.Abs(command.Y - markers[0].Cy) <= quantizationBound);
        var target = Target(prepared, "gateways", "west");
        Assert.Equal("0", target.Attribute("data-cfx-value")!.Value);
        Assert.Equal("-10", target.Attribute("data-cfx-color-value")!.Value);
        Assert.Contains("Delay", target.Attribute("aria-label")!.Value);
        Assert.Equal(1, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(node => node.Role == "dotted-map-connector"));
        var originalSvg = prepared.ToSvg(); var originalPng = prepared.ToPng();
        sites.ReplaceMapPoints(new[] {
            new ChartMapPoint("West gateway", 0, 50, 50).WithColorValue(-1),
            new ChartMapPoint("North gateway", 10, 60, 0)
        });
        Assert.Empty(sites.ObservationIds);
        sites.WithObservationIds(new[] { new ChartObservationId("west"), new ChartObservationId("north") });
        sites.ReplaceMapConnectors(Array.Empty<ChartMapConnector>());
        chart.MoveSeries(sites, 0);
        var updated = Prepare(chart);
        Assert.Equal("0", Target(updated, "gateways", "west").Attribute("data-cfx-series")!.Value);
        Assert.Equal("-1", Target(updated, "gateways", "west").Attribute("data-cfx-color-value")!.Value);
        Assert.Equal("true", Target(updated, "gateways", "north").Attribute("data-cfx-color-missing")!.Value);
        Assert.Equal(50, sites.MapPoints[0].Value); Assert.Equal(-1, sites.MapPoints[0].ColorValue);
        Assert.Null(sites.MapPoints[1].ColorValue); Assert.Equal(0, sites.MapPoints[1].Value);
        Assert.Equal(originalSvg, prepared.ToSvg()); Assert.Equal(originalPng, prepared.ToPng());
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(width, image.Width); Assert.Equal(460, image.Height);
        var north = markers[1]; var painted = north.Fill!.Value;
        var sample = ((int)Math.Floor(north.Cy - north.Ry * .6) * image.Width + (int)Math.Floor(north.Cx)) * 4;
        Assert.Equal(painted.R, image.Pixels[sample]); Assert.Equal(painted.G, image.Pixels[sample + 1]); Assert.Equal(painted.B, image.Pixels[sample + 2]);
        var artifact = prepared.ToArtifact("map-layers", VisualArtifactKind.Chart);
        Assert.Contains("data-cfx-projection", artifact.ToSvg());
        Capture(prepared, "map-layers-" + projection + "-" + width + "-" + mode);
    }

    [Fact]
    public void DatasetFormattingIsCapturedOnceAcrossPointLegendsKeysAndMarksAndRegionKeysMayInheritChartScale() {
        var chart = Fixture(ChartMapGeoJsonProjection.Equirectangular, 760, VisualThemeMode.Light).WithPointLegend();
        var calls = new Dictionary<double, int>();
        chart.Series[1].ConfigureColorDimension(options => options.ValueFormat = ChartValueFormat.Custom(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " minutes";
        }));
        var inherited = chart.Series[0].ColorScale;
        chart.Series[0].WithColorScale(null).ConfigureColorDimension(options => options.LegendStyle = ChartColorLegendStyle.Gradient);
        chart.WithMapColorScale(inherited);
        var prepared = Prepare(chart);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Contains("minutes", prepared.ToSvg());
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "map-color-scale-source"));
    }

    [Fact]
    public void StandaloneDottedMapKeepsItsExistingUnoutlinedCaptionPaint() {
        var chart = Chart.Create().WithDataLabels().AddDottedMap("Sites", new[] { new ChartMapPoint("Site", 5, 52, 10) });
        var label = Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "dotted-map-data-label");
        Assert.Null(label.Stroke);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void CaptionsOverRegionLayersUseContrastingHalosInNativeSvgAndPng(VisualThemeMode mode) {
        var blue = ChartColor.FromHex("#2255CC");
        var definition = new ChartMapDefinition("zone", "Zone", new ChartRect(-10, -65, 30, 25), new[] {
            new ChartMapRegion("all", "All", "M-10 -65H20V-40H-10Z")
        }).WithGeographicProjection(ChartMapGeoJsonProjection.Equirectangular);
        var sites = ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Sites", new[] {
            new ChartMapPoint("West depot", 0, 50, 10), new ChartMapPoint("North depot", 10, 60, 20)
        }).ReplaceMapConnectors(new[] { new ChartMapConnector("Delivery route", 0, 50, 10, 60) });
        var chart = Chart.Create().WithSize(640, 460).WithTheme(VisualTheme.Graphite(), mode).WithHeader(false).WithLegend(false).WithMapLabels(false).WithDataLabels()
            .AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("zone"), "Coverage", definition, new[] { new ChartRegionMapItem("all", 80) })
                .WithColorScale(ChartColorScale.Sequential(blue, blue).WithValueRange(0, 100))).AddSeries(sites);
        var prepared = Prepare(chart); var scene = prepared.Scene;
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(IsMapCaption).ToArray();
        Assert.Equal(3, labels.Length);
        Assert.All(labels, label => {
            Assert.True(label.Stroke.HasValue && label.StrokeWidth > 0);
            Assert.True(ChartColorMath.ContrastRatio(label.Color, label.Stroke!.Value) >= 4.5);
        });
        var svgLabels = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node =>
            (string?)node.Attribute("data-cfx-role") is "dotted-map-data-label" or "dotted-map-connector-label")
            .Select(node => Assert.Single(node.Descendants(), child => child.Name.LocalName == "text")).ToArray();
        Assert.Equal(3, svgLabels.Length);
        Assert.All(svgLabels, label => { Assert.NotNull(label.Attribute("stroke")); Assert.Equal("stroke", (string?)label.Attribute("paint-order")); });
        var actual = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size, scene.Nodes.Select(node => node is VisualSceneText label && IsMapCaption(label)
            ? new VisualSceneText(label.Text, label.X, label.Baseline, label.Color, label.Alignment, label.Role, label.Id, label.Paint) : node).ToArray(), scene.Diagnostics, scene.Regions), supersampling: 1);
        Assert.True(actual.Pixels.Where((value, index) => value != without.Pixels[index]).Take(80).Count() >= 80, "Contrasting caption halos must reach the native PNG painter.");
        Capture(prepared, "map-label-halos-" + mode);
    }

    private static bool IsMapCaption(VisualSceneText label) => label.Role is "dotted-map-data-label" or "dotted-map-connector-label";

    [Fact]
    public void DatasetNumericKeyVisibilityOverridesChartPolicyWithoutCategoricalEntries() {
        var chart = Fixture(ChartMapGeoJsonProjection.Equirectangular, 760, VisualThemeMode.Light);
        chart.Options.MapColorDimension.ShowLegend = false;
        foreach (var series in chart.Series) { series.ShowInLegend = false; series.ConfigureColorDimension(options => options.ShowLegend = false); }
        chart.Series[0].ConfigureColorDimension(options => options.ShowLegend = true);
        Assert.Equal("0", Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "map-color-scale-source").Metadata["data-cfx-series"]);
        chart.Series[0].ConfigureColorDimension(options => options.ShowLegend = false);
        chart.Series[1].ConfigureColorDimension(options => options.ShowLegend = true);
        Assert.Equal("1", Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "map-color-scale-source").Metadata["data-cfx-series"]);
        chart.Series[1].ConfigureColorDimension(options => options.ShowLegend = false);
        chart.Options.MapColorDimension.ShowLegend = true;
        Assert.DoesNotContain(Prepare(chart).Scene.Nodes, node => node.Role == "map-color-scale-source");
    }

    [Fact]
    public void HomogeneousGeographicDatasetsUseTheSameCompositorWithoutDroppingPeers() {
        var definition = ChartMapGeoJson.ToMapDefinition("same-frame", "Same frame", Geometry);
        var regions = Chart.Create().AddRegionMap("First", definition, new[] { new ChartRegionMapItem("A", 1) })
            .AddRegionMap("Second", definition, new[] { new ChartRegionMapItem("B", 10) });
        var regionScene = Prepare(regions);
        Assert.Equal(6, regionScene.Scene.Nodes.OfType<VisualScenePath>().Count(node => node.Role == "region-map-region"));
        Assert.Equal(2, regionScene.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "map-series"));
        Assert.Equal(2, regionScene.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "map-color-scale-source"));
        var sites = Chart.Create().AddDottedMap("First", new[] { new ChartMapPoint("First site", 1, 2, 3) })
            .AddDottedMap("Second", new[] { new ChartMapPoint("Second site", 3, 4, 5) });
        var siteScene = Prepare(sites);
        Assert.Equal(2, siteScene.Scene.Nodes.OfType<VisualSceneEllipse>().Count(node => node.Role == "dotted-map-point"));
        Assert.Equal(2, siteScene.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "dotted-map"));
    }

    [Fact]
    public void ProjectionAssociationAndReadOnlyRoutesRetainAllIndependentPointMeasurements() {
        var definition = ChartMapGeoJson.ToMapDefinition("areas", "Areas", Geometry, new ChartMapGeoJsonOptions { Projection = ChartMapGeoJsonProjection.WebMercator });
        Assert.Equal(ChartMapGeoJsonProjection.WebMercator, definition.GeographicProjection);
        var independent = definition.WithGeographicProjection(ChartMapGeoJsonProjection.Equirectangular);
        Assert.Equal(ChartMapGeoJsonProjection.WebMercator, definition.GeographicProjection);
        Assert.Equal(ChartMapGeoJsonProjection.Equirectangular, independent.GeographicProjection);
        Assert.Equal(definition.Bounds, independent.Bounds); Assert.Equal(definition.Regions[0].Path, independent.Regions[0].Path);
        var source = new[] { new ChartMapPoint("Start", 1, 2, 0).WithColorValue(-10), new ChartMapPoint("End", 3, 4, 20).WithColorValue(10) };
        var route = new ChartMapConnector("Route", source);
        var sites = ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Sites", source).ReplaceMapConnectors(new[] { route });
        source[0] = new ChartMapPoint("Changed", 20, 30);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartMapPoint>)route.RoutePoints)[0] = source[0]);
        Assert.Equal(-10, sites.MapConnectors[0].RoutePoints[0].ColorValue);
        Assert.Equal(0, sites.MapConnectors[0].RoutePoints[0].Value);
        sites.WithObservationId(0, new ChartObservationId("start"));
        sites.UpdateMapPoint(0, new ChartMapPoint("Updated", 1, 2, 7).WithColorValue(-4));
        Assert.Equal("start", sites.ObservationIds[0].Value); Assert.Equal(-4, sites.MapPoints[0].ColorValue); Assert.Equal(7, sites.MapPoints[0].Value);
        Assert.Throws<ArgumentException>(() => sites.ReplaceMapConnectors(new[] { default(ChartMapConnector) }));
        Assert.Single(sites.MapConnectors);
    }

    [Fact]
    public void AmbiguousCoordinateSystemsAndMapPointNamesRejectAtPublicBoundaries() {
        var arbitrary = new ChartMapDefinition("xy", "XY", 10, 10, new[] { new ChartMapRegion("A", "A", "M0 0L10 0L10 10Z") });
        var chart = Chart.Create().AddRegionMap("Regions", arbitrary, new[] { new ChartRegionMapItem("A", 1) })
            .AddDottedMap("Sites", new[] { new ChartMapPoint("Site", 1, 1) });
        Assert.Contains("projection", Assert.Throws<InvalidOperationException>(() => Prepare(chart)).Message);
        var equirectangular = ChartMapGeoJson.ToMapDefinition("eq", "EQ", Geometry);
        var mercator = ChartMapGeoJson.ToMapDefinition("merc", "Merc", Geometry, new ChartMapGeoJsonOptions { Projection = ChartMapGeoJsonProjection.WebMercator });
        var mismatch = Chart.Create().AddRegionMap("EQ", equirectangular, new[] { new ChartRegionMapItem("A", 1) })
            .AddRegionMap("Merc", mercator, new[] { new ChartRegionMapItem("A", 2) });
        Assert.Contains("same projection", Assert.Throws<InvalidOperationException>(() => Prepare(mismatch)).Message);
        var tiles = Chart.Create().AddTileMap("Tiles", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 2) })
            .AddDottedMap("Sites", new[] { new ChartMapPoint("Site", 1, 1) });
        Assert.Throws<InvalidOperationException>(() => Prepare(tiles));
        var duplicates = Chart.Create().AddDottedMap("First", new[] { new ChartMapPoint("Shared", 1, 1) })
            .AddDottedMap("Second", new[] { new ChartMapPoint("Shared", 2, 2), new ChartMapPoint("Other", 3, 3) });
        Assert.Contains("one geographic location", Assert.Throws<ArgumentException>(() => duplicates.AddMapRouteBetweenPoints("Ambiguous", "Shared", "Other")).Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LabelRoutesAcceptCoincidentObservationsWithoutCombiningTheirFacts(bool acrossDatasets) {
        var first = new ChartMapPoint("Shared", 21, 52, 3).WithColorValue(-5);
        var peer = new ChartMapPoint("shared", 21, 52, 9).WithColorValue(5);
        var destination = new ChartMapPoint("Other", 10, 60, 12);
        var chart = Chart.Create().WithSize(640, 460).WithLegend(false).WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("First", acrossDatasets ? new[] { first, destination } : new[] { first, peer, destination });
        if (acrossDatasets) chart.AddDottedMap("Second", new[] { peer });
        if (acrossDatasets) chart.AddMapConnectorBetweenPoints("Shared route", "SHARED", "Other");
        else chart.AddMapRouteBetweenPoints("Shared route", " SHARED ", "Other");
        var connector = Assert.Single(chart.Options.MapConnectors);
        Assert.Equal((21d, 52d, 10d, 60d),
            (connector.FromLongitude, connector.FromLatitude, connector.ToLongitude, connector.ToLatitude));
        Assert.Equal(3, chart.Series.Sum(series => series.MapPoints.Count));
        Assert.Equal(new double?[] { -5, 5 }, chart.Series.SelectMany(series => series.MapPoints)
            .Where(point => point.Longitude == 21).Select(point => point.ColorValue));
        var prepared = Prepare(chart);
        Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "dotted-map-point-source"));
        var route = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "dotted-map-connector-source");
        Assert.Equal("21,52;10,60", route.Metadata["data-cfx-route-coordinates"]);
        Assert.Contains("Shared route", prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng());
        Capture(prepared, "coincident-label-route-" + acrossDatasets);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LabelRoutesRejectEvenAdjacentRepresentableCoordinatesWithoutAddingAConnector(bool longitudeDiffers) {
        var longitude = longitudeDiffers ? Math.BitIncrement(21d) : 21;
        var latitude = longitudeDiffers ? 52 : Math.BitIncrement(52d);
        var chart = Chart.Create().AddDottedMap("First", new[] { new ChartMapPoint("Shared", 21, 52) })
            .AddDottedMap("Second", new[] { new ChartMapPoint("Shared", longitude, latitude), new ChartMapPoint("Other", 10, 60) });
        var error = Assert.Throws<ArgumentException>(() => chart.AddMapRouteBetweenPoints("Ambiguous", "Shared", "Other"));
        Assert.Equal("fromPointLabel", error.ParamName);
        Assert.Empty(chart.Options.MapConnectors);
        Assert.Equal(3, chart.Series.Sum(series => series.MapPoints.Count));
    }

    internal static Chart Fixture(ChartMapGeoJsonProjection projection, int width, VisualThemeMode mode) {
        var definition = ChartMapGeoJson.ToMapDefinition("districts", "Districts", Geometry, new ChartMapGeoJsonOptions { Projection = projection, CoordinatePrecision = CoordinatePrecision });
        var regions = ChartSeries.CreateRegionMap(new ChartSeriesId("coverage"), "District coverage", definition,
            new[] { new ChartRegionMapItem("A", 0), new ChartRegionMapItem("B", 100) })
            .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#1D4ED8")).WithValueRange(0, 100));
        regions.ConfigureColorDimension(options => options.Name = "Coverage");
        var points = new[] { new ChartMapPoint("West gateway", 0, 50, 0).WithColorValue(-10), new ChartMapPoint("North gateway", 10, 60, 100).WithColorValue(30) };
        var sites = ChartSeries.CreateDottedMap(new ChartSeriesId("gateways"), "Gateway delays", points)
            .WithColorScale(ChartColorScale.Diverging(ChartColor.FromHex("#0D9488"), ChartColor.FromHex("#E2E8F0"), ChartColor.FromHex("#EA580C"), 0).WithValueRange(-20, 40));
        sites.ConfigureColorDimension(options => options.Name = "Delay");
        sites.WithObservationIds(new[] { new ChartObservationId("west"), new ChartObservationId("north") });
        sites.ReplaceMapConnectors(new[] { new ChartMapConnector("Gateway route", new[] { points[0], new ChartMapPoint("Transfer", 6, 52), points[1] }) });
        return Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(width, 460).WithTitle("Districts and gateways")
            .WithSubtitle("Independent coverage and delay colors; marker size shows traffic")
            .WithLegend(true).WithMapLabels(false).WithDataLabels().AddSeries(regions).AddSeries(sites);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static XElement Target(PreparedVisual prepared, string series, string observation) => Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), node =>
        (string?)node.Attribute("data-cfx-series-key") == series && (string?)node.Attribute("data-cfx-observation-id") == observation && node.Attribute("data-cfx-label-for") == null);
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
    }
}

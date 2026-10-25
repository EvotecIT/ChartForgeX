using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SeamPolygonPaintTests {
    [Theory]
    [InlineData(ChartMapGeoJsonProjection.Equirectangular)]
    [InlineData(ChartMapGeoJsonProjection.WebMercator)]
    public void ExistingCompoundFillAndRectangleClipRetainTheSeamHoleInNativePixelsAndSvg(ChartMapGeoJsonProjection projection) {
        var chart = SeamPolygonMapRecipe.Create(projection == ChartMapGeoJsonProjection.WebMercator ? "mercator" : "wide");
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        var path = prepared.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "region-map-region");
        var map = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "region-map-surface").Bounds;
        var isolated = new VisualSceneBuilder(prepared.Scene.Size, FontSpec.SystemSans());
        using (isolated.PushClip(map)) isolated.Path(new ChartPath(path.Commands), ChartColor.Black, strokeWidth: 0, role: "actual-island", close: true);
        var native = new PreparedVisual(isolated.Build()); var image = native.ToRgba();
        var viewport = chart.Options.MapViewport; var source = ChartGeographicProjection.Bounds(viewport, projection);
        ChartPoint At(double longitude, double latitude) {
            var point = ChartGeographicProjection.Project(longitude, latitude, projection, viewport);
            return new ChartPoint(map.Left + (point.X - source.Left) / source.Width * map.Width, map.Top + (point.Y - source.Top) / source.Height * map.Height);
        }
        Assert.Equal(255, Alpha(image, At(176, -15))); Assert.Equal(0, Alpha(image, At(180, -15))); Assert.Equal(0, Alpha(image, At(174, -15)));
        var svg = XDocument.Parse(native.ToSvg()); var element = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "actual-island");
        Assert.Equal("evenodd", (string?)element.Attribute("fill-rule")); Assert.Equal(2, element.Attribute("d")!.Value.Count(character => character == 'M'));
        Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "clipPath");
        MapLabelClusterDepthTests.Capture(native, "seam-polygon-isolated-hole-" + projection);
    }

    [Fact]
    public void NearWorldViewKeepsBothPeriodicPolygonCopiesAndTheirHolesWithoutDuplicatingRawFacts() {
        var viewport = ChartMapViewport.AcrossAntimeridian("Two edge pieces", -170, -180, -30, 10);
        const string json = """{"type":"Feature","id":"one-feature","properties":{"code":"A"},"geometry":{"type":"Polygon","coordinates":[[[165,-25],[-160,-25],[-160,-5],[165,-5],[165,-25]],[[170,-20],[-165,-20],[-165,-10],[170,-10],[170,-20]]]}}""";
        var definition = ChartMapGeoJson.ToMapDefinition("two-pieces", "Two edge pieces", json, new ChartMapGeoJsonOptions { PolygonViewport = viewport, MaximumObservations = 20 });
        var region = Assert.Single(definition.Regions); Assert.Equal(4, region.Path.Count(character => character == 'M'));
        var chart = Chart.Create().WithSize(800, 360).WithLegend(false).WithMapLabels(false).WithMapViewport(viewport)
            .AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("one-region"), "One region", definition, new[] { new ChartRegionMapItem("A", 1) }));
        var prepared = MapLabelClusterDepthTests.Prepare(chart); var source = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "region-map-region-source"));
        Assert.Equal("\"one-feature\"", source.Metadata["data-cfx-feature-id-json"]); Assert.Equal("true", source.Metadata["data-cfx-visible"]);
        Assert.Equal(2, System.Text.Json.JsonDocument.Parse(source.Metadata["data-cfx-geojson-geometry"]).RootElement.GetProperty("coordinates").GetArrayLength());
        var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "region-map-region");
        var map = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "region-map-surface").Bounds;
        Assert.Contains(path.Commands, command => command.X < map.Left); Assert.Contains(path.Commands, command => command.X > map.Right);
        Assert.Equal(4, path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        MapLabelClusterDepthTests.Capture(prepared, "seam-polygon-both-periodic-edges");
    }

    [Theory]
    [InlineData("chart-window")]
    [InlineData("chart-projection")]
    [InlineData("missing-chart-view")]
    [InlineData("ordinary-base")]
    [InlineData("ordinary-overlay")]
    public void CapturedPolygonGeometryCannotBeReinterpretedByAMismatchedView(string mismatch) {
        var chart = SeamPolygonMapRecipe.Create();
        if (mismatch == "chart-window") chart.WithMapViewport(ChartMapViewport.AcrossAntimeridian("Different window", 172, -168, -30, 10));
        else if (mismatch == "chart-projection") chart.WithMapViewport(ChartMapViewport.AcrossAntimeridian("Different projection", 170, -170, -30, 10, ChartMapGeoJsonProjection.WebMercator));
        else if (mismatch == "missing-chart-view") chart.WithMapViewport(ChartMapViewport.World());
        else {
            var ordinary = new ChartMapDefinition("ordinary", "Ordinary", 10, 10, new[] { new ChartMapRegion("A", "Alpha", "M0 0H10V10H0Z") });
            var layer = new ChartMapLayer(ordinary, null, ChartColor.Black, 1, "ordinary-context");
            (mismatch.EndsWith("base", StringComparison.Ordinal) ? chart.Options.MapBaseLayers : chart.Options.MapOverlayLayers).Add(layer);
        }
        Assert.Contains("PolygonViewport", Assert.Throws<NotSupportedException>(() => MapLabelClusterDepthTests.Prepare(chart)).Message);
    }

    [Fact]
    public void MatchingContextDeclarationsIgnoreOnlyDisplayNameAndRetainTheirRawSourceSnapshots() {
        var chart = SeamPolygonMapRecipe.Create(); var definition = chart.Series[0].RegionMapDefinition!;
        var renamed = ChartMapViewport.AcrossAntimeridian("Another display label", 170, -170, -30, 10);
        var context = definition.WithPolygonViewport(renamed);
        chart.Options.MapBaseLayers.Add(new ChartMapLayer(context, null, ChartColor.Black.WithAlpha(50), .5, "seam-context"));
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        Assert.Equal(3, MapLabelClusterDepthTests.Groups(prepared, "map-layer-region-source").Length);
        Assert.Equal(definition.Regions[0].GeoJsonGeometry, MapLabelClusterDepthTests.Groups(prepared, "map-layer-region-source")[0].Metadata["data-cfx-geojson-geometry"]);
        Assert.NotEqual(renamed.Name, definition.PolygonViewport!.Value.Name); Assert.Equal(renamed.Name, context.PolygonViewport!.Value.Name);
        Assert.Throws<ArgumentException>(() => definition.WithGeographicProjection(ChartMapGeoJsonProjection.WebMercator));
        Assert.Throws<ArgumentException>(() => definition.WithPolygonViewport(ChartMapViewport.World()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StandaloneDottedContextCannotBypassTheRequiredMatchingPolygonView(bool overlay) {
        var definition = SeamPolygonMapRecipe.Create().Series[0].RegionMapDefinition!;
        var chart = Chart.Create().WithSize(480, 320).AddSeries(ChartSeries.CreateDottedMap(new ChartSeriesId("points"), "Points", new[] { new ChartMapPoint("Site", 178, -18) }));
        var layer = new ChartMapLayer(definition, null, ChartColor.Black, 1, "seam-context");
        (overlay ? chart.Options.MapOverlayLayers : chart.Options.MapBaseLayers).Add(layer);
        Assert.Contains("PolygonViewport", Assert.Throws<NotSupportedException>(() => MapLabelClusterDepthTests.Prepare(chart)).Message);
        chart.WithMapViewport(definition.PolygonViewport!.Value); var prepared = MapLabelClusterDepthTests.Prepare(chart);
        Assert.Equal(3, MapLabelClusterDepthTests.Groups(prepared, "map-layer-region-source").Length);
        Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-point-source"));
    }

    [Fact]
    public void AViewWhollyInsideAHoleRetainsTheFeatureWithoutPaintOrAnInteractiveSemanticRegion() {
        var viewport = ChartMapViewport.AcrossAntimeridian("Hole only", 170, -170, -20, -10);
        const string json = """{"type":"Feature","id":"hole-context","properties":{"code":"A"},"geometry":{"type":"Polygon","coordinates":[[[150,-30],[-140,-30],[-140,0],[150,0],[150,-30]],[[160,-25],[-150,-25],[-150,-5],[160,-5],[160,-25]]]}}""";
        var definition = ChartMapGeoJson.ToMapDefinition("hole-only", "Hole only", json, new ChartMapGeoJsonOptions { PolygonViewport = viewport });
        var chart = Chart.Create().WithSize(480, 320).WithLegend(false).WithMapLabels(false).WithMapViewport(viewport)
            .AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("hole-only"), "Hole only", definition, new[] { new ChartRegionMapItem("A", 1) }));
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        Assert.Equal("false", Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "region-map-region-source")).Metadata["data-cfx-visible"]);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "region-map-region");
        Assert.DoesNotContain(prepared.Regions, region => region.Role == "region-map-region");
        Assert.Equal("hole-context", definition.Regions[0].Feature!.Id);
        MapLabelClusterDepthTests.Capture(prepared, "seam-polygon-view-entirely-in-hole");
    }

    private static byte Alpha(RgbaImage image, ChartPoint point) => image.Pixels[((int)Math.Round(point.Y) * image.Width + (int)Math.Round(point.X)) * 4 + 3];
}

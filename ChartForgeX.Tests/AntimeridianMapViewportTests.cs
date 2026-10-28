using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AntimeridianMapViewportTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("mercator", VisualThemeMode.Light)]
    [InlineData("mercator", VisualThemeMode.Dark)]
    [InlineData("compact-mercator", VisualThemeMode.Light)]
    [InlineData("compact-mercator", VisualThemeMode.Dark)]
    public void ExplicitSeamWindowRetainsRawFactsCoincidentBearingsAndRectangularlyClippedNativeCurves(string variant, VisualThemeMode mode) {
        var chart = DatelineMapRecipe.Create(variant, mode); var prepared = MapLabelClusterDepthTests.Prepare(chart);
        MapLabelClusterDepthTests.Capture(prepared, "dateline-map-" + variant + "-" + mode);
        var map = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "geographic-map"));
        Assert.Equal("170", map.Metadata["data-cfx-source-left"]); Assert.Equal("20", map.Metadata["data-cfx-source-width"]);
        Assert.Equal("170", map.Metadata["data-cfx-viewport-min-longitude"]); Assert.Equal("-170", map.Metadata["data-cfx-viewport-max-longitude"]);
        Assert.Equal("20", map.Metadata["data-cfx-unwrapped-longitude-span"]); Assert.Equal("true", map.Metadata["data-cfx-crosses-antimeridian"]);
        Assert.Equal(variant.EndsWith("mercator", StringComparison.Ordinal) ? "web-mercator" : "equirectangular", map.Metadata["data-cfx-projection"]);
        var sources = MapLabelClusterDepthTests.Groups(prepared, "dotted-map-point-source"); Assert.Equal(5, sources.Length);
        Assert.Equal(new[] { "178", "-172", "180", "-180", "160" }, sources.Select(source => source.Metadata["data-cfx-longitude"]));
        Assert.Equal(new[] { "fiji", "samoa", "seam-east", "seam-west", "outside" }, sources.Select(source => source.Metadata["data-cfx-observation-id"]));
        Assert.Equal(new[] { "10", "20", "30", "40", "50" }, sources.Select(source => source.Metadata["data-cfx-value"]));
        Assert.Equal("false", sources[4].Metadata["data-cfx-visible"]);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray(); Assert.Equal(4, marks.Length);
        Assert.True(marks[1].Cx > marks[0].Cx); Assert.Equal(marks[2].Cx, marks[3].Cx); Assert.Equal(marks[2].Cy, marks[3].Cy);
        var factor = (marks[1].Cx - marks[0].Cx) / 10;
        var left = marks[0].Cx - 8 * factor; var right = left + 20 * factor;
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Clip.HasValue
            && Math.Abs(group.Clip.Value.Left - left) < 1e-7 && Math.Abs(group.Clip.Value.Right - right) < 1e-7);
        var routes = MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"); Assert.Equal(3, routes.Length);
        Assert.Equal("178,-18;-172,-14", routes[0].Metadata["data-cfx-route-coordinates"]);
        Assert.Equal("true", routes[0].Metadata["data-cfx-visible"]); Assert.Equal("true", routes[0].Metadata["data-cfx-route-unwrapped"]);
        Assert.Equal("168,-22;175,-20;-168,-18", routes[1].Metadata["data-cfx-route-coordinates"]); Assert.Equal("3", routes[1].Metadata["data-cfx-waypoint-count"]);
        Assert.Equal("true", routes[1].Metadata["data-cfx-visible"]); Assert.Equal("true", routes[1].Metadata["data-cfx-route-view-clipped"]);
        Assert.Equal("false", routes[2].Metadata["data-cfx-visible"]);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-connector").ToArray(); Assert.Equal(2, paths.Length);
        Assert.Contains(paths[1].Commands, command => command.X < left || command.X > right);
        Assert.All(paths, path => Assert.Contains(path.Commands, command => command.Kind == ChartPathCommandKind.CubicTo));
        MapCompositionDepthTests.AssertScriptFreeAndDimensions(prepared, variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800, 520);
    }

    [Fact]
    public void FactoryRetainsRawBoundsAndRejectsEmptyAmbiguousOrInvalidWindowsWithoutChangingOrderedConstruction() {
        var view = ChartMapViewport.AcrossAntimeridian("Pacific", 170, -170, -30, 10);
        Assert.True(view.CrossesAntimeridian); Assert.Equal(170, view.MinimumLongitude); Assert.Equal(-170, view.MaximumLongitude); Assert.Equal(20, view.LongitudeSpan);
        Assert.Equal(ChartMapGeoJsonProjection.Equirectangular, view.GeographicProjection);
        Assert.Equal(10, ChartMapViewport.AcrossAntimeridian("West seam", 180, -170, -30, 10).LongitudeSpan);
        Assert.Equal(10, ChartMapViewport.AcrossAntimeridian("East seam", 170, -180, -30, 10).LongitudeSpan);
        Assert.Equal(350, ChartMapViewport.AcrossAntimeridian("Almost world", -170, -180, -30, 10).LongitudeSpan);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapViewport("Still ordered", 170, -170, -30, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMapViewport.AcrossAntimeridian("Zero", 180, -180, -30, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMapViewport.AcrossAntimeridian("Equal", 170, 170, -30, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMapViewport.AcrossAntimeridian("Not crossing", -170, 170, -30, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMapViewport.AcrossAntimeridian("Invalid", double.NaN, -170, -30, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMapViewport.AcrossAntimeridian("Invalid", 170, -170, 10, -30));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMapViewport.AcrossAntimeridian("Invalid", 170, -170, -30, 10, (ChartMapGeoJsonProjection)999));
        var world = ChartMapViewport.World(); Assert.False(world.CrossesAntimeridian); Assert.Equal(360, world.LongitudeSpan); Assert.Null(world.GeographicProjection);
    }

    [Theory]
    [InlineData(ChartMapGeoJsonProjection.Equirectangular)]
    [InlineData(ChartMapGeoJsonProjection.WebMercator)]
    public void SourceWrappingUsesTheExistingLatitudeProjectionAndOrderedControls(ChartMapGeoJsonProjection projection) {
        var view = ChartMapViewport.AcrossAntimeridian("Pacific", 170, -170, -30, 10, projection);
        Assert.Equal(178, ChartGeographicProjection.Project(178, -18, projection, view).X);
        Assert.Equal(188, ChartGeographicProjection.Project(-172, -14, projection, view).X);
        Assert.Equal(ChartGeographicProjection.Project(-172, -14, projection).Y, ChartGeographicProjection.Project(-172, -14, projection, view).Y);
        Assert.Equal(180, ChartGeographicProjection.Project(180, -10, projection, view).X); Assert.Equal(180, ChartGeographicProjection.Project(-180, -10, projection, view).X);
        Assert.True(ChartGeographicProjection.Contains(new ChartPoint(170, -30), view)); Assert.True(ChartGeographicProjection.Contains(new ChartPoint(-170, 10), view));
        Assert.False(ChartGeographicProjection.Contains(new ChartPoint(169, 0), view)); Assert.False(ChartGeographicProjection.Contains(new ChartPoint(-169, 0), view));
        Assert.False(ChartGeographicProjection.Contains(new ChartPoint(178, 11), view));
        var ordered = ChartMapViewport.Europe(); Assert.Equal(ChartGeographicProjection.Project(21, 52, projection), ChartGeographicProjection.Project(21, 52, projection, ordered));
    }

    [Fact]
    public void ContinuousRoutesRetainSourceOrderAndHalfTurnSignWithoutInventingOppositeMeridianCrossings() {
        var view = ChartMapViewport.AcrossAntimeridian("Pacific", 170, -170, -30, 10);
        ChartPoint[] Route(params double[] longitudes) => ChartGeographicProjection.ProjectRoute(longitudes.Select(value => new ChartPoint(value, 0)).ToArray(), ChartMapGeoJsonProjection.Equirectangular, view);
        Assert.Equal(new[] { 178d, 188 }, Route(178, -172).Select(point => point.X));
        Assert.Equal(new[] { 182d, 178 }, Route(-178, 178).Select(point => point.X));
        Assert.Equal(new[] { 168d, 175, 192 }, Route(168, 175, -168).Select(point => point.X));
        Assert.Equal(new[] { 0d, 180 }, Route(0, 180).Select(point => point.X));
        Assert.Equal(new[] { 360d, 180 }, Route(0, -180).Select(point => point.X));
        var opposite = Route(-10, 10); Assert.All(opposite, point => Assert.True(point.X > 190 || point.X < 170));
    }

    [Theory]
    [InlineData("arbitrary-region")]
    [InlineData("geographic-region")]
    [InlineData("base")]
    [InlineData("overlay")]
    [InlineData("bounds")]
    public void NonSeamAwareGeometryFailsClearlyBeforeAViewCanBeMisaligned(string kind) {
        var chart = DatelineMapRecipe.Create();
        var definition = new ChartMapDefinition("old-context", "Old context", 20, 20, new[] { new ChartMapRegion("A", "Alpha", "M0 0H20V20H0Z") });
        if (kind == "geographic-region") definition = definition.WithGeographicProjection(ChartMapGeoJsonProjection.Equirectangular);
        if (kind.EndsWith("region", StringComparison.Ordinal)) chart.AddSeries(ChartSeries.CreateRegionMap(new ChartSeriesId("old-regions"), "Old regions", definition, new[] { new ChartRegionMapItem("A", 1) }));
        else if (kind == "base") chart.Options.MapBaseLayers.Add(new ChartMapLayer(definition, null, ChartColor.Black, 1, "old-context"));
        else if (kind == "overlay") chart.Options.MapOverlayLayers.Add(new ChartMapLayer(definition, null, ChartColor.Black, 1, "old-context"));
        else chart.Options.RegionMapBounds = new ChartRect(170, -10, 20, 40);
        Assert.Contains("antimeridian", Assert.Throws<NotSupportedException>(() => MapLabelClusterDepthTests.Prepare(chart)).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(170, chart.Options.MapViewport.MinimumLongitude); Assert.Equal(-170, chart.Options.MapViewport.MaximumLongitude);
    }

    [Fact]
    public void IndependentTilesStillComposeAndPreparedRawIdentitiesSurviveUpdatesAndFullWorldReset() {
        var chart = DatelineMapRecipe.Create();
        var tiles = ChartSeries.CreateTileMap(new ChartSeriesId("inset"), "Inset", new ChartTileMapDefinition("inset", "Inset", new[] { new ChartTileMapRegion("A", "Alpha", 0, 0) }), new[] { new ChartRegionMapItem("A", 7) }).WithMapInset(new ChartRect(.02, .02, .25, .2));
        chart.AddSeries(tiles); var prepared = MapLabelClusterDepthTests.Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "tile-map-region-source"));
        chart.Series[0].UpdateMapPoint(0, new ChartMapPoint("Fiji moved", -179, -18, 70));
        var updated = MapLabelClusterDepthTests.Prepare(chart);
        Assert.Equal("fiji", MapLabelClusterDepthTests.Groups(updated, "dotted-map-point-source")[0].Metadata["data-cfx-observation-id"]);
        Assert.Equal("-179", MapLabelClusterDepthTests.Groups(updated, "dotted-map-point-source")[0].Metadata["data-cfx-longitude"]);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        chart.WithMapViewport(ChartMapViewport.World());
        var world = MapLabelClusterDepthTests.Prepare(chart); Assert.DoesNotContain(MapLabelClusterDepthTests.Groups(world, "geographic-map"), group => group.Metadata.ContainsKey("data-cfx-crosses-antimeridian"));
        var marks = world.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray(); Assert.Equal(5, marks.Length);
        Assert.True(marks[2].Cx > marks[3].Cx); // Ordered World keeps +180/-180 on opposite frame edges, preserving the incumbent contract.
    }

    [Fact]
    public void EquivalentSeamContributorsCanUseTheExistingCountBadgeAndEndpointClearancePolicies() {
        var chart = DatelineMapRecipe.Create();
        var series = chart.Series[0].WithMapClustering(new ChartMapClustering(.01, 2, 12));
        series.ReplaceMapConnectors(series.MapConnectors.Select(route => route.WithEndpointClearance(2)));
        var prepared = MapLabelClusterDepthTests.Prepare(chart); MapLabelClusterDepthTests.Capture(prepared, "dateline-cluster-clearance");
        var cluster = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-cluster-source"));
        Assert.Equal("2,3", cluster.Metadata["data-cfx-source-points"]); Assert.Equal("2", cluster.Metadata["data-cfx-source-count"]);
        var sources = MapLabelClusterDepthTests.Groups(prepared, "dotted-map-point-source");
        Assert.Equal("180", sources[2].Metadata["data-cfx-longitude"]); Assert.Equal("-180", sources[3].Metadata["data-cfx-longitude"]);
        Assert.All(sources.Skip(2).Take(2), source => Assert.Equal("false", source.Metadata["data-cfx-visible"]));
        Assert.Equal(new[] { "seam-east", "seam-west" }, sources.Skip(2).Take(2).Select(source => source.Metadata["data-cfx-observation-id"]));
        var route = MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source")[0];
        Assert.Equal("178,-18;-172,-14", route.Metadata["data-cfx-route-coordinates"]); Assert.Equal("2", route.Metadata["data-cfx-endpoint-clearance"]);
        Assert.Equal("true", route.Metadata["data-cfx-visible"]);
    }

    [Fact]
    public void ExistingFixedProjectionTopologyRejectsTheNewUnsupportedWindowAndKeepsItsOrderedControl() {
        var chart = TopologyChart.Create().WithViewport(520, 360).WithLayout(TopologyLayoutMode.Geographic)
            .AddNode("site", "Site", 0, 0).WithNodeCoordinates("site", 178, -18);
        chart.WithMapViewport(ChartMapViewport.AcrossAntimeridian("Pacific", 170, -170, -30, 10));
        Assert.Contains("ordered", Assert.Throws<NotSupportedException>(() => chart.ToSvg()).Message);
        chart.WithMapViewport(ChartMapViewport.World()); Assert.Contains("topology", chart.ToSvg());
    }
}

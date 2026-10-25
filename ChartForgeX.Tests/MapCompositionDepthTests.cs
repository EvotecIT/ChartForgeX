using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapCompositionDepthTests {
    [Theory]
    [InlineData("arbitrary", VisualThemeMode.Light)]
    [InlineData("arbitrary", VisualThemeMode.Dark)]
    [InlineData("compact-arbitrary", VisualThemeMode.Light)]
    [InlineData("compact-arbitrary", VisualThemeMode.Dark)]
    public void ArbitraryRegionPeersRetainOneNumericFrameAndIndependentNativeValues(string variant, VisualThemeMode mode) {
        var chart = MapCompositionDepthRecipe.Create(variant, mode);
        var prepared = Prepare(chart);
        var frame = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "arbitrary-map");
        Assert.Equal("arbitrary", frame.Metadata["data-cfx-projection"]);
        Assert.Equal("100", frame.Metadata["data-cfx-source-left"]); Assert.Equal("200", frame.Metadata["data-cfx-source-top"]);
        Assert.Equal("200", frame.Metadata["data-cfx-source-width"]); Assert.Equal("100", frame.Metadata["data-cfx-source-height"]);
        var regions = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "region-map-region").ToArray();
        Assert.Equal(2, regions.Length);
        Assert.Equal(regions[0].Commands.Max(command => command.X), regions[1].Commands.Min(command => command.X), 9);
        Assert.Equal(new[] { "82", "64" }, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "region-map-region-source").Select(node => node.Metadata["data-cfx-value"]));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "map-color-scale-source"));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "dotted-map-point");
        AssertScriptFreeAndDimensions(prepared, variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800, 520);
        Capture(prepared, "map-depth-" + variant + "-" + mode);
    }

    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    public void GeographicTileInsetUsesExistingTilePaintAndKeepsItsOwnFactsKeysAndIdentity(string variant, VisualThemeMode mode) {
        var chart = MapCompositionDepthRecipe.Create(variant, mode);
        var tile = chart.Series[2].WithObservationIds(new[] { new ChartObservationId("central"), new ChartObservationId("north"), new ChartObservationId("west") });
        var prepared = Prepare(chart);
        var root = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "geographic-map");
        Assert.Equal("equirectangular", root.Metadata["data-cfx-projection"]);
        var inset = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "tile-map");
        Assert.Equal("independent-tiles", inset.Metadata["data-cfx-coordinate-system"]);
        Assert.Equal("0.02", inset.Metadata["data-cfx-inset-left"]); Assert.Equal("0.48", inset.Metadata["data-cfx-inset-width"]);
        Assert.Contains("Summary coverage", inset.Metadata["aria-label"]);
        Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(node => node.Role == "tile-map-region"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(node => node.Role == "region-map-region"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Count(node => node.Role == "dotted-map-point"));
        Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "map-color-scale-source"));
        var target = Source(prepared, "tile-map-region-source", "summary", "west");
        Assert.Equal("92", (string?)target.Attribute("data-cfx-value")); Assert.Equal("92 %", (string?)target.Attribute("data-cfx-formatted-value"));
        var tileSources = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "tile-map-region-source").ToArray();
        Assert.All(tileSources, source => Assert.Equal("2", source.Metadata["data-cfx-series"]));
        var oldSvg = prepared.ToSvg(); var oldPng = prepared.ToPng();
        tile.UpdateRegionValue(2, new ChartRegionMapItem("W", 35)); chart.MoveSeries(tile, 0);
        var updated = Prepare(chart);
        Assert.Equal("0", (string?)Source(updated, "tile-map-region-source", "summary", "west").Attribute("data-cfx-series"));
        Assert.Equal("35", (string?)Source(updated, "tile-map-region-source", "summary", "west").Attribute("data-cfx-value"));
        Assert.Equal(oldSvg, prepared.ToSvg()); Assert.Equal(oldPng, prepared.ToPng());
        AssertScriptFreeAndDimensions(prepared, variant == "compact" ? 390 : 800, 600);
        Capture(prepared, "map-depth-" + variant + "-" + mode);
        Capture(updated, "map-depth-" + variant + "-" + mode + "-reordered");
    }

    [Theory]
    [InlineData(ChartMapGeoJsonProjection.Equirectangular)]
    [InlineData(ChartMapGeoJsonProjection.WebMercator)]
    public void TileInsetNeverChangesTheGeographicPeerProjectionOrNativePointPositions(ChartMapGeoJsonProjection projection) {
        var chart = GeographicMapCompositionTests.Fixture(projection, 800, VisualThemeMode.Light).WithLegend(false);
        var before = Prepare(chart).Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray();
        var tile = Tiles().WithMapInset(new ChartRect(.65, .65, .3, .3));
        chart.AddSeries(tile);
        var prepared = Prepare(chart);
        var after = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray();
        Assert.Equal(before.Select(node => (node.Cx, node.Cy, node.Rx, node.Ry)), after.Select(node => (node.Cx, node.Cy, node.Rx, node.Ry)));
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "geographic-map");
        Assert.Equal(projection == ChartMapGeoJsonProjection.WebMercator ? "web-mercator" : "equirectangular", source.Metadata["data-cfx-projection"]);
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(node => node.Role == "tile-map-region"));
        Capture(prepared, "map-depth-tile-" + projection);
    }

    [Fact]
    public void InsetValidationRejectsUnplacedOrAmbiguousMixturesAndClearingPreservesStandaloneOutput() {
        var tile = Tiles();
        Assert.Throws<ArgumentOutOfRangeException>(() => tile.WithMapInset(new ChartRect(0, 0, 0, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => tile.WithMapInset(new ChartRect(-.01, 0, .5, .5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => tile.WithMapInset(new ChartRect(.9, 0, .2, .5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => tile.WithMapInset(new ChartRect(0, .9, .5, .2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRect(double.NaN, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRect(0, 0, double.PositiveInfinity, 1));
        tile.WithMapInset(new ChartRect(.1, .1, .4, .4));
        var retained = tile.MapInset;
        Assert.Throws<ArgumentOutOfRangeException>(() => tile.WithMapInset(new ChartRect(.9, 0, .2, .5)));
        Assert.Equal(retained, tile.MapInset); tile.WithMapInset(null);
        var standalone = Chart.Create().WithSize(520, 300).AddSeries(tile);
        var original = Prepare(standalone);
        tile.WithMapInset(new ChartRect(0, 0, 1, 1));
        Assert.Throws<InvalidOperationException>(() => Prepare(standalone));
        tile.WithMapInset(null);
        tile.WithColorScale(ChartColorScale.Sequential(ChartColor.Black, ChartColor.White));
        Assert.Contains("inset", Assert.Throws<InvalidOperationException>(() => Prepare(standalone)).Message);
        tile.WithColorScale(null);
        Assert.Equal(original.ToSvg(), Prepare(standalone).ToSvg()); Assert.Equal(original.ToPng(), Prepare(standalone).ToPng());
        var points = ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Sites", new[] { new ChartMapPoint("Site", 0, 0) });
        Assert.Throws<InvalidOperationException>(() => points.WithMapInset(new ChartRect(0, 0, 1, 1)));
        var unplaced = Chart.Create().AddSeries(points).AddSeries(Tiles());
        Assert.Throws<InvalidOperationException>(() => Prepare(unplaced));
        var arbitrary = MapCompositionDepthRecipe.Create("arbitrary");
        arbitrary.AddSeries(ChartSeries.CreateDottedMap(new ChartSeriesId("mixed"), "Mixed", new[] { new ChartMapPoint("Site", 0, 0) }));
        Assert.Contains("projection", Assert.Throws<InvalidOperationException>(() => Prepare(arbitrary)).Message);
        var viewport = MapCompositionDepthRecipe.Create("arbitrary").WithMapViewport(ChartMapViewport.World());
        Assert.Throws<InvalidOperationException>(() => Prepare(viewport));
    }

    [Theory]
    [InlineData(ChartSeriesKind.RegionMap, false)]
    [InlineData(ChartSeriesKind.RegionMap, true)]
    [InlineData(ChartSeriesKind.TileMap, false)]
    [InlineData(ChartSeriesKind.TileMap, true)]
    public void EmptyPublicMapConstructorWithoutGeometryFailsClearlyBeforeLegendOrComposition(ChartSeriesKind kind, bool mixed) {
        var missing = new ChartSeries(new ChartSeriesId("missing"), "Missing", kind, Array.Empty<ChartPoint>());
        if (mixed && kind == ChartSeriesKind.TileMap) missing.WithMapInset(new ChartRect(.1, .1, .3, .3));
        var chart = Chart.Create().AddSeries(missing);
        if (mixed) chart.AddDottedMap("Peer", new[] { new ChartMapPoint("Site", 0, 0) });
        var error = Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Contains(kind == ChartSeriesKind.RegionMap ? "geometry definition" : "tile definition", error.Message);
    }

    [Fact]
    public void TypedEmptyRegionAndTileDefinitionsRetainTheirSupportedNoDataScenes() {
        var region = new ChartMapDefinition("empty-area", "Empty area", 100, 50, new[] { new ChartMapRegion("A", "Alpha", "M0 0H100V50H0Z") });
        var regionChart = Chart.Create().AddRegionMap("Empty", region, Array.Empty<ChartRegionMapItem>());
        Assert.NotEmpty(Prepare(regionChart).ToSvg()); Assert.NotEmpty(Prepare(regionChart).ToPng());
        var tileChart = Chart.Create().AddTileMap("Empty", new ChartTileMapDefinition("empty-tiles", "Empty tiles", new[] {
            new ChartTileMapRegion("A", "Alpha", 0, 0)
        }), Array.Empty<ChartRegionMapItem>());
        Assert.NotEmpty(Prepare(tileChart).ToSvg()); Assert.NotEmpty(Prepare(tileChart).ToPng());
    }

    private static ChartSeries Tiles() => ChartSeries.CreateTileMap(new ChartSeriesId("tiles"), "Tiles", new ChartTileMapDefinition("tiles", "Tiles", new[] {
        new ChartTileMapRegion("A", "Alpha", 0, 0), new ChartTileMapRegion("B", "Beta", 1, 0)
    }), new[] { new ChartRegionMapItem("A", 1) });
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    internal static XElement Source(PreparedVisual prepared, string role, string series, string observation) => Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), node =>
        (string?)node.Attribute("data-cfx-role") == role && (string?)node.Attribute("data-cfx-series-key") == series && (string?)node.Attribute("data-cfx-observation-id") == observation);
    internal static void AssertScriptFreeAndDimensions(PreparedVisual prepared, int width, int height) {
        Assert.DoesNotContain(XDocument.Parse(prepared.ToSvg()).Descendants(), node => node.Name.LocalName == "script");
        var image = RasterImageDecoder.Decode(prepared.ToPng()); Assert.Equal(width, image.Width); Assert.Equal(height, image.Height);
    }
    internal static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
    }
}

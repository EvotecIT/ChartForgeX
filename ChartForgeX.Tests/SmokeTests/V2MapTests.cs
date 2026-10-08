using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2MapTests {
    [Fact]
    public void RegionHolesRemainUnpaintedInSvgAndNativeRaster() {
        var definition = new ChartMapDefinition("hole", "Hole", 100, 100, new[] {
            new ChartMapRegion("A", "Ring", "M0 0H100V100H0Z M30 30H70V70H30Z") });
        var color = ChartColor.FromHex("#123456");
        var chart = Chart.Create().AddRegionMap("Values", definition, new[] { new ChartRegionMapItem("A", 10, color) })
            .WithMapLabels(false).WithMapScaleLegend(false).WithMapRegionStroke(null, 0).WithMapSurface(false);
        var scene = Compile(chart, 400, 240);
        var path = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "region-map-region");
        Assert.Equal(2, path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo)); Assert.True(path.Close); Assert.Equal(color, path.Fill);
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "region-map-region" && (string?)node.Attribute("fill-rule") == "evenodd");
        var image = VisualSceneRasterRenderer.Render(scene);
        Assert.Equal(0, image.Pixels[(120 * image.Width + 200) * 4 + 3]);
        Assert.True(image.Pixels[(20 * image.Width + 100) * 4 + 3] > 240);
    }

    [Fact]
    public void RegionCropAndOpenOverlayKeepSharedCoordinatesAndLayerOrder() {
        var definition = Definition();
        var overlay = new ChartMapDefinition("road", "Road", 100, 100, new[] { new ChartMapRegion("R", "Route", "M0 0Q50 100 100 0") });
        var chart = Chart.Create().AddRegionMap("Crop", definition, new[] { new ChartRegionMapItem("Alpha", 40) })
            .WithRegionMapBounds(new ChartRect(20, 10, 50, 60)).WithMapLabels(false).WithMapScaleLegend(false);
        chart.Options.MapBaseLayers.Add(new ChartMapLayer(definition, ChartColor.FromHex("#ABCDEF"), null, 0, "base-land"));
        chart.Options.MapOverlayLayers.Add(new ChartMapLayer(overlay, null, ChartColor.FromHex("#123456"), 2, "road"));
        var scene = Compile(chart);
        var groups = scene.Nodes.OfType<VisualSceneGroup>().ToArray();
        Assert.Contains(groups, group => group.Role == "region-map" && group.Metadata["data-cfx-source-left"] == "20" && group.Metadata["data-cfx-source-width"] == "50");
        var road = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "road");
        Assert.False(road.Close); Assert.Null(road.Fill); Assert.True(road.Commands.Count > 5);
        var ordered = scene.Nodes.ToList();
        Assert.True(ordered.FindIndex(node => node.Role == "base-land" && node is VisualScenePath) < ordered.FindIndex(node => node.Role == "region-map-region"));
        Assert.True(ordered.FindIndex(node => node.Role == "road" && node is VisualScenePath) > ordered.FindLastIndex(node => node.Role == "region-map-region"));
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Theory]
    [InlineData(ChartMapScaleLegendPosition.Bottom)]
    [InlineData(ChartMapScaleLegendPosition.Right)]
    public void ExplicitMapScaleKeepsDomainMidpointNoDataAndPointOverrides(ChartMapScaleLegendPosition position) {
        var low = ChartColor.FromHex("#123456"); var middle = ChartColor.FromHex("#ABCDEF"); var high = ChartColor.FromHex("#654321");
        var missing = ChartColor.FromHex("#CCCCCC"); var explicitPoint = ChartColor.FromHex("#987654");
        var scale = ChartMapColorScale.Diverging(low, middle, high, 20).WithValueRange(0, 100).WithNoDataColor(missing).WithLabels("Low", "Mid", "High");
        var chart = Chart.Create().AddRegionMap("Rate", Definition(), new[] { new ChartRegionMapItem("A", 20), new ChartRegionMapItem("B", 80, explicitPoint) })
            .WithMapColorScale(scale).WithMapScaleLegendPosition(position).WithMapLabels(false);
        chart.Options.ValueFormatter = value => value.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL")) + " %";
        var scene = Compile(chart);
        var regions = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "region-map-region").ToArray();
        Assert.Equal(middle, regions[0].Fill); Assert.Equal(explicitPoint, regions[1].Fill); Assert.Equal(missing, regions[2].Fill);
        Assert.Contains(scene.Regions, region => region.Label == "Mid 20,0 %");
        Assert.Contains(scene.Nodes.OfType<VisualSceneText>(), text =>
            text.Role is "map-scale-label" or "map-scale-midpoint-label" && string.Join("\n", text.Text.Lines.Select(line => line.Text)) == "Mid 20,0 %");
        Assert.Contains(scene.Regions, region => region.Label == "High · 100,0 %");
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "map-scale" && group.Metadata["data-cfx-midpoint-value"] == "20");
        Assert.Contains(scene.Nodes, node => node.Role == "map-scale-no-data");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MapScaleHonorsExplicitChartAndHostLegendVisibility(bool host) {
        var chart = Chart.Create().AddRegionMap("Values", Definition(), new[] { new ChartRegionMapItem("A", 10) }).WithMapLabels(false);
        var visible = Compile(chart);
        Assert.Contains(visible.Nodes, node => node.Role == "map-scale");
        var hiddenContext = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(440, 330)),
            frame: new VisualFrame(showLegend: host ? false : null));
        if (!host) chart.WithLegend(false);
        var hidden = chart.Prepare(hiddenContext).Scene;
        Assert.DoesNotContain(hidden.Nodes, node => node.Role == "map-scale");
        Assert.Equal(3, hidden.Nodes.Count(node => node.Role == "region-map-region"));
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(hidden).Pixels);
    }

    [Theory]
    [InlineData(140, 100)]
    [InlineData(700, 180)]
    public void TileGeometryAndMeasuredLabelsFitTheViewportAndKeepCompleteDescriptions(int width, int height) {
        const string full = "A very long canonical region code retained in artifact descriptions";
        var definition = new ChartTileMapDefinition("tiles", "Tiles", new[] {
            new ChartTileMapRegion(full, "Complete name", 0, 0), new ChartTileMapRegion("B", "Second", 1, 0), new ChartTileMapRegion("C", "Third", 0, 1) });
        var chart = Chart.Create().AddTileMap("Long scale title", definition, new[] { new ChartRegionMapItem(full, 0), new ChartRegionMapItem("B", 70) });
        chart.Series[0].WithPointFillPattern(0, ChartFillPattern.Crosshatch);
        var scene = Compile(chart, width, height);
        foreach (var path in scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "tile-map-region")) {
            Assert.Equal(6, path.Commands.Count); Assert.True(path.Close);
            Assert.All(path.Commands, command => { Assert.InRange(command.X, 0, width); Assert.InRange(command.Y, 0, height); });
        }
        foreach (var text in scene.Nodes.OfType<VisualSceneText>()) {
            var left = text.X - (text.Alignment == TextAlignment.Center ? text.Text.Metrics.Width / 2 : 0);
            Assert.InRange(left, -.001, width); Assert.True(left + text.Text.Metrics.Width <= width + .001);
            Assert.True(text.Baseline - text.Text.Ascent >= -.001 && text.Baseline - text.Text.Ascent + text.Text.Metrics.Height <= height + .001);
        }
        Assert.Contains(scene.Nodes, node => node.Role == "tile-map-pattern");
        var artifact = new PreparedVisual(scene).ToArtifact("tile-map", VisualArtifactKind.Chart);
        Assert.Contains(artifact.Regions, region => region.Label == full); Assert.Contains(full, artifact.ToSvg());
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void DottedLabelLeadersJoinTheirMarkerToMeasuredTextWithSharedHaloAndNativeInk() {
        var color = ChartColor.FromHex("#DC2626");
        var chart = Chart.Create().AddDottedMap("Cities", new[] { new ChartMapPoint("Warsaw", 19.1451, 51.9194, 142, color) })
            .WithMapViewport(ChartMapViewport.Europe()).WithDataLabels();
        var scene = Compile(chart);
        var point = Assert.Single(scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "dotted-map-point");
        var label = Assert.Single(scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "dotted-map-data-label");
        var leader = Assert.Single(scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "dotted-map-label-leader");
        var halo = Assert.Single(scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "dotted-map-label-leader-halo");
        Assert.Equal(color, leader.Stroke);
        Assert.True(halo.StrokeWidth > leader.StrokeWidth && halo.Stroke!.Value.A < color.A);
        var startDistance = Math.Sqrt(Math.Pow(leader.Start.X - point.Cx, 2) + Math.Pow(leader.Start.Y - point.Cy, 2));
        Assert.True(startDistance > point.Rx, "Leaders must start outside the painted observation circle.");
        var textBounds = new ChartRect(label.X, label.Baseline - label.Text.Ascent, label.Text.Metrics.Width, label.Text.Metrics.Height);
        Assert.False(leader.End.X >= textBounds.Left && leader.End.X <= textBounds.Right && leader.End.Y >= textBounds.Top && leader.End.Y <= textBounds.Bottom,
            "Leader strokes must stop before the measured label rectangle.");
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        var x = (int)Math.Round((leader.Start.X + leader.End.X) / 2); var y = (int)Math.Round((leader.Start.Y + leader.End.Y) / 2);
        Assert.Contains(Enumerable.Range(-1, 3).SelectMany(dy => Enumerable.Range(-1, 3).Select(dx => ((y + dy) * image.Width + x + dx) * 4)),
            pixel => image.Pixels[pixel] > 150 && image.Pixels[pixel + 1] < 90 && image.Pixels[pixel + 3] > 0);
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "dotted-map-label-leader-halo");
        chart.WithDataLabels(false);
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role?.StartsWith("dotted-map-label-leader", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void DottedWeightsUseSeparateMagnitudeValuesAndRetainInvisibleSourceIdentity() {
        var color = ChartColor.FromHex("#123456"); var calls = 0;
        var chart = Chart.Create().AddDottedMap("Cities", new[] { new ChartMapPoint("Small", 15, 50, 10), new ChartMapPoint("Large", 20, 52, 100, color),
            new ChartMapPoint("Outside", -100, 30, 40) }).WithMapViewport(ChartMapViewport.Poland()).WithDataLabels();
        chart.Options.ValueFormatter = value => { calls++; return value.ToString(CultureInfo.InvariantCulture) + " observations"; };
        var scene = Compile(chart); var points = scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray();
        Assert.Equal(2, points.Length); Assert.True(points[1].Rx > points[0].Rx); Assert.Equal(color, points[1].Fill); Assert.Equal(3, calls);
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-2" && region.Label!.Contains("Outside: 40 observations"));
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-0-point-2" && group.Metadata["data-cfx-visible"] == "false");
        Assert.Contains("dotted-map-outline", VisualSceneSvgRenderer.Render(scene)); Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void DottedArcAndWaypointRoutesAreNativeTrimmedAndDetachedFromMutableWaypoints() {
        var chart = Chart.Create().AddDottedMap("Route", new[] { new ChartMapPoint("Start", 0, 0), new ChartMapPoint("End", 20, 20) }).WithDataLabels();
        chart.Options.MapViewport = new ChartMapViewport("Custom", -10, 30, -10, 30);
        var route = new ChartMapConnector("A complete waypoint route label retained after fitting", new[] {
            new ChartMapPoint("A", 0, 0), new ChartMapPoint("Via", 12, 3), new ChartMapPoint("B", 20, 20) });
        chart.Options.MapConnectors.Add(new ChartMapConnector("Direct", 0, 0, 20, 20)); chart.Options.MapConnectors.Add(route);
        var scene = Compile(chart); var paths = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "dotted-map-connector").ToArray();
        Assert.Equal(2, paths.Length); Assert.All(paths, path => Assert.Contains(path.Commands, command => command.Kind == ChartPathCommandKind.CubicTo));
        var source = scene.Nodes.OfType<VisualSceneEllipse>().First(point => point.Role == "dotted-map-point");
        Assert.All(paths, path => Assert.True(Math.Sqrt(Math.Pow(path.Commands[0].X - source.Cx, 2) + Math.Pow(path.Commands[0].Y - source.Cy, 2)) > source.Rx));
        Assert.Equal(2, scene.Nodes.Count(node => node.Role == "dotted-map-connector-arrow"));
        var before = VisualSceneSvgRenderer.Render(scene); route.RoutePoints[1] = new ChartMapPoint("Changed", -5, -5);
        Assert.Equal(before, VisualSceneSvgRenderer.Render(scene));
        var artifact = new PreparedVisual(scene).ToArtifact("routes", VisualArtifactKind.Chart);
        Assert.Contains(artifact.Regions, region => region.Label == route.Label); Assert.Contains(route.Label, artifact.ToSvg());
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void PreparedRegionGeometryUsesTheProjectionAlreadySelectedAtGeoJsonImport() {
        const string geoJson = "{\"type\":\"Feature\",\"properties\":{\"code\":\"A\",\"name\":\"Area\"},\"geometry\":{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[10,0],[10,60],[0,60],[0,0]]]}}";
        var linear = ChartMapDefinition.FromGeoJson("linear", "Linear", geoJson);
        var mercator = ChartMapDefinition.FromGeoJson("mercator", "Mercator", geoJson, new ChartMapGeoJsonOptions { Projection = ChartMapGeoJsonProjection.WebMercator });
        VisualScene CompileDefinition(ChartMapDefinition definition) => Compile(Chart.Create().AddRegionMap("Value", definition, new[] { new ChartRegionMapItem("A", 50) }).WithMapLabels(false).WithMapScaleLegend(false));
        var linearPath = Assert.Single(CompileDefinition(linear).Nodes.OfType<VisualScenePath>(), path => path.Role == "region-map-region");
        var mercatorPath = Assert.Single(CompileDefinition(mercator).Nodes.OfType<VisualScenePath>(), path => path.Role == "region-map-region");
        var linearWidth = linearPath.Commands.Max(command => command.X) - linearPath.Commands.Min(command => command.X);
        var mercatorWidth = mercatorPath.Commands.Max(command => command.X) - mercatorPath.Commands.Min(command => command.X);
        Assert.True(mercatorWidth < linearWidth);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void CanonicalThemeStateAndFractionalWeightsRemainVisibleAcrossModes(VisualThemeMode mode) {
        var chart = Chart.Create().AddDottedMap("Fractional observations", new[] { new ChartMapPoint("Low", -20, 20, .1), new ChartMapPoint("High", 20, 30, .2) });
        chart.Series[0].StateRole = ChartSeriesState.Warning;
        var context = new VisualRenderContext(themeMode: mode); var scene = Compile(chart, context: context);
        var points = scene.Nodes.OfType<VisualSceneEllipse>().Where(point => point.Role == "dotted-map-point").ToArray();
        Assert.True(points[1].Rx > points[0].Rx * 1.4);
        Assert.All(points, point => Assert.Equal(context.Theme.Resolve(mode).Status.Medium.Fill, point.Fill));
        if (mode == VisualThemeMode.Light) Assert.DoesNotContain(scene.Nodes, node => node.Role == "dotted-map-land-dot");
        else Assert.Contains(scene.Nodes, node => node.Role == "dotted-map-land-dot");
        chart.WithMapSurface(false); chart.Options.ShowGrid = false;
        Assert.DoesNotContain(Compile(chart, context: context).Nodes, node => node.Role is "dotted-map-surface" or "dotted-map-graticule");
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void MutatedInvalidMapInputsFailBeforeExportAndScaleVisibilityIsIndependent() {
        var chart = Chart.Create().AddDottedMap("Point", new[] { new ChartMapPoint("A", 0, 0) });
        chart.Options.MapViewport = default; Assert.Throws<InvalidOperationException>(() => Compile(chart));
        chart.Options.MapViewport = ChartMapViewport.World(); chart.Series[0].Points[0] = new ChartPoint(200, 0);
        Assert.Throws<InvalidOperationException>(() => Compile(chart));
        var region = Chart.Create().AddRegionMap("Area", Definition(), new[] { new ChartRegionMapItem("A", 20) }).WithMapLabels(false).WithMapScaleLegend(false);
        Assert.DoesNotContain(Compile(region).Nodes, node => node.Role is "region-map-label" or "map-scale-step");
        region.Options.XAxisLabels[0] = new ChartAxisLabel(1, "Unknown"); Assert.Throws<InvalidOperationException>(() => Compile(region));
    }

    private static ChartMapDefinition Definition() => new("test", "Test", 100, 100, new[] {
        new ChartMapRegion("A", "Alpha", "M0 0H50V50H0Z"), new ChartMapRegion("B", "Beta", "M50 0H100V50H50Z"), new ChartMapRegion("C", "Gamma", "M0 50H100V100H0Z") });
    private static VisualScene Compile(Chart chart, int width = 440, int height = 330, VisualRenderContext? context = null) {
        context ??= new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(width, height), context.Font);
        VisualMapCompiler.Build(chart, context, builder, new ChartRect(0, 0, width, height)); return builder.Build();
    }
}

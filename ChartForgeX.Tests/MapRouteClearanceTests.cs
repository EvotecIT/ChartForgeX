using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapRouteClearanceTests {
    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public void OptInClearanceUsesActualPeerEndpointMarksAndKeepsTheOriginalSourceRoute(int width, VisualThemeMode mode) {
        var route = new ChartMapConnector("Peer-owned endpoints", -5, 45, 25, 55).WithEndpointClearance(4)
            .WithFeature(new ChartMapFeature("peer-route", "{\"active\":true}"));
        var chart = Chart.Create().WithSize(width, 400).WithTheme(VisualTheme.Graphite(), mode).WithMapViewport(ChartMapViewport.Europe())
            .AddSeries(ChartSeries.CreateDottedMap(new ChartSeriesId("route-owner"), "Routes", Array.Empty<ChartMapPoint>()).ReplaceMapConnectors(new[] { route }))
            .AddSeries(ChartSeries.CreateDottedMap(new ChartSeriesId("marks"), "Endpoints", new[] { new ChartMapPoint("Start", -5, 45), new ChartMapPoint("End", 25, 55) }).WithMarkerRadius(18));
        var prepared = MapLabelClusterDepthTests.Prepare(chart); MapLabelClusterDepthTests.Capture(prepared, "map-peer-clearance-" + width + "-" + mode);
        var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "dotted-map-connector");
        var points = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray(); Assert.Equal(2, points.Length);
        var start = new ChartPoint(path.Commands[0].X, path.Commands[0].Y); var end = new ChartPoint(path.Commands[path.Commands.Count - 1].X, path.Commands[path.Commands.Count - 1].Y);
        Assert.True(Distance(start, new ChartPoint(points[0].Cx, points[0].Cy)) >= 22);
        Assert.True(Distance(end, new ChartPoint(points[1].Cx, points[1].Cy)) >= 22);
        Assert.Contains(path.Commands, command => command.Kind == ChartPathCommandKind.CubicTo);
        var source = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"));
        Assert.Equal("-5,45;25,55", source.Metadata["data-cfx-route-coordinates"]); Assert.Equal("peer-route", source.Metadata["data-cfx-feature-id"]);
        Assert.Equal("false", source.Metadata["data-cfx-route-collapsed"]);
        var retained = prepared.ToSvg(); chart.Series[0].ReplaceMapConnectors(new[] { route.WithEndpointClearance(null) });
        Assert.Equal(retained, prepared.ToSvg()); Assert.NotEqual(retained, MapLabelClusterDepthTests.Prepare(chart).ToSvg());
    }

    [Fact]
    public void SharedPathOwnerRetainsExactCubicInteriorsAndTheirEndpointTangents() {
        var original = new ChartPath(new[] { ChartPathCommand.MoveTo(0, 0), ChartPathCommand.CubicTo(0, 80, 100, 80, 100, 0) });
        ChartPoint At(double t) => new(300 * t * t - 200 * t * t * t, 240 * t * (1 - t));
        var expected = At(.25); var radius = Distance(new ChartPoint(0, 0), expected);
        var cropped = original.TrimEndpointDisks(new[] { (new ChartPoint(0, 0), radius) }, new[] { (new ChartPoint(100, 0), radius) });
        Assert.Equal(2, cropped.Commands.Count); Assert.Equal(ChartPathCommandKind.CubicTo, cropped.Commands[1].Kind);
        Assert.Equal(expected.X, cropped.Commands[0].X, 7); Assert.Equal(expected.Y, cropped.Commands[0].Y, 7);
        Assert.Equal(At(.75).X, cropped.Commands[1].X, 7); Assert.Equal(At(.75).Y, cropped.Commands[1].Y, 7);
        // de Casteljau [1/4,3/4] is independently known here: midpoint (50,60), vertical tangent directions retained.
        var start = new ChartPoint(cropped.Commands[0].X, cropped.Commands[0].Y);
        var c1 = new ChartPoint(cropped.Commands[1].Control1X, cropped.Commands[1].Control1Y);
        var c2 = new ChartPoint(cropped.Commands[1].Control2X, cropped.Commands[1].Control2Y);
        var end = new ChartPoint(cropped.Commands[1].X, cropped.Commands[1].Y);
        var middle = new ChartPoint((start.X + 3 * c1.X + 3 * c2.X + end.X) / 8, (start.Y + 3 * c1.Y + 3 * c2.Y + end.Y) / 8);
        Assert.Equal(50, middle.X, 7); Assert.Equal(60, middle.Y, 7);
        var derivative = new ChartPoint(600 * .25 * (1 - .25), 240 * (1 - 2 * .25));
        Assert.InRange(Math.Abs((c1.X - start.X) * derivative.Y - (c1.Y - start.Y) * derivative.X), 0, 1e-7);
    }

    [Fact]
    public void SharedEndpointTrimCrossesShortSegmentsHandlesTangencyAndCollapsesCoveredRoutes() {
        var path = new ChartPath(new[] { ChartPathCommand.MoveTo(0, 0), ChartPathCommand.LineTo(2, 0), ChartPathCommand.LineTo(4, 0), ChartPathCommand.LineTo(100, 0) });
        var cropped = path.TrimEndpointDisks(new[] { (new ChartPoint(0, 0), 10d) }, new[] { (new ChartPoint(100, 0), 10d) });
        Assert.Equal(10, cropped.Commands[0].X, 8); Assert.Equal(90, cropped.Commands[cropped.Commands.Count - 1].X, 8);
        Assert.Empty(path.TrimEndpointDisks(new[] { (new ChartPoint(0, 0), 60d) }, new[] { (new ChartPoint(100, 0), 60d) }).Commands);
        var tangent = new ChartPath(new[] { ChartPathCommand.MoveTo(-20, 10), ChartPathCommand.LineTo(20, 10) });
        var preserved = tangent.TrimEndpointDisks(new[] { (new ChartPoint(0, 0), 10d) }, Array.Empty<(ChartPoint, double)>());
        Assert.Equal(tangent.Commands.Select(command => (command.Kind, command.X, command.Y)), preserved.Commands.Select(command => (command.Kind, command.X, command.Y)));
    }

    [Fact]
    public void FullyCoveredPublicRouteHasTruthfulCollapsedSourceAndClearanceValidationIsAtomic() {
        var route = new ChartMapConnector("Short source route", 0, 50, .1, 50).WithEndpointClearance(500);
        Assert.Throws<ArgumentOutOfRangeException>(() => route.WithEndpointClearance(-1)); Assert.Throws<ArgumentOutOfRangeException>(() => route.WithEndpointClearance(double.PositiveInfinity));
        var chart = Chart.Create().WithSize(390, 320).WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("Sites", new[] { new ChartMapPoint("Start", 0, 50), new ChartMapPoint("End", .1, 50) });
        chart.Series[0].ReplaceMapConnectors(new[] { route });
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "dotted-map-connector");
        var source = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"));
        Assert.Equal("true", source.Metadata["data-cfx-route-collapsed"]); Assert.Equal("false", source.Metadata["data-cfx-visible"]);
        Assert.Equal("0,50;0.1,50", source.Metadata["data-cfx-route-coordinates"]);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "map.route-clearance-collapsed");
    }
    private static double Distance(ChartPoint a, ChartPoint b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}

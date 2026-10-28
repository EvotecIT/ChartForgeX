using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Periodic map paint keeps one source route, exact native curves, and independently aligned endpoint marks.</summary>
public sealed class MapPeriodicRouteCopyTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SharedNativeLineAndCubicSlicesRetainEveryVisibleCopyWithoutBridgingTheWorld(bool cubic) {
        var original = new ChartPath(new[] { ChartPathCommand.MoveTo(165, 0), cubic
            ? ChartPathCommand.CubicTo(170, 8, 195, 8, 200, 0) : ChartPathCommand.LineTo(200, 0) });
        var paths = original.PeriodicRectangleFragments(new ChartRect(-170, -30, 350, 60), 360, out var clipped);
        Assert.True(clipped); Assert.Equal(2, paths.Count);
        var left = Assert.Single(paths, path => path.Bounds().Right < 0);
        var right = Assert.Single(paths, path => path.Bounds().Left > 0);
        Assert.Equal(-170, left.Commands[0].X, 7); Assert.Equal(-160, left.Commands[left.Commands.Count - 1].X, 7);
        Assert.Equal(165, right.Commands[0].X, 7); Assert.Equal(180, right.Commands[right.Commands.Count - 1].X, 7);
        Assert.All(paths, path => {
            Assert.Equal(2, path.Commands.Count); Assert.Equal(ChartPathCommandKind.MoveTo, path.Commands[0].Kind);
            Assert.Equal(cubic ? ChartPathCommandKind.CubicTo : ChartPathCommandKind.LineTo, path.Commands[1].Kind);
        });
        if (cubic) {
            // Original cubic at t=3/4 is independently (193.125,4.5). Find its parameter in the retained left slice.
            double X(double t) => 165 + 15 * t + 60 * t * t - 40 * t * t * t;
            var low = 0d; var high = 1d;
            for (var index = 0; index < 64; index++) { var middle = low + (high - low) / 2; if (X(middle) < 190) low = middle; else high = middle; }
            var first = low + (high - low) / 2; var t = (.75 - first) / (1 - first);
            var start = left.Commands[0]; var end = left.Commands[1]; var inverse = 1 - t;
            var x = start.X * inverse * inverse * inverse + 3 * end.Control1X * inverse * inverse * t
                + 3 * end.Control2X * inverse * t * t + end.X * t * t * t;
            var y = start.Y * inverse * inverse * inverse + 3 * end.Control1Y * inverse * inverse * t
                + 3 * end.Control2Y * inverse * t * t + end.Y * t * t * t;
            Assert.Equal(-166.875, x, 7); Assert.Equal(4.5, y, 7);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OriginalOnlyWindowRetainsTheExactExistingNativePathAndClippedDisposition(bool partiallyClipped) {
        var original = new ChartPath(new[] { ChartPathCommand.MoveTo(178, 0), ChartPathCommand.CubicTo(180, -2, 186, -2, 188, 0) });
        var paths = original.PeriodicRectangleFragments(new ChartRect(170, -10, partiallyClipped ? 15 : 20, 20), 360, out var clipped);
        Assert.Same(original, Assert.Single(paths)); Assert.Equal(partiallyClipped, clipped);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void EndpointClearanceFollowsBothPeerClusterCopiesAndRetainsTheirRawIdentities(bool mercator, bool reverse) {
        var chart = Clustered(800, VisualThemeMode.Light, mercator, reverse);
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        MapLabelClusterDepthTests.Capture(prepared, "near-world-cluster-clearance-" + mercator + "-" + reverse);
        var clusters = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-cluster").OrderBy(node => node.Cx).ToArray();
        Assert.Equal(2, clusters.Length);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-connector").ToArray(); Assert.Equal(2, paths.Length);
        var midpoint = (clusters[0].Cx + clusters[1].Cx) / 2;
        foreach (var cluster in clusters) {
            var path = Assert.Single(paths, path => cluster.Cx < midpoint ? new ChartPath(path.Commands).Bounds().Right < midpoint : new ChartPath(path.Commands).Bounds().Left > midpoint);
            var ends = new[] { new ChartPoint(path.Commands[0].X, path.Commands[0].Y), new ChartPoint(path.Commands[path.Commands.Count - 1].X, path.Commands[path.Commands.Count - 1].Y) };
            var distance = ends.Min(point => Distance(point, new ChartPoint(cluster.Cx, cluster.Cy)));
            Assert.InRange(distance, cluster.Rx + 2, cluster.Rx + 16);
            Assert.Equal(1, path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        }
        var route = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"));
        Assert.Equal("periodic-route", route.Metadata["data-cfx-feature-id"]);
        Assert.Equal("true", route.Metadata["data-cfx-route-view-clipped"]); Assert.Equal("false", route.Metadata["data-cfx-route-collapsed"]);
        Assert.Equal(reverse ? "-130,0;140,0" : "140,0;-130,0", route.Metadata["data-cfx-route-coordinates"]);
        var sources = MapLabelClusterDepthTests.Groups(prepared, "dotted-map-point-source");
        Assert.Equal(new[] { "start", "start-peer", "end", "end-peer" }, sources.Select(group => group.Metadata["data-cfx-observation-id"]));
        Assert.Equal(4, chart.Series[1].MapPoints.Count); Assert.Empty(chart.Series[0].MapPoints);
        var retained = prepared.ToSvg(); chart.Series[1].WithMapClustering(null); Assert.Equal(retained, prepared.ToSvg());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WindingWaypointsExpandByVisibleSegmentsWithoutDuplicatingTheSourceFeature(bool mercator) {
        var raw = Enumerable.Range(0, 49).Select(index => new ChartMapPoint("Stop " + index,
            (165 + index * 150 + 180) % 360 - 180, index % 2 == 0 ? -2 : 2)).ToArray();
        var route = new ChartMapConnector("Many turns", raw).WithFeature(new ChartMapFeature("winding", "{\"kind\":\"route\"}"));
        var chart = Chart.Create().WithSize(800, 360).WithLegend(false).WithMapViewport(ChartMapViewport.AcrossAntimeridian("Winding", -170, -180, -30, 30,
            mercator ? ChartMapGeoJsonProjection.WebMercator : ChartMapGeoJsonProjection.Equirectangular))
            .AddDottedMap("Source", new[] { raw[0], raw[raw.Length - 1] });
        chart.Series[0].ReplaceMapConnectors(new[] { route });
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        MapLabelClusterDepthTests.Capture(prepared, "near-world-winding-" + mercator);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-connector").ToArray();
        Assert.True(paths.Length > 2); Assert.InRange(paths.Sum(path => path.Commands.Count), 2, raw.Length * 6);
        Assert.All(paths, path => Assert.Equal(1, path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo)));
        Assert.Contains(paths.SelectMany(path => path.Commands), command => command.Kind == ChartPathCommandKind.CubicTo);
        var source = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"));
        Assert.Equal("winding", source.Metadata["data-cfx-feature-id"]); Assert.Equal("49", source.Metadata["data-cfx-waypoint-count"]);
        Assert.Equal(raw.Select(point => (point.Longitude, point.Latitude)), chart.Series[0].MapConnectors[0].RoutePoints.Select(point => (point.Longitude, point.Latitude)));
        Assert.Equal(2, MapLabelClusterDepthTests.Groups(prepared, "dotted-map-point-source").Length);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void VisibleLabelsKeepOneStableCaptionFactForDefaultAndPeriodicRoutePaint(bool periodic) {
        var chart = periodic ? Clustered(800, VisualThemeMode.Light)
            : Chart.Create().WithSize(800, 360).WithLegend(false).WithMapViewport(ChartMapViewport.Europe())
                .AddDottedMap("Default source", new[] { new ChartMapPoint("Start", -5, 45), new ChartMapPoint("End", 25, 55) });
        if (!periodic) chart.Series[0].ReplaceMapConnectors(new[] { new ChartMapConnector("Default route", -5, 45, 25, 55) });
        chart.WithDataLabels(); chart.Options.MapCallouts = new ChartMapCallouts();
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        MapLabelClusterDepthTests.Capture(prepared, "single-periodic-route-caption-" + periodic);
        var region = Assert.Single(prepared.Regions, item => item.Role == "dotted-map-connector-label");
        Assert.Equal("map-route-0-label", region.Id); Assert.Equal(periodic ? "Across the seam" : "Default route", region.Label);
        var source = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"));
        Assert.Equal("map-route-0", source.Id);
        Assert.Equal("map-route-0", Assert.Single(prepared.Regions, item => item.Role == "dotted-map-connector").Id);
        var caption = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-label-source"));
        Assert.Equal("map-route-0-label-source", caption.Id);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-connector").ToArray();
        Assert.Equal(periodic ? 2 : 1, paths.Length);
        Assert.All(paths, path => Assert.Equal(1, path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo)));
        var selected = paths.OrderByDescending(path => {
            var points = new ChartPath(path.Commands).Flatten(3); var length = 0d;
            for (var index = 1; index < points.Count; index++) length += Distance(points[index - 1], points[index]);
            return length;
        }).First();
        var bounds = new ChartPath(selected.Commands).Bounds();
        var x = double.Parse(caption.Metadata["data-cfx-anchor-x"], CultureInfo.InvariantCulture);
        var y = double.Parse(caption.Metadata["data-cfx-anchor-y"], CultureInfo.InvariantCulture);
        Assert.InRange(x, bounds.Left - 1e-7, bounds.Right + 1e-7); Assert.InRange(y, bounds.Top - 1e-7, bounds.Bottom + 1e-7);
        var retainedSvg = prepared.ToSvg(); var retainedPng = prepared.ToPng(); chart.WithDataLabels(false);
        Assert.Equal(retainedSvg, prepared.ToSvg()); Assert.Equal(retainedPng, prepared.ToPng());
    }

    internal static Chart Clustered(int width, VisualThemeMode mode, bool mercator = false, bool reverse = false) {
        var route = reverse ? new ChartMapConnector("Across the seam", -130, 0, 140, 0) : new ChartMapConnector("Across the seam", 140, 0, -130, 0);
        route = route.WithEndpointClearance(2).WithFeature(new ChartMapFeature("periodic-route", "{\"active\":true}"));
        var owner = ChartSeries.CreateDottedMap(new ChartSeriesId("routes"), "Route source", Array.Empty<ChartMapPoint>()).ReplaceMapConnectors(new[] { route });
        var points = ChartSeries.CreateDottedMap(new ChartSeriesId("marks"), "Endpoint marks", new[] {
            new ChartMapPoint("Start", 140, 0), new ChartMapPoint("Start peer", 140.05, 0),
            new ChartMapPoint("End", -130, 0), new ChartMapPoint("End peer", -129.95, 0)
        }).WithObservationIds(new[] { "start", "start-peer", "end", "end-peer" }.Select(value => new ChartObservationId(value)))
            .WithMapClustering(new ChartMapClustering(80, 2, 12));
        return Chart.Create().WithSize(width, 360).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode).WithLegend(false)
            .WithMapViewport(ChartMapViewport.AcrossAntimeridian("Peer endpoints", -170, -180, -30, 30,
                mercator ? ChartMapGeoJsonProjection.WebMercator : ChartMapGeoJsonProjection.Equirectangular)).AddSeries(owner).AddSeries(points);
    }
    private static double Distance(ChartPoint a, ChartPoint b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}

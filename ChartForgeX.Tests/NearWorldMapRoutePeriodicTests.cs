using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Visible periodic route pieces retain one source identity in supported antimeridian views.</summary>
public sealed class NearWorldMapRoutePeriodicTests {
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, false, false, true)]
    [InlineData(true, true, true, true)]
    public void NearWorldWindowPaintsBothVisibleRoutePiecesWithoutDuplicatingSourceFacts(bool mercator, bool waypoints, bool reverse, bool clearance) {
        var chart = Model(mercator, waypoints, reverse, clearance, false);
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        MapLabelClusterDepthTests.Capture(prepared, $"near-world-route-{mercator}-{waypoints}-{reverse}-{clearance}");
        var marks = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray();
        Assert.Equal(2, marks.Length);
        var midpoint = (marks.Min(mark => mark.Cx) + marks.Max(mark => mark.Cx)) / 2;
        var endpoints = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-connector")
            .SelectMany(path => path.Commands).ToArray();
        Assert.Contains(endpoints, point => point.X < midpoint);
        Assert.Contains(endpoints, point => point.X > midpoint);
        var route = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"));
        Assert.Equal("true", route.Metadata["data-cfx-visible"]);
        Assert.Equal(reverse ? waypoints ? "-160,0;-175,0;165,0" : "-160,0;165,0" : waypoints ? "165,0;-175,0;-160,0" : "165,0;-160,0",
            route.Metadata["data-cfx-route-coordinates"]);
        Assert.Equal(2, MapLabelClusterDepthTests.Groups(prepared, "dotted-map-point-source").Length);
        Assert.Equal(2, chart.Series[0].MapPoints.Count);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void NarrowSeamControlsRetainOneContinuousNativeRouteAndItsRawBearings(bool mercator, bool waypoints) {
        var chart = Model(mercator, waypoints, false, false, true);
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "dotted-map-connector");
        var route = Assert.Single(MapLabelClusterDepthTests.Groups(prepared, "dotted-map-connector-source"));
        Assert.Equal("true", route.Metadata["data-cfx-visible"]);
        Assert.Equal(waypoints ? "178,0;180,0;-172,0" : "178,0;-172,0", route.Metadata["data-cfx-route-coordinates"]);
    }

    private static Chart Model(bool mercator, bool waypoints, bool reverse, bool clearance, bool narrow) {
        var viewport = ChartMapViewport.AcrossAntimeridian("Periodic route", narrow ? 170 : -170, narrow ? -170 : -180, -30, 10,
            mercator ? ChartMapGeoJsonProjection.WebMercator : ChartMapGeoJsonProjection.Equirectangular);
        var start = narrow ? 178d : 165; var end = narrow ? -172d : -160;
        var points = new[] { new ChartMapPoint("Start", start, 0), new ChartMapPoint("End", end, 0) };
        if (reverse) Array.Reverse(points);
        var chart = Chart.Create().WithSize(800, 360).WithLegend(false).WithMapViewport(viewport).AddDottedMap("Endpoints", points);
        var connector = waypoints ? new ChartMapConnector("Crossing", new[] {
            points[0], new ChartMapPoint("Waypoint", narrow ? 180 : -175, 0), points[1]
        }) : new ChartMapConnector("Crossing", points[0].Longitude, 0, points[1].Longitude, 0);
        if (clearance) connector = connector.WithEndpointClearance(2);
        chart.Series[0].ReplaceMapConnectors(new[] { connector });
        return chart;
    }
}

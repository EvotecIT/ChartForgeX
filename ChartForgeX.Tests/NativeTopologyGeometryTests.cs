using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyGeometryTests {
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void SharedTailIsPaintedOnceAndReachesTheFittedTargetBorder(bool namedPort, int pngScale) {
        var chart = TopologyRoutingFixtures.FanIn(namedPort);
        var options = TopologyRoutingFixtures.Options(); options.ShareIncomingTrunks = true; options.FitContentToViewport = true; options.PngOutputScale = pngScale;
        var prepared = chart.Prepare(options);
        var document = XDocument.Parse(prepared.ToSvg());
        var tail = Assert.Single(document.Descendants(), element => Role(element) == "topology-shared-trunk-tail");
        var owner = (string?)tail.Attribute("data-trunk-owner-id"); Assert.False(string.IsNullOrEmpty(owner));
        var members = document.Descendants().Where(element => Role(element) == "topology-edge" && (string?)element.Attribute("data-trunk-owner-id") == owner).ToArray();
        Assert.Equal(3, members.Length);
        var paintedTail = Assert.Single(tail.Descendants(), element => Role(element) == "topology-edge-line");
        var tailCoordinates = PathCoordinates(paintedTail);
        var endpoint = tailCoordinates.TakeLast(2).ToArray();
        var target = document.Descendants().Single(element => Role(element) == "topology-node" && (string?)element.Attribute("data-node-id") == "target")
            .Descendants().Single(element => Role(element) == "topology-node-surface");
        Assert.Equal(Number(target, "x"), endpoint[0], 3);
        Assert.InRange(endpoint[1], Number(target, "y") - .001, Number(target, "y") + Number(target, "height") + .001);
        var unsharedPaintedLines = document.Descendants().Where(element => Role(element) == "topology-edge-line"
            && !element.Ancestors().Any(ancestor => Role(ancestor) == "topology-shared-trunk-tail"));
        Assert.DoesNotContain(unsharedPaintedLines, line => {
            var routeEnd = PathCoordinates(line).TakeLast(2).ToArray();
            return Math.Abs(routeEnd[0] - Number(target, "x")) < .001
                && routeEnd[1] >= Number(target, "y") - .001
                && routeEnd[1] <= Number(target, "y") + Number(target, "height") + .001;
        });
        Assert.Single(tail.Descendants(), element => Role(element) == "topology-marker");
        Assert.Single(document.Descendants(), element => Role(element) == "topology-marker");
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.All(members, member => {
            var hit = Assert.Single(member.Elements(), element => Role(element) == "topology-shared-trunk-hit");
            Assert.Equal(new ChartColor(0, 0, 0, 0).ToCss(), (string?)hit.Attribute("stroke"));
            var edge = envelope.Edges.Single(item => item.Id == (string?)member.Attribute("data-edge-id"));
            Assert.True(edge.ResolvedRoute.Count >= 2);
            Assert.Equal(Number(member, "data-route-end-x"), edge.ResolvedRoute.Last().X, 5);
            Assert.Equal(Number(member, "data-route-end-y"), edge.ResolvedRoute.Last().Y, 5);
            Assert.False(string.IsNullOrEmpty((string?)member.Attribute("data-route-strategy")));
        });
        Assert.All(chart.Edges, edge => Assert.Null(edge.TargetMarker));
        Assert.NotEmpty(prepared.ToPng());

        static double[] PathCoordinates(XElement element) => Regex.Matches((string)element.Attribute("d")!, @"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    }

    [Fact]
    public void GeographicCalloutRetainsSelectableGroupCountsAndExactNativeAnchor() {
        var chart = TopologyChart.Create().WithId("regions").WithViewport(900, 560).WithLegend(null);
        chart.LayoutMode = TopologyLayoutMode.Geographic;
        chart.Groups.Add(new TopologyGroup { Id = "region", Label = "Region", Longitude = 20, Latitude = 50, Href = "/region", Tooltip = "Regional state" });
        var statuses = new[] { TopologyHealthStatus.Healthy, TopologyHealthStatus.Warning, TopologyHealthStatus.Critical, TopologyHealthStatus.Unknown, TopologyHealthStatus.Disabled };
        for (var index = 0; index < statuses.Length; index++) chart.Nodes.Add(new TopologyNode {
            Id = "node-" + index, Label = "Site " + index, GroupId = "region", Longitude = 18 + index, Latitude = 48 + index,
            Width = 70, Height = 40, Status = statuses[index]
        });
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeGroups = false,
            IncludeGeographicCallouts = true, FitContentToViewport = true };
        var prepared = chart.Prepare(options);
        var document = XDocument.Parse(prepared.ToSvg());
        var callout = Assert.Single(document.Descendants(), element => (string?)element.Attribute("data-cfx-visual-role") == "topology-geographic-callout");
        Assert.Equal("topology-group", Role(callout)); Assert.Equal("region", (string?)callout.Attribute("data-group-id"));
        Assert.Equal("5", (string?)callout.Attribute("data-callout-node-count"));
        foreach (var name in new[] { "healthy", "warning", "critical", "unknown", "disabled" }) Assert.Equal("1", (string?)callout.Attribute("data-callout-" + name + "-count"));
        var anchor = Assert.Single(callout.Descendants(), element => Role(element) == "topology-geographic-callout-anchor");
        // Native metadata retains full precision; SVG coordinates round to 0.001 pixels.
        Assert.InRange(Math.Abs(Number(callout, "data-callout-anchor-x") - Number(anchor, "cx")), 0, .000500001);
        Assert.InRange(Math.Abs(Number(callout, "data-callout-anchor-y") - Number(anchor, "cy")), 0, .000500001);
        Assert.Contains(callout.Ancestors(), element => (string?)element.Attribute("href") == "/region");
        Assert.Contains(prepared.Visual.Regions, region => region.Id == "region-callout" && region.Role == "topology-callout");
        Assert.Contains(prepared.ToInterchangeEnvelope().Groups, group => group.Id == "region");
    }

    [Theory]
    [InlineData(TopologyEdgeRouting.Straight, 0)]
    [InlineData(TopologyEdgeRouting.Straight, -30)]
    [InlineData(TopologyEdgeRouting.Straight, 30)]
    [InlineData(TopologyEdgeRouting.Orthogonal, 0)]
    [InlineData(TopologyEdgeRouting.ObstacleAvoidingOrthogonal, 0)]
    public void AutomaticInterGroupLabelStaysOnItsOwnRouteAcrossNativeExports(TopologyEdgeRouting routing, double targetOffsetY) {
        var chart = InterGroup(routing);
        chart.Nodes[1].Y += targetOffsetY;
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, FitContentToViewport = true };
        var prepared = chart.Prepare(options);
        var diagnostic = Assert.Single(prepared.Analyze().EdgeLabels);
        Assert.InRange(diagnostic.RouteDistance, 0, .01);
        var edge = Assert.Single(prepared.ToInterchangeEnvelope().Edges);
        var bounds = edge.ResolvedLabelBounds!.Value;
        var lineY = edge.ResolvedRoute[0].Y;
        if (routing == TopologyEdgeRouting.Straight) {
            var start = edge.ResolvedRoute[0]; var end = edge.ResolvedRoute[edge.ResolvedRoute.Count - 1];
            var centerX = bounds.Left + bounds.Width / 2;
            lineY += (centerX - start.X) * (end.Y - start.Y) / (end.X - start.X);
        }
        Assert.InRange(lineY, bounds.Top - 2, bounds.Bottom + 2);
        var document = XDocument.Parse(prepared.ToSvg());
        var plate = Assert.Single(document.Descendants(), element => Role(element) == "topology-edge-label-surface");
        Assert.InRange(lineY, Number(plate, "y"), Number(plate, "y") + Number(plate, "height"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void AutomaticMonitoringMeshBadgeKeepsItsOwnRouteAnchorAndClearance() {
        // Exercise the actual example that previously put LON/SFO's badge over the NYC/SFO route.
        var factory = typeof(GalleryWriter).Assembly.GetType("TopologyVisualExamples")!.GetMethod("BuildReplicationMeshExplorer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var chart = Assert.IsType<TopologyChart>(factory.Invoke(null, null));
        var options = new TopologyRenderOptions { IncludeIconLabels = true, LegendMode = TopologyLegendMode.Merge }
            .WithMonitoringDashboardStyle().WithActiveScenario("client-request-europe");
        var prepared = chart.Prepare(options);
        var edge = Assert.Single(prepared.ToInterchangeEnvelope().Edges, item => item.Id == "lon-sfo");
        var document = XDocument.Parse(prepared.ToSvg());
        var label = Assert.Single(document.Descendants(), element => Role(element) == "topology-edge-label" && (string?)element.Attribute("data-edge-id") == edge.Id);
        var anchorX = Number(label, "data-label-anchor-x"); var anchorY = Number(label, "data-label-anchor-y");
        var centerX = Number(label, "data-label-x"); var centerY = Number(label, "data-label-y");
        var anchorDistance = edge.ResolvedRoute.Zip(edge.ResolvedRoute.Skip(1), (a, b) => {
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            var lengthSquared = dx * dx + dy * dy;
            var t = lengthSquared == 0 ? 0 : Math.Max(0, Math.Min(1, ((anchorX - a.X) * dx + (anchorY - a.Y) * dy) / lengthSquared));
            return Math.Sqrt(Math.Pow(anchorX - a.X - t * dx, 2) + Math.Pow(anchorY - a.Y - t * dy, 2));
        }).Min();
        Assert.InRange(anchorDistance, 0, .01);
        var normalOffset = Math.Sqrt(Math.Pow(centerX - anchorX, 2) + Math.Pow(centerY - anchorY, 2));
        var largestExtent = Math.Max(Number(label, "data-label-width"), Number(label, "data-label-height"));
        Assert.InRange(normalOffset, 24 - .01, Math.Max(54, largestExtent / 2 + 10.01) + .01);
        Assert.True(Assert.Single(prepared.Analyze().EdgeLabels, item => item.EdgeId == edge.Id).RouteDistance >= 9.9);
        Assert.Equal("false", (string?)label.Attribute("data-label-leader"));
        Assert.Equal("118 msQ:30110m ago", label.Value);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AutomaticShortPolylineBadgeStaysOnItsOwnRouteAcrossNativeExports(bool example) {
        TopologyChart chart;
        if (example) {
            var factory = typeof(GalleryWriter).Assembly.GetType("TopologyExamples")!.GetMethod("BuildDcConnectivityChart",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            chart = Assert.IsType<TopologyChart>(factory.Invoke(null, null));
        } else {
            chart = TopologyChart.Create().WithViewport(400, 300, 16).WithLegend(null)
                .AddNode("a", "A", 130, 70, width: 80, height: 40)
                .AddNode("b", "B", 136, 191, width: 80, height: 40)
                .AddEdge("stair", "a", "b", "LDAP fail", routing: TopologyEdgeRouting.Orthogonal, secondaryLabel: "DNS OK")
                .WithEdgePorts("stair", TopologyEdgePort.Bottom, TopologyEdgePort.Top);
        }
        var edgeId = example ? "na-dc01-na-dc04" : "stair";
        var prepared = chart.Prepare();
        var diagnostic = Assert.Single(prepared.Analyze().EdgeLabels, item => item.EdgeId == edgeId);
        Assert.InRange(diagnostic.RouteDistance, 0, .01);
        var edge = Assert.Single(prepared.ToInterchangeEnvelope().Edges, item => item.Id == edgeId);
        var document = XDocument.Parse(prepared.ToSvg());
        var label = Assert.Single(document.Descendants(), element => Role(element) == "topology-edge-label" && (string?)element.Attribute("data-edge-id") == edgeId);
        var badgeWidth = Number(label, "data-label-width"); var badgeHeight = Number(label, "data-label-height");
        Assert.All(edge.ResolvedRoute.Zip(edge.ResolvedRoute.Skip(1)), segment => {
            var dx = segment.Second.X - segment.First.X; var dy = segment.Second.Y - segment.First.Y;
            Assert.True(Math.Sqrt(dx * dx + dy * dy) < (Math.Abs(dx) < .01 ? badgeHeight : badgeWidth),
                "This connected route must need the polyline fallback rather than an individual long run.");
        });
        Assert.Equal(Number(label, "data-label-anchor-x"), Number(label, "data-label-x"), 6);
        Assert.Equal(Number(label, "data-label-anchor-y"), Number(label, "data-label-y"), 6);
        Assert.Equal("LDAP failDNS OK", label.Value);
        Assert.Equal("false", (string?)label.Attribute("data-label-leader"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void AuthoredDisplacedLabelKeepsItsRouteAnchorAndNativeLeader() {
        var chart = InterGroup(TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        chart.WithEdgeLabelOffset("request", 0, 125).WithEdgeLabelAnchor("request", 355, 107.5);
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, IncludeEdgeLabelLeaders = true, FitContentToViewport = true };
        var prepared = chart.Prepare(options);
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Single(document.Descendants(), element => Role(element) == "topology-edge-label-leader");
        var edge = Assert.Single(prepared.ToInterchangeEnvelope().Edges);
        Assert.NotNull(edge.ResolvedLabelBounds);
        Assert.True(Assert.Single(prepared.Analyze().EdgeLabels).RouteDistance > 18);
        Assert.Equal(125, chart.Edges[0].LabelOffsetY);
        Assert.True(chart.Edges[0].HasLabelAnchorOverride);
    }

    [Fact]
    public void NativeDiagnosticOverlayAndHostReportDescribeTheResolvedRoute() {
        var chart = TopologyRoutingFixtures.FanIn(false);
        var options = TopologyRoutingFixtures.Options(); options.IncludeLayoutDiagnosticOverlay = true; options.FitContentToViewport = true;
        var prepared = chart.Prepare(options);
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Single(document.Descendants(), element => Role(element) == "topology-layout-diagnostics");
        Assert.Equal(chart.Nodes.Count, document.Descendants().Count(element => Role(element) == "topology-layout-node"));
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(envelope.Edges.Sum(edge => edge.ResolvedRoute.Count), document.Descendants().Count(element => Role(element) == "topology-layout-route-point"));
        foreach (var element in document.Descendants().Where(element => Role(element) == "topology-edge")) {
            Assert.True(Number(element, "data-route-candidate-count") >= 1);
            Assert.True(Number(element, "data-route-segment-count") >= 1);
            Assert.Equal(0, Number(element, "data-route-obstacle-hits"));
        }
    }

    private static TopologyChart InterGroup(TopologyEdgeRouting routing) {
        var chart = TopologyChart.Create().WithId("inter-group").WithViewport(960, 600).WithLegend(null);
        chart.Groups.Add(new TopologyGroup { Id = "primary", Label = "Primary", X = 0, Y = 0, Width = 340, Height = 300 });
        chart.Groups.Add(new TopologyGroup { Id = "secondary", Label = "Secondary", X = 390, Y = 0, Width = 340, Height = 300 });
        chart.Nodes.Add(new TopologyNode { Id = "client", Label = "Client", GroupId = "primary", X = 30, Y = 65, Width = 240, Height = 85 });
        chart.Nodes.Add(new TopologyNode { Id = "worker", Label = "Worker", GroupId = "secondary", X = 430, Y = 65, Width = 240, Height = 85 });
        chart.Edges.Add(new TopologyEdge { Id = "request", SourceNodeId = "client", TargetNodeId = "worker", Label = "Requests", SecondaryLabel = "Active", Routing = routing, Direction = VisualLinkDirection.Forward });
        return chart;
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
}

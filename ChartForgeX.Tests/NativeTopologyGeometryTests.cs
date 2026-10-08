using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyGeometryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedTailIsPaintedOnceWhileEveryRelationshipRetainsItsWholeRoute(bool namedPort) {
        var chart = TopologyRoutingFixtures.FanIn(namedPort);
        var options = TopologyRoutingFixtures.Options(); options.ShareIncomingTrunks = true; options.FitContentToViewport = true;
        var prepared = chart.Prepare(options);
        var document = XDocument.Parse(prepared.ToSvg());
        var tail = Assert.Single(document.Descendants(), element => Role(element) == "topology-shared-trunk-tail");
        var owner = (string?)tail.Attribute("data-trunk-owner-id"); Assert.False(string.IsNullOrEmpty(owner));
        var members = document.Descendants().Where(element => Role(element) == "topology-edge" && (string?)element.Attribute("data-trunk-owner-id") == owner).ToArray();
        Assert.Equal(3, members.Length);
        Assert.Single(tail.Descendants(), element => Role(element) == "topology-edge-line");
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
        Assert.Equal(Number(callout, "data-callout-anchor-x"), Number(anchor, "cx"), 2);
        Assert.Equal(Number(callout, "data-callout-anchor-y"), Number(anchor, "cy"), 2);
        Assert.Contains(callout.Ancestors(), element => (string?)element.Attribute("href") == "/region");
        Assert.Contains(prepared.Visual.Regions, region => region.Id == "region-callout" && region.Role == "topology-callout");
        Assert.Contains(prepared.ToInterchangeEnvelope().Groups, group => group.Id == "region");
    }

    [Theory]
    [InlineData(TopologyEdgeRouting.Straight)]
    [InlineData(TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyEdgeRouting.ObstacleAvoidingOrthogonal)]
    public void AutomaticInterGroupLabelStaysOnItsOwnRouteAcrossNativeExports(TopologyEdgeRouting routing) {
        var chart = InterGroup(routing);
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, FitContentToViewport = true };
        var prepared = chart.Prepare(options);
        var diagnostic = Assert.Single(prepared.Analyze().EdgeLabels);
        Assert.InRange(diagnostic.RouteDistance, 0, .01);
        var edge = Assert.Single(prepared.ToInterchangeEnvelope().Edges);
        var bounds = edge.ResolvedLabelBounds!.Value;
        var lineY = edge.ResolvedRoute[0].Y;
        Assert.InRange(lineY, bounds.Top - 2, bounds.Bottom + 2);
        var document = XDocument.Parse(prepared.ToSvg());
        var plate = Assert.Single(document.Descendants(), element => Role(element) == "topology-edge-label-surface");
        Assert.InRange(lineY, Number(plate, "y"), Number(plate, "y") + Number(plate, "height"));
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

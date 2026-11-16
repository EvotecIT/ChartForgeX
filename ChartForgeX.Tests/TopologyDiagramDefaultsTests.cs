using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the default look of diagram edges, routes and cards.</summary>
public sealed class TopologyDiagramDefaultsTests {
    [Theory]
    [InlineData(TopologyHealthStatus.Unknown, "none")]
    [InlineData(TopologyHealthStatus.Healthy, "none")]
    [InlineData(TopologyHealthStatus.Warning, "8 5")]
    [InlineData(TopologyHealthStatus.Critical, "8 5")]
    [InlineData(TopologyHealthStatus.Disabled, "3 6")]
    public void EdgesAreSolidUnlessTheirStatusReportsAProblem(TopologyHealthStatus status, string expectedDash) {
        var chart = TopologyChart.Create().WithViewport(420, 160)
            .AddNode("a", "Alpha", 20, 40).AddNode("b", "Beta", 280, 40)
            .AddEdge("link", "a", "b", status: status);
        var line = XDocument.Parse(chart.ToSvg()).Descendants()
            .First(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        Assert.Equal(expectedDash, (string?)line.Attribute("stroke-dasharray") ?? "none");
        Assert.Equal(expectedDash != "none", TopologyRenderPrimitives.EffectiveEdgePngDashArray(chart.Edges[0]) != null);
    }

    [Fact]
    public void DiagonalObstacleRouteTurnsOnceWithoutSharingAnotherEdgesSide() {
        var chart = TopologyChart.Create().WithViewport(640, 420)
            .AddNode("client", "Client", 40, 40, width: 200, height: 80)
            .AddNode("cache", "Cache", 400, 40, width: 150, height: 80)
            .AddNode("worker", "Worker", 360, 280, width: 200, height: 80)
            .AddEdge("lookup", "client", "cache", routing: TopologyEdgeRouting.Curved)
            .AddEdge("request", "client", "worker", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var report = chart.Prepare().Analyze();
        var request = report.Edges.Single(edge => edge.Id == "request");
        var client = report.Nodes.Single(node => node.Id == "client").Bounds;
        var worker = report.Nodes.Single(node => node.Id == "worker").Bounds;
        Assert.True(request.SourceAttached && request.TargetAttached);
        Assert.Equal(0, request.ObstacleHits);
        Assert.Equal(3, request.Points.Count);
        // Leaves the client's bottom (the lookup edge owns its right side) and enters the worker's left side.
        Assert.Equal(client.Bottom, request.Points[0].Y, 1);
        Assert.Equal(worker.Left, request.Points[^1].X, 1);
    }

    [Theory]
    [InlineData(null, .45, .7)]
    [InlineData("v2", 0, .45)]
    public void TitleOnlyCardCentresItsHeadingUnlessABadgeHoldsTheLowerRow(string? badge, double minimum, double maximum) {
        var chart = TopologyChart.Create().WithViewport(320, 160)
            .AddNode("worker", "Processing service", 40, 30, TopologyNodeKind.Server, width: 240, height: 85);
        chart.Nodes[0].Badge = badge;
        var document = XDocument.Parse(chart.ToSvg());
        var node = document.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "topology-node"
            && (string?)element.Attribute("data-node-id") == "worker");
        var surface = node.Descendants().First(element => element.Name.LocalName == "rect");
        var top = (double)surface.Attribute("y")!; var height = (double)surface.Attribute("height")!;
        var label = node.Descendants().First(element => element.Name.LocalName == "text"
            && element.Ancestors().Any(parent => (string?)parent.Attribute("data-cfx-role") == "topology-node-label"));
        var baseline = (double)label.Attribute("y")!;
        // Centred, the baseline sits just below the card's middle; beside a badge it stays in the top row.
        Assert.InRange(baseline - top, height * minimum, height * maximum);
    }
}

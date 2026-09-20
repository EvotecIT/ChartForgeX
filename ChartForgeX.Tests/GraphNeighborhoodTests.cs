using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphNeighborhoodTests {
    [Fact]
    public void HubPagesRetainRootAndAccountForEveryRelationship() {
        var scene = Hub(1000);
        var first = scene.CreateNeighborhood("root");
        var second = scene.CreateNeighborhood("root", options => options.NeighborOffset = 39);
        Assert.Equal(40, first.VisibleNodeIds.Count);
        Assert.Equal(39, first.VisibleEdgeIds.Count);
        Assert.Equal(961, first.HiddenNodeCount);
        Assert.Equal(961, first.HiddenEdgeCount);
        Assert.Equal(961, first.BoundaryEdgeCount);
        Assert.Equal(1001, first.ScopeNodeCount);
        Assert.Equal(new[] { "root" }, first.VisibleNodeIds.Intersect(second.VisibleNodeIds));
        Assert.False(first.IsFullScene);
        var visited = new HashSet<string>();
        for (var offset = 0; offset < first.ScopeNodeCount - 1; offset += 39)
            visited.UnionWith(scene.CreateNeighborhood("root", options => options.NeighborOffset = offset).VisibleNodeIds);
        Assert.Equal(1001, visited.Count);
        Assert.Equal(1001, scene.Nodes.Count);
        Assert.Equal(1000, scene.Edges.Count);
    }

    [Fact]
    public void EdgeBudgetIsAppliedByBothPlannerAndStaticExporter() {
        var scene = Hub(6);
        scene.AddEdge("parallel", "root", "n0000", configure: edge => edge.Directed = true);
        scene.AddEdge("self", "root", "root");
        var stage = scene.CreateNeighborhood("root", options => options.MaximumEdges = 2);
        Assert.Equal(2, stage.VisibleEdgeIds.Count);
        Assert.Equal(6, stage.HiddenEdgeCount);
        Assert.Equal(0, stage.BoundaryEdgeCount);
        Assert.False(stage.IsFullScene);
        var svg = XDocument.Parse(scene.ToGraphSvg(stage));
        var edges = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "graph-edge")
            .Select(element => (string?)element.Attribute("data-edge-id")).OrderBy(id => id).ToArray();
        Assert.Equal(stage.VisibleEdgeIds.OrderBy(id => id), edges);
        Assert.Contains("6 relationships omitted", svg.Root!.Value);
        Assert.Equal(new byte[] { 137, 80, 78, 71 }, scene.ToGraphPng(stage).Take(4));
        scene.Edges.RemoveAll(edge => edge.Id == stage.VisibleEdgeIds[0]);
        Assert.Throws<InvalidOperationException>(() => scene.ToGraphSvg(stage));
    }

    [Fact]
    public void TraversalAndPagesAreIndependentOfInputOrder() {
        var scene = Hub(6);
        scene.AddNode("next", "Next").AddNode("disconnected", "Disconnected");
        scene.AddEdge("inbound", "next", "n0000", configure: edge => edge.Directed = true);
        var stage = scene.CreateNeighborhood("root", options => { options.Hops = 2; options.MaximumNodes = 5; });
        scene.Nodes.Reverse();
        scene.Edges.Reverse();
        var reordered = scene.CreateNeighborhood("root", options => { options.Hops = 2; options.MaximumNodes = 5; });
        Assert.Equal(stage.VisibleNodeIds, reordered.VisibleNodeIds);
        Assert.Equal(stage.VisibleEdgeIds, reordered.VisibleEdgeIds);
        Assert.Equal(8, stage.ScopeNodeCount);
        Assert.DoesNotContain("next", stage.VisibleNodeIds);
        var last = scene.CreateNeighborhood("root", options => { options.Hops = 2; options.NeighborOffset = 6; });
        Assert.Equal(new[] { "next", "root" }, last.VisibleNodeIds);
        Assert.Empty(last.VisibleEdgeIds);
        Assert.Equal(7, last.BoundaryEdgeCount);
        var rootOnly = scene.CreateNeighborhood("root", options => options.Hops = 0);
        Assert.Single(rootOnly.VisibleNodeIds);
        Assert.True(rootOnly.IsFullScene);
        Assert.Throws<ArgumentException>(() => scene.CreateNeighborhood("missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => scene.CreateNeighborhood("root", options => options.MaximumNodes = 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => scene.CreateNeighborhood("root", options => options.MaximumEdges = -1));
    }

    private static GraphScene Hub(int neighbors) {
        var scene = GraphScene.Create("hub", "Service neighborhood").AddNode("root", "Gateway");
        for (var i = 0; i < neighbors; i++) {
            var id = "n" + i.ToString("D4");
            scene.AddNode(id, "Service " + i).AddEdge("e" + id, "root", id);
        }
        return scene;
    }
}

using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphNeighborhoodTests {
    [Fact]
    public void HierarchyFooterCountsRenderedObjectsInsteadOfHiddenMembers() {
        var scene = GraphScene.Create("hidden-hierarchy", "Hidden hierarchy")
            .AddNode("root", "Root").AddNode("visible", "Visible", node => node.ParentId = "root")
            .AddNode("hidden", "Hidden", node => { node.ParentId = "root"; node.Hidden = true; })
            .AddEdge("visible-edge", "root", "visible")
            .AddEdge("hidden-edge", "root", "hidden", configure: edge => edge.Style.Hidden = true);
        var stage = scene.CreateStages(options => options.Depths.Add(1)).Last();
        var svg = XDocument.Parse(scene.ToGraphSvg(stage));
        Assert.Contains(svg.Descendants().Where(element => element.Name.LocalName == "text"),
            element => element.Value.Contains("2 nodes · 1 relationships shown; 1 nodes · 1 relationships omitted"));
    }

    [Fact]
    public void InteractiveNeighborhoodOptionsAreValidatedAndRendered() {
        var scene = Hub(30);
        scene.Options.Neighborhood.MaximumNodes = 7;
        scene.Options.Neighborhood.MaximumEdges = 9;
        scene.Options.Neighborhood.Hops = 2;
        var html = scene.ToGraphExplorerHtmlPage();
        Assert.Contains("data-cfx-neighborhood-max-nodes=\"7\"", html);
        Assert.Contains("data-cfx-neighborhood-max-edges=\"9\"", html);
        Assert.Contains("data-cfx-neighborhood-hops=\"2\"", html);
        Assert.Contains("aria-label=\"Neighborhood navigation\"", html);
        scene.Options.Neighborhood.MaximumNodes = 1;
        Assert.Throws<ArgumentOutOfRangeException>(() => scene.ToGraphExplorerHtmlPage());
    }

    [Fact]
    public void HiddenObjectsDoNotConsumeVisibleBudgets() {
        var scene = Hub(5);
        scene.Nodes.Single(node => node.Id == "n0000").Hidden = true;
        scene.Edges.Single(edge => edge.TargetNodeId == "n0001").Style.Hidden = true;
        var stage = scene.CreateNeighborhood("root", options => { options.MaximumNodes = 3; options.MaximumEdges = 1; });
        Assert.Equal(new[] { "n0001", "n0002", "root" }, stage.VisibleNodeIds);
        Assert.Equal(new[] { "en0002" }, stage.VisibleEdgeIds);
        Assert.Equal(5, stage.ScopeNodeCount);
        var next = scene.CreateNeighborhood("root", options => { options.MaximumNodes = 3; options.NeighborOffset = 2; });
        Assert.Equal(new[] { "n0003", "n0004", "root" }, next.VisibleNodeIds);
        scene.Nodes.Single(node => node.Id == "root").Hidden = true;
        Assert.Throws<ArgumentException>(() => scene.CreateNeighborhood("root"));
    }

    [Fact]
    public void DiscoveryCanTraverseHiddenIntermediateObjects() {
        var scene = GraphScene.Create("hidden-bridge", "Hidden bridge traversal").AddNode("root", "Root")
            .AddNode("bridge", "Hidden bridge", node => node.Hidden = true).AddNode("leaf", "Visible leaf")
            .AddEdge("first", "root", "bridge", configure: edge => edge.Style.Hidden = true)
            .AddEdge("second", "bridge", "leaf");
        var stage = scene.CreateNeighborhood("root", options => { options.Hops = 2; options.MaximumNodes = 2; });
        Assert.Equal(new[] { "leaf", "root" }, stage.VisibleNodeIds);
        Assert.Empty(stage.VisibleEdgeIds);
        Assert.Equal(2, stage.ScopeNodeCount);
        Assert.Equal(1, stage.HiddenNodeCount);
        Assert.Equal(2, stage.HiddenEdgeCount);
    }

    [Theory]
    [InlineData(300)]
    [InlineData(1000)]
    public void LargeExplicitNeighborhoodBudgetFitsAllNodeCenters(int neighbors) {
        var scene = Hub(neighbors);
        var stage = scene.CreateNeighborhood("root", options => { options.MaximumNodes = neighbors + 1; options.MaximumEdges = 0; });
        var document = XDocument.Parse(scene.ToGraphSvg(stage));
        var viewport = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "graph-viewport");
        var transform = System.Text.RegularExpressions.Regex.Match((string)viewport.Attribute("transform")!, @"translate\(([^ ]+) ([^)]+)\) scale\(([^)]+)\)");
        double Parse(string value) => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        double x = Parse(transform.Groups[1].Value), y = Parse(transform.Groups[2].Value), scale = Parse(transform.Groups[3].Value);
        Assert.InRange(scale, double.Epsilon, 0.05);
        var nodes = viewport.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "graph-node").ToArray();
        Assert.Equal(neighbors + 1, nodes.Length);
        foreach (var node in nodes) {
            Assert.InRange(x + Parse((string)node.Attribute("data-node-x")!) * scale, 0, 960);
            Assert.InRange(y + Parse((string)node.Attribute("data-node-y")!) * scale, 0, 560);
        }
    }

    [Fact]
    public void LargeStarSpokesDoNotPassThroughOtherNodes() {
        var scene = Hub(1000);
        var stage = scene.CreateNeighborhood("root", options => { options.MaximumNodes = 40; options.MaximumEdges = 80; });
        var document = XDocument.Parse(scene.ToGraphSvg(stage));
        var nodes = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "graph-node")
            .ToDictionary(element => (string)element.Attribute("data-node-id")!, element => (
                X: double.Parse((string)element.Attribute("data-node-x")!, System.Globalization.CultureInfo.InvariantCulture),
                Y: double.Parse((string)element.Attribute("data-node-y")!, System.Globalization.CultureInfo.InvariantCulture)));
        var root = nodes["root"];
        foreach (var target in nodes.Where(pair => pair.Key != "root")) {
            var dx = target.Value.X - root.X; var dy = target.Value.Y - root.Y;
            foreach (var other in nodes.Where(pair => pair.Key != "root" && pair.Key != target.Key)) {
                var t = Math.Clamp(((other.Value.X - root.X) * dx + (other.Value.Y - root.Y) * dy) / (dx * dx + dy * dy), 0, 1);
                var distance = Math.Sqrt(Math.Pow(other.Value.X - root.X - t * dx, 2) + Math.Pow(other.Value.Y - root.Y - t * dy, 2));
                Assert.True(distance > 16, "A spoke must not imply a connection through another service node.");
            }
        }
        Assert.All(scene.Nodes, node => Assert.False(node.HasExplicitPosition));
    }

    [Fact]
    public void HubPagesRetainRootAndAccountForEveryRelationship() {
        var scene = Hub(1000);
        Assert.Equal(13, scene.CreateNeighborhood("root").VisibleNodeIds.Count);
        var first = scene.CreateNeighborhood("root", options => { options.MaximumNodes = 40; options.MaximumEdges = 80; });
        var second = scene.CreateNeighborhood("root", options => { options.NeighborOffset = 39; options.MaximumNodes = 40; options.MaximumEdges = 80; });
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
            visited.UnionWith(scene.CreateNeighborhood("root", options => { options.NeighborOffset = offset; options.MaximumNodes = 40; }).VisibleNodeIds);
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

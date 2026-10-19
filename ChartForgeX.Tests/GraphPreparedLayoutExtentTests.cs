using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects fresh node extents and the existing label policy across repeated layout requests.</summary>
public sealed class GraphPreparedLayoutExtentTests {
    [Theory]
    [InlineData(GraphLayoutMode.StructuredPrepared, GraphLayoutDirection.TopToBottom, 16, 0, true)]
    [InlineData(GraphLayoutMode.Hierarchical, GraphLayoutDirection.TopToBottom, 16, 0, true)]
    [InlineData(GraphLayoutMode.Hierarchical, GraphLayoutDirection.LeftToRight, 16, 0, true)]
    [InlineData(GraphLayoutMode.StructuredPrepared, GraphLayoutDirection.TopToBottom, 500, 1, true)]
    [InlineData(GraphLayoutMode.StructuredPrepared, GraphLayoutDirection.TopToBottom, 500, 0, false)]
    [InlineData(GraphLayoutMode.Hierarchical, GraphLayoutDirection.TopToBottom, 500, 1, false)]
    public void ReusedScenesRemeasureBadgesWithoutChangingTheLayoutLabelCutoff(GraphLayoutMode mode,
        GraphLayoutDirection direction, int count, int explicitNodes, bool changesPositions) {
        var graph = Scene(mode, direction, count, explicitNodes);
        var before = Positions(graph);
        graph.Nodes[3].BadgeText = "+123456789012345678901234567890";
        var updated = Positions(graph);
        var fresh = Scene(mode, direction, count, explicitNodes);
        fresh.Nodes[3].BadgeText = graph.Nodes[3].BadgeText;
        Assert.Equal(Positions(fresh), updated);
        Assert.Equal(changesPositions, !before.SequenceEqual(updated));
        Assert.Equal(explicitNodes, graph.Nodes.Count(node => node.HasExplicitPosition));
    }

    private static GraphScene Scene(GraphLayoutMode mode, GraphLayoutDirection direction, int count, int explicitNodes) {
        var graph = GraphScene.Create("extent-updates", "Badge counts");
        graph.Options.Layout.Mode = mode;
        graph.Options.Layout.Direction = direction;
        graph.Options.Features = GraphSceneFeatures.Selection;
        for (var index = 0; index < count; index++) {
            var node = new GraphSceneNode { Id = "n" + index, Label = "N", Size = 8, BadgeText = "+1" };
            if (index > 0) node.ParentId = "n0";
            if (index < explicitNodes) { node.X = 480; node.Y = 280; }
            graph.Nodes.Add(node);
            if (index > 0) graph.AddEdge("e" + index, "n0", node.Id, configure: edge => edge.Directed = true);
        }
        return graph;
    }

    private static (string Id, string X, string Y)[] Positions(GraphScene graph) {
        var html = graph.ToGraphExplorerHtmlFragment(options => options.RenderBackend = HtmlGraphRenderBackend.Svg);
        return Regex.Matches(html, "<g\\b[^>]*data-cfx-role=\"graph-node\"[^>]*>")
            .Select(match => XElement.Parse(match.Value + "</g>"))
            .Select(node => ((string)node.Attribute("data-node-id")!, (string)node.Attribute("data-node-x")!, (string)node.Attribute("data-node-y")!))
            .ToArray();
    }
}

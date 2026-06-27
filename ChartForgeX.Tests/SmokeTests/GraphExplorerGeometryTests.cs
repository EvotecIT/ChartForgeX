using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphExplorerEdgeGeometryKeepsBidirectionalArrowLabelsAligned() {
        var bidirectionalHtml = GraphScene.Create("bidirectional-edge", "Bidirectional edge")
            .AddNode("source", "Source", node => {
                node.X = 100;
                node.Y = 280;
                node.Size = 30;
                node.Shape = GraphNodeShape.Box;
            })
            .AddNode("target", "Target", node => {
                node.X = 480;
                node.Y = 280;
                node.Size = 30;
                node.Shape = GraphNodeShape.Box;
            })
            .AddEdge("both", "source", "target", "Both", edge => {
                edge.Directed = true;
                edge.SourceArrow = true;
            })
            .ToGraphExplorerHtmlFragment();

        Assert(bidirectionalHtml.Contains("data-edge-id=\"both\" data-edge-label=\"Both\"", StringComparison.Ordinal) && bidirectionalHtml.Contains("data-edge-source-arrow=\"true\" data-edge-target-arrow=\"true\"", StringComparison.Ordinal) && bidirectionalHtml.Contains("marker-start=\"url(#bidirectional-edge-", StringComparison.Ordinal) && bidirectionalHtml.Contains("marker-end=\"url(#bidirectional-edge-", StringComparison.Ordinal), "Graph explorer SVG should keep the compatibility target arrow when direct callers add a source arrow to a directed edge.");
        Assert(Math.Abs(ExtractGraphEdgeLabelPoint(bidirectionalHtml, "both").X - 290) < 0.001, "Graph explorer SVG should place labels from shape-trimmed source and target endpoints when arrows are rendered at both ends.");
    }
}

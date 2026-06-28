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
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("if (edge.targetArrow || edge.directed) drawArrow(context, rendered, control, 'target', edge.strokeColor);", StringComparison.Ordinal), "Graph explorer Canvas and PNG output should keep the compatibility target arrow even when a directed edge also renders a source arrow.");

        var computedRouteHtml = GraphScene.Create("computed-route", "Computed route")
            .AddNode("source", "Source")
            .AddNode("target", "Target")
            .AddEdge("route", "source", "target", "Route", edge => {
                edge.Shape = GraphEdgeShape.Polyline;
                edge.Directed = true;
                edge.RoutePoints.AddRange(new[] {
                    new GraphScenePoint(100, 260),
                    new GraphScenePoint(100, 120),
                    new GraphScenePoint(520, 120),
                    new GraphScenePoint(520, 260)
                });
            })
            .ToGraphExplorerHtmlFragment();
        var computedTarget = ExtractGraphNodePoint(computedRouteHtml, "target");
        var computedRouteTarget = ExtractLastPathPoint(ExtractGraphEdgePath(computedRouteHtml, "route"));
        Assert(Distance(computedRouteTarget, computedTarget) < 40 && Distance(computedRouteTarget, (0d, 0d)) > 80, "Graph explorer SVG should trim routed arrows from computed node positions instead of default model coordinates.");
    }

    private static (double X, double Y) ExtractLastPathPoint(string path) {
        var parts = path.Split(new[] { ' ', 'M', 'L', 'Q', 'C' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) throw new InvalidOperationException("Malformed graph path: " + path);
        return (double.Parse(parts[parts.Length - 2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(parts[parts.Length - 1], System.Globalization.CultureInfo.InvariantCulture));
    }
}

using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AssertRoutedLabelCollisionStaysOnPolyline() {
        var html = GraphScene.Create("routed-label-collision", "Routed label collision")
            .AddNode("source", "Source", node => { node.X = 100; node.Y = 260; node.Size = 120; node.Shape = GraphNodeShape.Box; })
            .AddNode("target", "Target", node => { node.X = 520; node.Y = 260; })
            .AddEdge("route", "source", "target", "Route", edge => {
                edge.Shape = GraphEdgeShape.Polyline;
                edge.RoutePoints.AddRange(new[] { new GraphScenePoint(100, 260), new GraphScenePoint(100, 80), new GraphScenePoint(100, 260), new GraphScenePoint(520, 80), new GraphScenePoint(520, 260) });
            })
            .ToGraphExplorerHtmlFragment();

        Assert(ExtractGraphEdgeLabelPoint(html, "route").Y < 210, "A routed SVG label colliding with a large node should move along its rendered route rather than jump to the unrelated straight midpoint.");
    }
}

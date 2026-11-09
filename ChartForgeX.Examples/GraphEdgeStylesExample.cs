using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphEdgeStylesExample {
    public static void Write(string output) {
        var graph = Build();
        foreach (var backend in new[] { HtmlGraphRenderBackend.Svg, HtmlGraphRenderBackend.Canvas, HtmlGraphRenderBackend.WebGl }) {
            File.WriteAllText(Path.Combine(output, "graph-edge-styles-" + backend.ToString().ToLowerInvariant() + ".html"), graph.ToGraphExplorerHtmlPage(options => {
                options.PageTitle = "Edge styles and tooltips — " + backend;
                options.RenderBackend = backend;
            }));
        }
        var path = Path.Combine(output, "graph-edge-styles");
        File.WriteAllText(path + ".svg", graph.ToGraphSvg());
        File.WriteAllBytes(path + ".png", graph.ToGraphPng());
        File.WriteAllText(path + ".static-only", "Static export of the edge-style explorer: every arrow shape, arrow position, dash pattern and value-mapped width.\n");
    }

    internal static GraphScene Build() {
        var graph = GraphScene.Create("edge-styles", "Edge styles and tooltips");
        graph.Subtitle = "Rest the pointer on a node or relationship to read its tooltip; Tab to the graph and use the arrow keys to read the same text. The thick route widens on hover and when selected.";
        graph.Options.Cluster.CollapseOnLoad = false;
        graph.Options.LevelOfDetail.CollapseClustersOnLoad = false;
        graph.Options.EdgeWidthScale.MinimumWidth = 1;
        graph.Options.EdgeWidthScale.MaximumWidth = 7;

        var shapes = Enum.GetValues<GraphEdgeArrowShape>();
        for (var index = 0; index < shapes.Length; index++) {
            var shape = shapes[index];
            Pair(graph, "shape-" + index, 40, 60 + index * 82, 260, shape + " target", edge => {
                edge.TargetArrow = true;
                edge.Style.TargetArrowShape = shape;
                edge.Tooltip = shape + " arrow at the target.\nThe same polygon is drawn by SVG, Canvas, WebGL and the static export.";
            });
        }

        Pair(graph, "positions", 420, 60, 660, "Start, middle and end", edge => {
            edge.SourceArrow = true; edge.MiddleArrow = true; edge.TargetArrow = true;
            edge.Style.SourceArrowShape = GraphEdgeArrowShape.Circle;
            edge.Style.MiddleArrowShape = GraphEdgeArrowShape.Vee;
            edge.Tooltip = "Circle at the start, vee halfway, arrow at the end.";
        });
        Pair(graph, "relation", 420, 142, 660, "Bar to diamond, scale 1.5", edge => {
            edge.SourceArrow = true; edge.TargetArrow = true; edge.Style.ArrowScale = 1.5;
            edge.Style.SourceArrowShape = GraphEdgeArrowShape.Bar;
            edge.Style.TargetArrowShape = GraphEdgeArrowShape.Diamond;
            edge.Style.Color = "#7c3aed";
            edge.Tooltip = "Arrow scale 1.5 enlarges every mark on the relationship.";
        });
        Pair(graph, "curve", 420, 224, 660, "Curve with middle arrow", edge => {
            edge.Shape = GraphEdgeShape.Curve; edge.Curvature = -38; edge.MiddleArrow = true; edge.Directed = true;
            edge.Tooltip = "The middle arrow follows the curve's tangent.";
        });
        Pair(graph, "dash-short", 420, 306, 660, "Dashes 2 4", edge => {
            edge.Dashed = true; edge.Style.DashPattern = "2 4"; edge.Style.Width = 2; edge.Directed = true;
            edge.Tooltip = "Dash array 2 4.";
        });
        Pair(graph, "dash-mixed", 420, 388, 660, "Dashes 10 4 2 4", edge => {
            edge.Dashed = true; edge.Style.DashPattern = "10 4 2 4"; edge.Style.Width = 2; edge.Directed = true;
            edge.Tooltip = "Dash array 10 4 2 4.";
        });
        graph.AddNode("route-a", "R1", node => Place(node, 420, 470, "Route start"));
        graph.AddNode("route-b", "R2", node => Place(node, 660, 520, "Route end"));
        graph.AddEdge("route", "route-a", "route-b", "Route with middle arrow", edge => {
            edge.Shape = GraphEdgeShape.Polyline; edge.MiddleArrow = true; edge.Directed = true;
            // Half the route length falls on the long first run, so the caption sits above a straight segment.
            edge.RoutePoints.Add(new GraphScenePoint(420, 470));
            edge.RoutePoints.Add(new GraphScenePoint(600, 470));
            edge.RoutePoints.Add(new GraphScenePoint(600, 520));
            edge.RoutePoints.Add(new GraphScenePoint(660, 520));
            edge.Tooltip = "A prepared route places its middle arrow at half the route length.";
        });

        var values = new[] { 1d, 4, 10 };
        for (var index = 0; index < values.Length; index++) {
            var value = values[index];
            Pair(graph, "value-" + index, 800, 60 + index * 82, 1020, "Value " + value, edge => {
                edge.Style.Value = value; edge.Style.Color = "#0f766e"; edge.Directed = true;
                edge.Tooltip = "Value " + value + " maps onto widths 1 to 7 across this scene.\nThe arrowhead grows gently with the width.";
            });
        }
        Pair(graph, "emphasis", 800, 306, 1020, "Hover 6, selected 9", edge => {
            edge.Style.Width = 3; edge.Style.HoverWidth = 6; edge.Style.SelectionWidth = 9;
            edge.Tooltip = "Width 3 at rest, 6 under the pointer and 9 when selected.";
        });
        graph.AddNode("loop", "Loop", node => Place(node, 880, 470, "A self reference."));
        graph.AddEdge("loop-edge", "loop", "loop", "Self loop", edge => {
            edge.Shape = GraphEdgeShape.SelfReference; edge.Directed = true;
            edge.Tooltip = "A directed loop ends in one arrowhead.";
        });
        graph.AddNode("escape", "Plain text", node => Place(node, 1020, 520, "Tooltips are text: <b>markup</b> & entities stay literal."));
        return graph;
    }

    private static void Pair(GraphScene graph, string id, double x1, double y, double x2, string label, Action<GraphSceneEdge> configure) {
        graph.AddNode(id + "-a", label, node => Place(node, x1, y, label + "\nSource node"));
        graph.AddNode(id + "-b", "To", node => Place(node, x2, y, label + "\nTarget node"));
        graph.AddEdge(id, id + "-a", id + "-b", configure: configure);
    }

    private static void Place(GraphSceneNode node, double x, double y, string tooltip) {
        node.X = x; node.Y = y; node.Fixed = true; node.Size = 9; node.Tooltip = tooltip;
    }
}

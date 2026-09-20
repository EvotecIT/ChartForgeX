using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphNeighborhoodExample {
    public static void Write(string output) {
        var graph = GraphScene.Create("service-neighborhood", "Service neighborhood")
            .AddNode("gateway", "Gateway", node => { node.Size = 18; node.Kind = "gateway"; });
        for (var index = 0; index < 1000; index++) {
            var id = "service-" + index.ToString("D4");
            graph.AddNode(id, "Service " + (index + 1), node => { node.ParentId = "gateway"; node.Kind = "service"; })
                .AddEdge("route-" + id, "gateway", id, configure: edge => { edge.Directed = true; edge.ShowLabel = false; });
        }
        for (var page = 0; page < 2; page++) {
            var stage = graph.CreateNeighborhood("gateway", options => {
                options.MaximumNodes = 13;
                options.MaximumEdges = 12;
                options.NeighborOffset = page * 12;
            });
            var path = Path.Combine(output, "graph-neighborhood-page-" + (page + 1));
            File.WriteAllText(path + ".svg", graph.ToGraphSvg(stage));
            File.WriteAllBytes(path + ".png", graph.ToGraphPng(stage));
            File.WriteAllText(path + ".static-only", "Bounded neighborhood from 1,001 nodes; each page keeps the gateway.\n");
        }
    }
}

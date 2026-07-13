using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphExplorerScaleExamples {
    private static readonly int[] ScaleCounts = { 1000, 5000, 10000 };

    public static void Write(string output) {
        foreach (var nodeCount in ScaleCounts) {
            var graph = Build(nodeCount);
            graph.ToGraphExplorerHtmlPage(options => {
                options.PageTitle = "Graph Explorer " + nodeCount.ToString("N0") + " Node Scale Baseline";
                options.RenderBackend = HtmlGraphRenderBackend.WebGl;
            }).SaveText(Path.Combine(output, FileName(nodeCount)));
        }
    }

    private static GraphScene Build(int nodeCount) {
        var graph = GraphScene.Create("graph-scale-" + nodeCount, nodeCount.ToString("N0") + " Node WebGL Scale Baseline");
        graph.Subtitle = nodeCount.ToString("N0") + " fixed-position nodes and " + (nodeCount * 2).ToString("N0") + " edges for repeatable browser startup, interaction, and frame-budget review.";
        graph.Options.Features = GraphSceneFeatures.Explorer | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry;
        graph.Options.LevelOfDetail.ClusterNodeThreshold = 500;
        graph.Options.LevelOfDetail.HideEdgeLabelsThreshold = 100;
        graph.Options.LevelOfDetail.CompactNodeThreshold = 250;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 300;
        graph.Options.LevelOfDetail.WebGlPreferredNodeThreshold = 800;
        graph.Options.LevelOfDetail.CollapseClustersOnLoad = false;
        graph.Options.Cluster.Mode = GraphClusterMode.Explicit;
        graph.Options.Cluster.CollapseOnLoad = false;
        graph.Options.Performance.FrameBudgetMilliseconds = 16;
        graph.Options.Performance.MaxInteractiveSvgNodes = 500;
        graph.Options.Performance.MaxInteractiveSvgEdges = 1000;
        graph.Options.Performance.MaxInteractiveCanvasNodes = 4000;
        graph.Options.Performance.MaxInteractiveCanvasEdges = 10000;
        graph.Options.Performance.MaxInteractiveWebGlNodes = 20000;
        graph.Options.Performance.MaxInteractiveWebGlEdges = 50000;
        graph.Options.Performance.TelemetrySampleInterval = 10;

        var columns = (int)Math.Ceiling(Math.Sqrt(nodeCount));
        const int clusterSize = 100;
        for (var clusterStart = 0; clusterStart < nodeCount; clusterStart += clusterSize) {
            var end = Math.Min(nodeCount, clusterStart + clusterSize);
            var clusterId = "zone-" + (clusterStart / clusterSize).ToString("D3");
            var nodeIds = Enumerable.Range(clusterStart, end - clusterStart).Select(NodeId).ToArray();
            graph.AddCluster(clusterId, "Zone " + (clusterStart / clusterSize + 1).ToString("D3"), nodeIds, cluster => cluster.Kind = "scale-zone");
            for (var index = clusterStart; index < end; index++) {
                graph.AddNode(NodeId(index), "Service " + index.ToString("D5"), node => {
                    node.ClusterId = clusterId;
                    node.GroupId = clusterId;
                    node.Kind = index % 7 == 0 ? "database" : index % 5 == 0 ? "identity" : "service";
                    node.Status = index % 97 == 0 ? "critical" : index % 19 == 0 ? "warning" : "healthy";
                    node.X = (index % columns) * 38;
                    node.Y = (index / columns) * 38;
                    node.Fixed = true;
                    node.Size = 5 + index % 3;
                    node.BadgeText = index % 100 == 0 ? "hub" : null;
                });
            }
        }

        for (var index = 0; index < nodeCount; index++) {
            AddEdge(graph, index, (index + 1) % nodeCount, "ring");
            AddEdge(graph, index, (index * 31 + 97) % nodeCount, "cross");
        }

        return graph;
    }

    private static void AddEdge(GraphScene graph, int source, int target, string kind) {
        graph.AddEdge(kind + "-" + source.ToString("D5"), NodeId(source), NodeId(target), null, edge => {
            edge.Kind = kind;
            edge.Status = source % 97 == 0 ? "critical" : "healthy";
            edge.ShowLabel = false;
            edge.Directed = kind == "cross";
            edge.Style.Width = kind == "cross" ? 0.8 : 0.5;
        });
    }

    private static string NodeId(int index) => "n-" + index.ToString("D5");

    private static string FileName(int nodeCount) => "graph-scale-" + nodeCount + ".html";

    private static void SaveText(this string value, string path) => File.WriteAllText(path, value);
}

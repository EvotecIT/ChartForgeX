using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphExplorerStageExportExample {
    public static void Write(string output) {
        var graph = Build();
        graph.ToGraphExplorerHtmlPage(options => {
            options.PageTitle = "2,000 Object Interactive Estate";
        }).SaveText(Path.Combine(output, "graph-2000-interactive.html"));

        var stageImages = graph.SaveGraphStageImages(output, "graph-2000-stage", options => {
            options.Stages.Depths.AddRange(new[] { 0, 1, 2, 3, 5 });
            options.Formats = GraphSceneStaticImageFormat.Both;
            options.Render.Width = 1600;
            options.Render.Height = 900;
            options.Render.MaximumNodeLabels = 12;
        });
        foreach (var stage in stageImages) File.WriteAllText(Path.ChangeExtension(stage.SvgPath!, ".static-only"), "Standalone SVG/PNG stage; no HTML companion is required.\n");
    }

    private static GraphScene Build() {
        var graph = GraphScene.Create("graph-2000-estate", "2,000 Object Global Estate");
        graph.Subtitle = "Explore 2,001 objects with Barnes-Hut physics and direct hierarchy drill-down, or use the five script-free SVG/PNG stages in reports and generated documentation.";
        graph.Options.Features = GraphSceneFeatures.Explorer | GraphSceneFeatures.HierarchyNavigation | GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry;
        graph.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        graph.Options.Layout.Direction = GraphLayoutDirection.TopToBottom;
        graph.Options.Layout.LevelSeparation = 150;
        graph.Options.Layout.NodeSpacing = 28;
        graph.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        graph.Options.Physics.Stabilization.Iterations = 220;
        graph.Options.Physics.BarnesHut.SpringLength = 44;
        graph.Options.Physics.BarnesHut.GravitationalConstant = -2800;
        graph.Options.Physics.BarnesHut.AvoidOverlap = 0.68;
        graph.Options.Hierarchy.InitialRootNodeId = "estate";
        graph.Options.Hierarchy.InitialDepth = 1;
        graph.Options.LevelOfDetail.HideEdgeLabelsThreshold = 100;
        graph.Options.LevelOfDetail.CompactNodeThreshold = 300;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 800;
        graph.Options.LevelOfDetail.WebGlPreferredNodeThreshold = 1600;
        graph.Options.Cluster.Mode = GraphClusterMode.ByGroup;
        graph.Options.Cluster.Adaptive = true;
        graph.Options.Cluster.MinimumClusterSize = 8;
        graph.Options.Cluster.TargetClusterSize = 100;
        graph.Options.Performance.MaxInteractiveWebGlNodes = 5000;
        graph.Options.Performance.MaxInteractiveWebGlEdges = 10000;

        AddNode(graph, "estate", "Global estate", null, 0, "estate", "healthy", 20, "GE", Image("GE", "#0f172a", "#38bdf8"));
        for (var index = 0; index < 10; index++) {
            var id = "region-" + index.ToString("00");
            AddNode(graph, id, "Region " + (index + 1).ToString("00"), "estate", 1, "region", index == 2 ? "warning" : "healthy", 15, "R" + (index + 1), Image("R" + (index + 1), "#172554", "#60a5fa"));
        }
        for (var index = 0; index < 50; index++) AddNode(graph, "zone-" + index.ToString("000"), "Zone " + (index + 1).ToString("000"), "region-" + (index % 10).ToString("00"), 2, "zone", index % 17 == 0 ? "warning" : "healthy", 10, "Z", null);
        for (var index = 0; index < 200; index++) AddNode(graph, "site-" + index.ToString("000"), "Site " + (index + 1).ToString("000"), "zone-" + (index % 50).ToString("000"), 3, "site", index % 53 == 0 ? "critical" : "healthy", 7, "S", null);
        for (var index = 0; index < 400; index++) AddNode(graph, "platform-" + index.ToString("000"), "Platform " + (index + 1).ToString("000"), "site-" + (index % 200).ToString("000"), 4, index % 4 == 0 ? "database" : "platform", index % 61 == 0 ? "warning" : "healthy", 5.5, "P", null);
        for (var index = 0; index < 1340; index++) AddNode(graph, "workload-" + index.ToString("0000"), "Workload " + (index + 1).ToString("0000"), "platform-" + (index % 400).ToString("000"), 5, "workload", index % 211 == 0 ? "critical" : index % 79 == 0 ? "warning" : "healthy", 3.8, string.Empty, null);

        foreach (var node in graph.Nodes.Where(node => node.ParentId != null)) {
            graph.AddEdge(node.ParentId + "-" + node.Id, node.ParentId!, node.Id, configure: edge => {
                edge.Kind = "contains";
                edge.LayoutDirected = true;
                edge.Directed = true;
                edge.ShowLabel = false;
                edge.Style.Width = node.Level <= 2 ? 1.4 : 0.6;
            });
        }
        return graph;
    }

    private static void AddNode(GraphScene graph, string id, string label, string? parentId, int level, string kind, string status, double size, string icon, string? image) {
        graph.AddNode(id, label, node => {
            node.ParentId = parentId;
            node.Level = level;
            node.GroupId = level <= 1 ? id : AncestorGroup(parentId);
            node.Kind = kind;
            node.Status = status;
            node.Size = size;
            node.IconText = string.IsNullOrWhiteSpace(icon) ? null : icon;
            node.Shape = image == null ? level <= 2 ? GraphNodeShape.Box : kind == "database" ? GraphNodeShape.Database : GraphNodeShape.Circle : GraphNodeShape.Image;
            node.ImageUrl = image;
            node.ImageAlt = image == null ? null : label + " icon";
            node.Style.BackgroundColor = status == "critical" ? "#7f1d1d" : status == "warning" ? "#78350f" : level <= 2 ? "#172554" : "#0f766e";
            node.Style.BorderColor = status == "critical" ? "#fb7185" : status == "warning" ? "#fbbf24" : "#5eead4";
            node.Style.LabelColor = "#0f172a";
            node.Style.LabelBackgroundColor = "#ffffff";
            node.Style.Shadow = level <= 2;
        });
    }

    private static string AncestorGroup(string? parentId) {
        if (string.IsNullOrWhiteSpace(parentId)) return "estate";
        var separator = parentId!.IndexOf('-');
        return separator < 0 ? parentId : parentId.Substring(0, separator) + "-group";
    }

    private static string Image(string text, string fill, string border) {
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><rect width='64' height='64' rx='18' fill='{fill}'/><rect x='4' y='4' width='56' height='56' rx='15' fill='none' stroke='{border}' stroke-width='3'/><text x='32' y='39' text-anchor='middle' font-family='Arial' font-size='15' font-weight='700' fill='white'>{text}</text></svg>";
        return "data:image/svg+xml," + Uri.EscapeDataString(svg);
    }

    private static void SaveText(this string value, string path) => File.WriteAllText(path, value);
}

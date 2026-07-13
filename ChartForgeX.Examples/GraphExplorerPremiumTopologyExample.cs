using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphExplorerPremiumTopologyExample {
    public static void Write(string output) {
        var graph = Build();
        graph.ToGraphExplorerHtmlPage(options => {
            options.PageTitle = "Global Estate Premium Topology";
            options.RenderBackend = HtmlGraphRenderBackend.Svg;
        }).SaveText(Path.Combine(output, "global-estate-premium-topology.html"));
    }

    private static GraphScene Build() {
        var graph = GraphScene.Create("global-estate-premium-topology", "Global Estate Topology");
        graph.Subtitle = "Drill from the global estate into regions, platforms, and workloads. Double-click a node to enter it; use the breadcrumb or Up control to return.";
        graph.Options.UseSuperTopologyDefaults();
        graph.Options.Physics.Solver = GraphPhysicsSolver.StaticPrepared;
        graph.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        graph.Options.Layout.Direction = GraphLayoutDirection.LeftToRight;
        graph.Options.Layout.LevelSeparation = 250;
        graph.Options.Layout.NodeSpacing = 138;
        graph.Options.Hierarchy.InitialRootNodeId = "estate";
        graph.Options.Hierarchy.InitialDepth = 1;
        graph.Options.LevelOfDetail.WebGlPreferredNodeThreshold = 2500;
        graph.Options.Cluster.Mode = GraphClusterMode.Explicit;
        graph.Options.Cluster.Adaptive = false;

        AddNode(graph, "estate", "Contoso estate", null, 0, "global", "healthy", "42", "Global control plane", "GL", "#0F172A", "#38BDF8", GraphNodeShape.Image);
        AddNode(graph, "eu", "Europe", "estate", 1, "region", "warning", "18", "4 sites · 18 workloads", "EU", "#172554", "#60A5FA", GraphNodeShape.Image);
        AddNode(graph, "us", "Americas", "estate", 1, "region", "healthy", "16", "3 sites · 16 workloads", "US", "#052E2B", "#2DD4BF", GraphNodeShape.Image);
        AddNode(graph, "apac", "Asia Pacific", "estate", 1, "region", "healthy", "8", "2 sites · 8 workloads", "AP", "#3B1D0B", "#FB923C", GraphNodeShape.Image);

        AddPlatform(graph, "eu", "eu-identity", "Identity plane", "identity", "warning", "3", "Entra · AD DS · PKI", "ID", 2);
        AddPlatform(graph, "eu", "eu-apps", "Application plane", "application", "healthy", "9", "Portal · CRM · ERP", "AP", 2);
        AddPlatform(graph, "eu", "eu-data", "Data plane", "database", "critical", "2", "SQL · Blob · Vault", "DB", 2);
        AddPlatform(graph, "us", "us-identity", "Identity plane", "identity", "healthy", "2", "Entra · AD DS", "ID", 2);
        AddPlatform(graph, "us", "us-apps", "Application plane", "application", "warning", "6", "Commerce · API", "AP", 2);
        AddPlatform(graph, "us", "us-data", "Data plane", "database", "healthy", "4", "SQL · Lake", "DB", 2);
        AddPlatform(graph, "apac", "apac-edge", "Edge plane", "endpoint", "healthy", "8", "Gateways · kiosks", "ED", 2);

        AddLeaf(graph, "eu-identity", "eu-entra", "Entra ID", "identity", "healthy", "Directory sync current", "E");
        AddLeaf(graph, "eu-identity", "eu-ad", "AD DS", "service", "warning", "Replication lag 38 s", "AD");
        AddLeaf(graph, "eu-identity", "eu-pki", "Issuing CA", "certificate", "critical", "Certificate expires in 8 d", "CA");
        AddLeaf(graph, "eu-apps", "eu-portal", "Customer portal", "application", "healthy", "99.98% availability", "P");
        AddLeaf(graph, "eu-apps", "eu-crm", "CRM", "application", "healthy", "2.1k active users", "C");
        AddLeaf(graph, "eu-data", "eu-sql", "SQL primary", "database", "critical", "Backup gap detected", "SQL");
        AddLeaf(graph, "eu-data", "eu-vault", "Secrets vault", "database", "healthy", "Rotation current", "V");
        AddLeaf(graph, "us-identity", "us-entra", "Entra ID", "identity", "healthy", "Directory sync current", "E");
        AddLeaf(graph, "us-apps", "us-api", "Commerce API", "application", "warning", "P95 latency 410 ms", "API");
        AddLeaf(graph, "us-data", "us-lake", "Analytics lake", "database", "healthy", "12 TB governed", "L");
        AddLeaf(graph, "apac-edge", "apac-gateway", "Edge gateways", "endpoint", "healthy", "8 of 8 online", "GW");
        ApplyPremiumPositions(graph);

        AddCluster(graph, "regions", "Global regions", null, "estate", "eu", "us", "apac");
        AddCluster(graph, "eu-platforms", "Europe platforms", "regions", "eu-identity", "eu-apps", "eu-data");
        AddCluster(graph, "us-platforms", "Americas platforms", "regions", "us-identity", "us-apps", "us-data");
        AddCluster(graph, "apac-platforms", "Asia Pacific platforms", "regions", "apac-edge");

        Connect(graph, "estate", "eu", "governs", "healthy");
        Connect(graph, "estate", "us", "governs", "healthy");
        Connect(graph, "estate", "apac", "governs", "healthy");
        ConnectParentChildren(graph);
        Connect(graph, "eu-portal", "eu-sql", "queries", "warning");
        Connect(graph, "us-api", "us-lake", "streams", "healthy");
        Connect(graph, "eu-entra", "us-entra", "federates", "healthy");
        return graph;
    }

    private static void ApplyPremiumPositions(GraphScene graph) {
        var positions = new Dictionary<string, (double X, double Y)> {
            ["estate"] = (100, 280),
            ["eu"] = (330, 110),
            ["us"] = (330, 280),
            ["apac"] = (330, 450),
            ["eu-identity"] = (560, 50),
            ["eu-apps"] = (560, 125),
            ["eu-data"] = (560, 200),
            ["us-identity"] = (560, 265),
            ["us-apps"] = (560, 335),
            ["us-data"] = (560, 405),
            ["apac-edge"] = (560, 500),
            ["eu-entra"] = (820, 20),
            ["eu-ad"] = (820, 65),
            ["eu-pki"] = (820, 110),
            ["eu-portal"] = (820, 165),
            ["eu-crm"] = (820, 215),
            ["eu-sql"] = (820, 270),
            ["eu-vault"] = (820, 320),
            ["us-entra"] = (820, 375),
            ["us-api"] = (820, 420),
            ["us-lake"] = (820, 465),
            ["apac-gateway"] = (820, 520)
        };
        foreach (var node in graph.Nodes) {
            var point = positions[node.Id];
            node.X = point.X;
            node.Y = point.Y;
            node.Fixed = true;
        }
    }

    private static void AddPlatform(GraphScene graph, string parent, string id, string label, string kind, string status, string badge, string secondary, string icon, int level) =>
        AddNode(graph, id, label, parent, level, kind, status, badge, secondary, icon, "#0F172A", status == "critical" ? "#FB7185" : status == "warning" ? "#FBBF24" : "#34D399", GraphNodeShape.Box);

    private static void AddLeaf(GraphScene graph, string parent, string id, string label, string kind, string status, string secondary, string icon) =>
        AddNode(graph, id, label, parent, 3, kind, status, null, secondary, icon, "#111827", status == "critical" ? "#FB7185" : status == "warning" ? "#FBBF24" : "#34D399", GraphNodeShape.Circle);

    private static void AddNode(GraphScene graph, string id, string label, string? parent, int level, string kind, string status, string? badge, string secondary, string icon, string fill, string border, GraphNodeShape shape) {
        graph.AddNode(id, label, node => {
            node.ParentId = parent;
            node.Level = level;
            node.Kind = kind;
            node.Status = status;
            node.SecondaryLabel = secondary;
            node.BadgeText = badge;
            node.IconText = icon;
            node.Shape = shape;
            node.Size = level == 0 ? 21 : level == 1 ? 17 : 13;
            node.Style.BackgroundColor = fill;
            node.Style.BorderColor = border;
            node.Style.LabelColor = "#0F172A";
            node.Style.Shadow = true;
            if (shape == GraphNodeShape.Image) {
                node.ImageUrl = Image(icon, fill, border);
                node.ImageAlt = label + " topology icon";
            }
        });
    }

    private static void AddCluster(GraphScene graph, string id, string label, string? parentClusterId, params string[] nodeIds) {
        graph.AddCluster(id, label, nodeIds, cluster => {
            cluster.Kind = "navigation";
            cluster.ParentClusterId = parentClusterId;
            cluster.Collapsed = false;
        });
    }

    private static void ConnectParentChildren(GraphScene graph) {
        foreach (var node in graph.Nodes.Where(node => node.ParentId != null && node.ParentId != "estate")) Connect(graph, node.ParentId!, node.Id, "contains", node.Status ?? "healthy");
    }

    private static void Connect(GraphScene graph, string source, string target, string label, string status) {
        graph.AddEdge(source + "-" + target, source, target, label, edge => {
            edge.Kind = label is "contains" or "governs" ? "hierarchy" : "dependency";
            edge.Status = status;
            edge.Directed = true;
            edge.LayoutDirected = true;
            edge.ShowLabel = edge.Kind != "hierarchy";
            edge.Style.Width = label == "governs" ? 2.6 : 1.5;
        });
    }

    private static string Image(string icon, string fill, string border) {
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'><stop stop-color='{border}'/><stop offset='1' stop-color='{fill}'/></linearGradient></defs><rect width='64' height='64' rx='18' fill='url(#g)'/><circle cx='32' cy='32' r='20' fill='none' stroke='rgba(255,255,255,.28)'/><text x='32' y='38' text-anchor='middle' font-family='Arial' font-size='17' font-weight='700' fill='white'>{icon}</text></svg>";
        return "data:image/svg+xml," + Uri.EscapeDataString(svg);
    }

    private static void SaveText(this string value, string path) => File.WriteAllText(path, value);
}

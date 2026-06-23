using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class GraphExplorerExamples {
    public static void Write(string output) {
        var graph = BuildIdentityRiskGraph();
        graph.ToGraphExplorerHtmlPage(options => {
            options.PageTitle = "Identity Risk Graph Explorer";
            options.RenderBackend = HtmlGraphRenderBackend.Svg;
        }).SaveText(Path.Combine(output, "identity-risk-graph-explorer.html"));

        var benchmark = BuildEnterpriseAccessBenchmark();
        benchmark.ToGraphExplorerHtmlPage(options => {
            options.PageTitle = "Enterprise Access Graph Benchmark";
            options.RenderBackend = HtmlGraphRenderBackend.Svg;
        }).SaveText(Path.Combine(output, "enterprise-access-graph-benchmark.html"));
    }

    private static GraphScene BuildEnterpriseAccessBenchmark() {
        const int clusters = 12;
        const int nodesPerCluster = 30;
        var graph = GraphScene.Create("enterprise-access-graph-benchmark", "Enterprise Access Graph Benchmark");
        graph.Subtitle = "Large dependency-free Canvas fallback benchmark with 360 nodes, 720 directed edges, image/icon nodes, drag, pan, zoom, LOD, and performance telemetry.";
        graph.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry);
        graph.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        graph.Options.Physics.StabilizationIterations = 90;
        graph.Options.Physics.LinkDistance = 58;
        graph.Options.Physics.Repulsion = 3600;
        graph.Options.LevelOfDetail.ClusterNodeThreshold = 80;
        graph.Options.LevelOfDetail.HideEdgeLabelsThreshold = 260;
        graph.Options.LevelOfDetail.CompactNodeThreshold = 180;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 160;
        graph.Options.LevelOfDetail.CollapseClustersOnLoad = false;
        graph.Options.Performance.FrameBudgetMilliseconds = 16;
        graph.Options.Performance.MaxInteractiveSvgNodes = 150;
        graph.Options.Performance.MaxInteractiveSvgEdges = 320;
        graph.Options.Performance.MaxInteractiveCanvasNodes = 900;
        graph.Options.Performance.MaxInteractiveCanvasEdges = 1800;
        graph.Options.Performance.TelemetrySampleInterval = 15;

        for (var cluster = 0; cluster < clusters; cluster++) {
            var clusterId = "access-zone-" + cluster.ToString("00");
            var nodeIds = Enumerable.Range(0, nodesPerCluster).Select(index => NodeId(cluster, index)).ToArray();
            graph.AddCluster(clusterId, "Access zone " + (cluster + 1).ToString("00"), nodeIds, item => {
                item.Kind = "community";
                item.Collapsed = false;
            });

            for (var index = 0; index < nodesPerCluster; index++) {
                var kind = KindFor(index);
                graph.AddNode(NodeId(cluster, index), Label(kind, cluster, index), node => {
                    node.Kind = kind;
                    node.ClusterId = clusterId;
                    node.GroupId = clusterId;
                    node.Status = index % 17 == 0 ? "critical" : index % 7 == 0 ? "warning" : "healthy";
                    node.Size = 7 + index % 5;
                    node.Shape = index % 5 == 0 ? GraphNodeShape.Image : index % 5 == 1 ? GraphNodeShape.Box : GraphNodeShape.Circle;
                    node.IconText = IconFor(kind);
                    if (node.Shape == GraphNodeShape.Image) {
                        node.ImageUrl = ImageFor(kind);
                        node.ImageAlt = "Benchmark " + kind + " icon";
                    }
                });
            }
        }

        for (var cluster = 0; cluster < clusters; cluster++) {
            for (var index = 0; index < nodesPerCluster; index++) {
                var source = NodeId(cluster, index);
                var localTarget = NodeId(cluster, (index * 7 + 11) % nodesPerCluster);
                var remoteTarget = NodeId((cluster + 1 + index % 3) % clusters, (index * 5 + cluster) % nodesPerCluster);
                Link(graph, source, localTarget, "uses", "session", index % 7 == 0 ? "warning" : "healthy", 1 + index % 3);
                Link(graph, source, remoteTarget, "trusts", index % 6 == 0 ? "finding" : "dependency", index % 17 == 0 ? "critical" : "healthy", 1 + index % 2);
            }
        }

        return graph;
    }

    private static GraphScene BuildIdentityRiskGraph() {
        var graph = GraphScene.Create("identity-risk-graph-explorer", "Identity Risk Graph Explorer");
        graph.Subtitle = "Product-real service, identity, owner, finding, and evidence relationships with runtime physics, clusters, LOD, and performance telemetry.";
        graph.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry);
        graph.Options.Physics.Solver = GraphPhysicsSolver.ForceAtlas2;
        graph.Options.Physics.StabilizationIterations = 700;
        graph.Options.Physics.LinkDistance = 92;
        graph.Options.Physics.Repulsion = 5200;
        graph.Options.LevelOfDetail.ClusterNodeThreshold = 28;
        graph.Options.LevelOfDetail.HideEdgeLabelsThreshold = 70;
        graph.Options.LevelOfDetail.CompactNodeThreshold = 42;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 160;
        graph.Options.LevelOfDetail.CollapseClustersOnLoad = false;
        graph.Options.Performance.FrameBudgetMilliseconds = 16;
        graph.Options.Performance.MaxInteractiveSvgNodes = 180;
        graph.Options.Performance.MaxInteractiveSvgEdges = 420;
        graph.Options.Performance.MaxInteractiveCanvasNodes = 2000;
        graph.Options.Performance.MaxInteractiveCanvasEdges = 5000;
        graph.Options.Performance.TelemetrySampleInterval = 20;

        AddCluster(graph, "identity", "Identity core", "service", "dc-01", "dc-02", "dc-03", "entra-connect", "adfs", "ca-root", "ca-issuing", "dns-core");
        AddCluster(graph, "apps", "Business apps", "application", "crm", "erp", "ticketing", "hr", "vpn", "portal", "mail", "monitoring");
        AddCluster(graph, "data", "Data stores", "database", "sql-prod", "sql-audit", "file-shares", "secrets", "logs", "backups");
        AddCluster(graph, "owners", "Owners", "team", "iam-team", "infra-team", "app-team", "security-team", "service-desk", "vendor-team");
        AddCluster(graph, "findings", "Findings", "finding", "stale-admins", "weak-tls", "expired-cert", "open-share", "no-mfa", "dns-drift", "slow-repl", "backup-gap");
        AddCluster(graph, "endpoints", "Endpoints", "endpoint", "workstation-a", "workstation-b", "kiosk-01", "build-agent", "admin-laptop", "jump-host");

        Link(graph, "dc-01", "dc-02", "replicates", "replication", "healthy", 2);
        Link(graph, "dc-02", "dc-03", "replicates", "replication", "warning", 2);
        Link(graph, "dc-01", "dns-core", "hosts", "dependency", "healthy", 1);
        Link(graph, "entra-connect", "dc-01", "syncs", "dependency", "healthy", 2);
        Link(graph, "adfs", "ca-issuing", "trusts", "certificate", "warning", 2);
        Link(graph, "ca-issuing", "ca-root", "chains", "certificate", "healthy", 1);
        Link(graph, "vpn", "adfs", "authenticates", "authentication", "warning", 2);
        Link(graph, "portal", "entra-connect", "provisions", "identity", "healthy", 1);
        Link(graph, "crm", "sql-prod", "queries", "dataflow", "healthy", 2);
        Link(graph, "erp", "sql-prod", "queries", "dataflow", "healthy", 2);
        Link(graph, "ticketing", "sql-audit", "writes", "dataflow", "healthy", 1);
        Link(graph, "hr", "file-shares", "exports", "dataflow", "warning", 1);
        Link(graph, "mail", "secrets", "uses", "dependency", "healthy", 1);
        Link(graph, "monitoring", "logs", "reads", "dataflow", "healthy", 1);
        Link(graph, "backups", "sql-prod", "protects", "dependency", "warning", 1);
        Link(graph, "iam-team", "dc-01", "owns", "ownership", "healthy", 1);
        Link(graph, "infra-team", "dns-core", "owns", "ownership", "healthy", 1);
        Link(graph, "app-team", "crm", "owns", "ownership", "healthy", 1);
        Link(graph, "security-team", "monitoring", "owns", "ownership", "healthy", 1);
        Link(graph, "service-desk", "ticketing", "owns", "ownership", "healthy", 1);
        Link(graph, "vendor-team", "vpn", "supports", "ownership", "warning", 1);
        Link(graph, "stale-admins", "dc-01", "affects", "finding", "critical", 3);
        Link(graph, "weak-tls", "adfs", "affects", "finding", "warning", 2);
        Link(graph, "expired-cert", "ca-issuing", "affects", "finding", "critical", 3);
        Link(graph, "open-share", "file-shares", "affects", "finding", "warning", 2);
        Link(graph, "no-mfa", "vpn", "affects", "finding", "critical", 3);
        Link(graph, "dns-drift", "dns-core", "affects", "finding", "warning", 2);
        Link(graph, "slow-repl", "dc-03", "affects", "finding", "warning", 2);
        Link(graph, "backup-gap", "backups", "affects", "finding", "critical", 3);
        Link(graph, "admin-laptop", "jump-host", "accesses", "session", "healthy", 1);
        Link(graph, "jump-host", "dc-01", "admin", "session", "warning", 2);
        Link(graph, "build-agent", "secrets", "reads", "dependency", "warning", 1);
        Link(graph, "workstation-a", "portal", "uses", "session", "healthy", 1);
        Link(graph, "workstation-b", "mail", "uses", "session", "healthy", 1);
        Link(graph, "kiosk-01", "vpn", "uses", "session", "warning", 1);

        return graph;
    }

    private static void AddCluster(GraphScene graph, string id, string label, string kind, params string[] nodeIds) {
        graph.AddCluster(id, label, nodeIds, cluster => {
            cluster.Kind = "community";
            cluster.Collapsed = false;
        });

        for (var index = 0; index < nodeIds.Length; index++) {
            var nodeId = nodeIds[index];
            graph.AddNode(nodeId, Label(nodeId), node => {
                node.Kind = kind;
                node.ClusterId = id;
                node.GroupId = id;
                node.Status = StatusFor(id, index);
                node.Size = id == "findings" ? 10 + index % 3 : 8 + index % 4;
                node.Shape = index == 0 || kind is "service" or "application" ? GraphNodeShape.Image : kind == "team" ? GraphNodeShape.Box : GraphNodeShape.Circle;
                node.IconText = IconFor(kind);
                if (node.Shape == GraphNodeShape.Image) {
                    node.ImageUrl = ImageFor(kind);
                    node.ImageAlt = label + " " + kind + " icon";
                }
                node.Metadata["cluster"] = label;
            });
        }
    }

    private static void Link(GraphScene graph, string source, string target, string label, string kind, string status, double weight) {
        graph.AddEdge(source + "-" + target + "-" + kind, source, target, label, edge => {
            edge.Kind = kind;
            edge.Status = status;
            edge.Weight = weight;
            edge.Length = kind == "finding" ? 72 : 96;
            edge.Directed = true;
            edge.Shape = kind is "finding" or "ownership" or "session" ? GraphEdgeShape.Curve : GraphEdgeShape.Line;
            edge.Curvature = kind == "finding" ? 46 : kind == "ownership" ? -34 : kind == "session" ? 26 : 0;
            edge.Dashed = status is "warning" or "critical";
        });
    }

    private static string Label(string id) {
        var words = id.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < words.Length; index++) {
            if (words[index].Length == 0) continue;
            words[index] = char.ToUpperInvariant(words[index][0]) + words[index].Substring(1);
        }

        return string.Join(" ", words);
    }

    private static string Label(string kind, int cluster, int index) {
        return kind + " " + cluster.ToString("00") + "." + index.ToString("00");
    }

    private static string StatusFor(string clusterId, int index) {
        if (clusterId == "findings") return index % 3 == 0 ? "critical" : "warning";
        if (clusterId == "identity" && index is 1 or 4) return "warning";
        if (clusterId == "endpoints" && index >= 3) return "warning";
        return "healthy";
    }

    private static string NodeId(int cluster, int index) => "z" + cluster.ToString("00") + "-n" + index.ToString("00");

    private static string KindFor(int index) => (index % 6) switch {
        0 => "identity",
        1 => "application",
        2 => "database",
        3 => "endpoint",
        4 => "team",
        _ => "service"
    };

    private static string IconFor(string kind) {
        return kind switch {
            "service" => "S",
            "application" => "A",
            "database" => "D",
            "team" => "T",
            "finding" => "!",
            "endpoint" => "E",
            _ => "N"
        };
    }

    private static string ImageFor(string kind) {
        var fill = kind switch {
            "service" => "%230f766e",
            "application" => "%232563eb",
            "database" => "%237c3aed",
            "team" => "%230f172a",
            "finding" => "%23dc2626",
            "endpoint" => "%23ea580c",
            _ => "%23475569"
        };
        return "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'%3E%3Crect width='64' height='64' rx='16' fill='" + fill + "'/%3E%3Ccircle cx='32' cy='26' r='12' fill='white' opacity='.92'/%3E%3Cpath d='M16 54c3-12 29-12 32 0' fill='white' opacity='.78'/%3E%3C/svg%3E";
    }

    private static void SaveText(this string value, string path) {
        File.WriteAllText(path, value.Replace("\r\n", "\n"));
    }
}

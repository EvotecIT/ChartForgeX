using System;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SuperTopologyBridgeMapsTopologyToGraphExplorerContract() {
        var topology = TopologyChart.Create()
            .WithId("super-topology")
            .WithTitle("Super Topology")
            .WithSubtitle("Topology projected into graph exploration")
            .WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("core", "Core", 20, 20, 280, 180, TopologyHealthStatus.Warning, subtitle: "Primary services", iconId: "common:service")
            .AddGroup("edge", "Edge", 360, 20, 280, 180, TopologyHealthStatus.Healthy)
            .AddNode("api", "API", 64, 82, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, groupId: "core", subtitle: "Public API", symbol: "API", iconId: "common:service")
            .AddNode("db", "Database", 180, 84, TopologyNodeKind.Database, TopologyHealthStatus.Warning, groupId: "core", subtitle: "SQL", symbol: "SQL")
            .AddNode("cdn", "CDN", 440, 92, TopologyNodeKind.Network, TopologyHealthStatus.Healthy, groupId: "edge", subtitle: "Ingress", symbol: "NET")
            .AddEdge("cdn-api", "cdn", "api", "routes", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("api-db", "api", "db", "queries", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, secondaryLabel: "32 ms");
        topology.WithNodeColor("api", "#0F766E").WithNodeBackground("api", "#CCFBF1").WithEdgeColor("cdn-api", "#7C3AED");
        topology.Nodes[0].Metadata["owner"] = "identity";
        topology.Nodes[1].Metrics["latency"] = "32";
        topology.Edges[1].Metrics["transport"] = "tcp";

        var scene = topology.ToGraphScene(options => options.EnableManipulation = true);
        scene.Validate();

        Assert(scene.Id == "super-topology" && scene.Title == "Super Topology" && scene.Subtitle == "Topology projected into graph exploration", "Topology graph bridge should preserve chart identity and title metadata.");
        Assert(scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics) && scene.Options.HasFeature(GraphSceneFeatures.Manipulation), "Super topology bridge should apply large-graph defaults and opt-in manipulation features.");
        Assert(scene.Options.Cluster.Mode == GraphClusterMode.Hybrid && scene.Options.Cluster.Adaptive, "Super topology bridge should use hybrid adaptive clustering so explicit topology groups and runtime summaries can coexist.");
        Assert(scene.Options.Manipulation.CanAddNodes && scene.Options.Manipulation.CanEditEdges && scene.Options.Manipulation.CanPersistPositions, "Super topology manipulation should advertise reusable edit and persisted-position capabilities.");
        Assert(scene.Nodes.Count == 3 && scene.Edges.Count == 2 && scene.Clusters.Count == 2, "Topology graph bridge should map topology nodes, edges, and groups into graph nodes, edges, and clusters.");
        Assert(scene.Nodes[0].ClusterId == "core" && scene.Clusters[0].NodeIds.Count == 2, "Topology groups should seed graph cluster membership.");
        Assert(scene.Nodes[0].Metadata["topology.meta.owner"] == "identity" && scene.Nodes[1].Metadata["topology.metric.latency"] == "32", "Topology graph nodes should carry source metadata and metrics for inspectors.");
        Assert(scene.Edges[0].Shape == GraphEdgeShape.Curve && scene.Edges[0].Directed, "Topology graph edges should preserve curved directed relationship hints.");
        Assert(scene.Nodes[0].Style.BackgroundColor == "#CCFBF1" && scene.Nodes[0].Style.BorderColor == "#0F766E" && scene.Nodes[1].Style.BorderColor == "#F97316" && scene.Edges[0].Style.Color == "#7C3AED", "Topology graph bridge should map explicit and status-derived topology colors into reusable GraphScene styling, not only metadata.");
        Assert(scene.Edges[1].Metadata["topology.metric.transport"] == "tcp" && scene.Edges[1].Metadata["topology.secondaryLabel"] == "32 ms", "Topology graph edges should carry topology metrics and secondary labels.");
        Assert(scene.Nodes[0].HasExplicitPosition && scene.Nodes[0].Fixed, "Manual topology coordinates should seed fixed graph positions for deterministic opening layouts.");

        var html = topology.ToGraphExplorerHtmlFragment(
            configureScene: options => options.EnableManipulation = true,
            configureHtml: options => options.RenderBackend = HtmlGraphRenderBackend.Canvas);
        Assert(html.Contains("data-cfx-graph-id=\"super-topology\"", StringComparison.Ordinal), "Topology graph explorer output should expose the original topology id.");
        Assert(html.Contains("data-cfx-graph-renderer=\"canvas\"", StringComparison.Ordinal), "Topology graph explorer output should allow Canvas large-scene rendering.");
        Assert(html.Contains("data-cfx-graph-cluster-mode=\"Hybrid\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-cluster-adaptive=\"true\"", StringComparison.Ordinal), "Topology graph explorer output should expose hybrid adaptive clustering policy.");
        Assert(html.Contains("data-cfx-graph-manipulation=\"true\"", StringComparison.Ordinal) && html.Contains("persistPositions", StringComparison.Ordinal), "Topology graph explorer output should expose opt-in manipulation capabilities.");
        Assert(html.Contains("clustering: {", StringComparison.Ordinal) && html.Contains("minimumClusterSize", StringComparison.Ordinal) && html.Contains("data-cfx-graph-cluster-count", StringComparison.Ordinal), "Topology graph JSON export should preserve clustering policy and runtime cluster state.");
        Assert(html.Contains("manipulation: {", StringComparison.Ordinal) && html.Contains("data-cfx-graph-manipulation-capabilities", StringComparison.Ordinal), "Topology graph JSON export should preserve opt-in manipulation capability policy.");
        Assert(html.Contains("data-node-id=\"api\"", StringComparison.Ordinal) && html.Contains("data-node-cluster=\"core\"", StringComparison.Ordinal), "Topology graph explorer output should render topology nodes with cluster metadata.");
        Assert(html.Contains("data-node-background-color=\"#CCFBF1\"", StringComparison.Ordinal) && html.Contains("data-node-border-color=\"#0F766E\"", StringComparison.Ordinal) && html.Contains("data-edge-color=\"#7C3AED\"", StringComparison.Ordinal), "Topology graph explorer output should serialize topology style hints for SVG, Canvas, PNG, and export paths.");

        var noClusterScene = topology.ToGraphScene(options => options.IncludeGroupsAsClusters = false);
        noClusterScene.Validate();
        Assert(noClusterScene.Clusters.Count == 0 && noClusterScene.GetEffectiveClusters().Count == 0 && noClusterScene.Nodes.All(node => string.IsNullOrWhiteSpace(node.ClusterId)), "Topology graph bridge should not assign dangling cluster ids or derive replacement clusters when group cluster rendering is disabled.");

        var anchored = TopologyChart.Create()
            .WithId("hidden-anchor")
            .AddNode("source", "Source", 20, 20, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("anchor", "Anchor", 140, 20, TopologyNodeKind.Network, TopologyHealthStatus.Unknown)
            .AddNode("target", "Target", 260, 20, TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
            .AddEdge("source-anchor", "source", "anchor", "route", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy)
            .AddEdge("anchor-target", "anchor", "target", "route", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional);
        anchored.WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);
        var anchoredScene = anchored.ToGraphScene();
        Assert(anchoredScene.Nodes.Single(node => node.Id == "anchor").Hidden && anchoredScene.Edges.Single(edge => edge.Id == "anchor-target").SourceArrow && anchoredScene.Edges.Single(edge => edge.Id == "anchor-target").TargetArrow, "Topology graph bridge should preserve hidden routing anchors and bidirectional edge markers.");
        var anchoredHtml = anchored.ToGraphExplorerHtmlFragment();
        Assert(anchoredHtml.Contains("data-node-id=\"anchor\"", StringComparison.Ordinal) && anchoredHtml.Contains("data-node-hidden=\"true\"", StringComparison.Ordinal) && anchoredHtml.Contains("endpointVisible(edgeVisualNode", StringComparison.Ordinal) && anchoredHtml.Contains("endpointAvailable(attr(edge, 'data-source-node-id'))", StringComparison.Ordinal) && anchoredHtml.Contains("data-edge-source-arrow=\"true\"", StringComparison.Ordinal), "Graph explorer output should keep hidden topology anchors available for Canvas, PNG, overview, and filter routing without drawing visible node marks.");

        var dashedTopology = TopologyChart.Create()
            .WithId("dash-parity")
            .AddNode("api", "API", 20, 40, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("db", "DB", 160, 40, TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
            .AddNode("queue", "Queue", 300, 40, TopologyNodeKind.Queue, TopologyHealthStatus.Healthy)
            .AddEdge("api-db", "api", "db", "warning", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning)
            .AddEdge("db-queue", "db", "queue", "dotted", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy)
            .AddEdge("queue-api", "queue", "api", "muted", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning);
        dashedTopology.WithEdgeLineStyle("db-queue", TopologyEdgeLineStyle.Dotted);
        dashedTopology.Edges[1].SecondaryLabel = "queue 7";
        dashedTopology.Edges[1].TertiaryLabel = "3m ago";
        dashedTopology.Edges[2].IsMuted = true;
        var dashedScene = dashedTopology.ToGraphScene();
        Assert(dashedScene.Edges.Single(edge => edge.Id == "api-db").Style.DashPattern == "8 5" && dashedScene.Edges.Single(edge => edge.Id == "db-queue").Style.DashPattern == "2 5", "Topology graph bridge should preserve auto status dashes and explicit dotted edge patterns.");
        Assert(dashedScene.Edges.Single(edge => edge.Id == "api-db").Style.Color == "#F97316" && dashedScene.Edges.Single(edge => edge.Id == "queue-api").Style.Color == "#CBD5E1", "Topology graph bridge should preserve status-derived edge colors, including muted fallback colors.");
        Assert(!dashedScene.Edges.Single(edge => edge.Id == "queue-api").Dashed && dashedScene.Edges.Single(edge => edge.Id == "db-queue").Label == "dotted / queue 7 / 3m ago", "Topology graph bridge should keep muted auto-dash edges solid while preserving secondary and tertiary label facts.");
        var dashedHtml = dashedTopology.ToGraphExplorerHtmlFragment();
        Assert(dashedHtml.Contains("data-edge-dash-pattern=\"8 5\"", StringComparison.Ordinal) && dashedHtml.Contains("data-edge-dash-pattern=\"2 5\"", StringComparison.Ordinal) && dashedHtml.Contains("stroke-dasharray:2 5", StringComparison.Ordinal) && dashedHtml.Contains("dashPattern: dashPattern(attr(el, 'data-edge-dash-pattern'), [8, 6])", StringComparison.Ordinal), "Graph explorer output should carry topology dash patterns into SVG and Canvas/PNG rendering state.");

        var duplicateEdges = TopologyChart.Create()
            .WithId("duplicate-edge-parity")
            .AddNode("a", "A", 20, 40, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("b", "B", 160, 40, TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
            .AddNode("c", "C", 300, 40, TopologyNodeKind.Queue, TopologyHealthStatus.Healthy)
            .AddEdge("duplicate-link", "a", "b", "primary", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy)
            .AddEdge("duplicate-link", "b", "c", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, secondaryLabel: "secondary only");
        var duplicateScene = duplicateEdges.ToGraphScene();
        duplicateScene.Validate();
        Assert(duplicateScene.Edges.Select(edge => edge.Id).Distinct(StringComparer.Ordinal).Count() == 2 && duplicateScene.Edges.Single(edge => edge.SourceNodeId == "b").Label == "secondary only", "Topology graph bridge should generate unique graph ids for duplicate topology edge ids and keep secondary-only labels visible.");

        var autoOrigin = TopologyChart.Create()
            .WithLayout(TopologyLayoutMode.Manual)
            .AddAutoNode("auto", "Auto", TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("origin", "Origin", 0, 0, TopologyNodeKind.Database, TopologyHealthStatus.Healthy);
        autoOrigin.Nodes.Single(node => node.Id == "auto").X = 220;
        autoOrigin.Nodes.Single(node => node.Id == "auto").Y = 80;
        var autoOriginScene = autoOrigin.ToGraphScene();
        Assert(autoOriginScene.Nodes.Single(node => node.Id == "auto").Fixed && autoOriginScene.Nodes.Single(node => node.Id == "auto").HasExplicitPosition && autoOriginScene.Nodes.Single(node => node.Id == "origin").Fixed && autoOriginScene.Nodes.Single(node => node.Id == "origin").HasExplicitPosition, "Topology graph bridge should preserve explicit coordinate mutations after AddAutoNode while preserving explicit manual origin nodes.");

        var freeAutoOrigin = TopologyChart.Create()
            .WithLayout(TopologyLayoutMode.Manual)
            .AddAutoNode("auto", "Auto", TopologyNodeKind.Service, TopologyHealthStatus.Healthy);
        var freeAutoOriginScene = freeAutoOrigin.ToGraphScene();
        Assert(!freeAutoOriginScene.Nodes.Single(node => node.Id == "auto").Fixed && !freeAutoOriginScene.Nodes.Single(node => node.Id == "auto").HasExplicitPosition, "Topology graph bridge should not pin untouched AddAutoNode placeholder coordinates at the origin.");

        var friendlyIds = TopologyChart.Create()
            .WithId("app map")
            .AddGroup("core services", "Core Services", 0, 0, 240, 160, TopologyHealthStatus.Healthy)
            .AddNode("app server", "App Server", 40, 50, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, groupId: "core services")
            .AddNode("sql db", "SQL DB", 160, 50, TopologyNodeKind.Database, TopologyHealthStatus.Warning, groupId: "core services")
            .AddEdge("app link", "app server", "sql db", "queries", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward);
        var friendlyScene = friendlyIds.ToGraphScene();
        friendlyScene.Validate();
        Assert(friendlyScene.Id == "app-map" && friendlyScene.Nodes.Any(node => node.Id == "app-server" && node.Metadata["topology.id"] == "app server") && friendlyScene.Clusters.Any(cluster => cluster.Id == "core-services" && cluster.Metadata["topology.id"] == "core services"), "Topology graph bridge should normalize friendly topology ids while preserving original ids in metadata.");
        Assert(friendlyScene.Edges.Single(edge => edge.Metadata["topology.id"] == "app link").SourceNodeId == "app-server" && friendlyScene.Edges.Single(edge => edge.Metadata["topology.id"] == "app link").TargetNodeId == "sql-db", "Topology graph bridge should rewrite edge references to normalized node ids.");
    }
}

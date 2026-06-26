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

        var noClusterScene = topology.ToGraphScene(options => options.IncludeGroupsAsClusters = false);
        noClusterScene.Validate();
        Assert(noClusterScene.Clusters.Count == 0 && noClusterScene.Nodes.All(node => string.IsNullOrWhiteSpace(node.ClusterId)), "Topology graph bridge should not assign dangling cluster ids when group cluster rendering is disabled.");

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
        Assert(anchoredHtml.Contains("data-node-id=\"anchor\"", StringComparison.Ordinal) && anchoredHtml.Contains("cfx-graph-hidden", StringComparison.Ordinal) && anchoredHtml.Contains("data-edge-source-arrow=\"true\"", StringComparison.Ordinal), "Graph explorer output should keep hidden topology anchors available for routing without drawing visible node marks.");
    }
}

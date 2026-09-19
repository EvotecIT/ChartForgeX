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
            .AddEdge("cdn-api", "cdn", "api", "routes", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("api-db", "api", "db", "queries", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, secondaryLabel: "32 ms");
        topology.WithNodeColor("api", "#0F766E").WithNodeBackground("api", "#CCFBF1").WithEdgeColor("cdn-api", "#7C3AED").WithEdgeEmphasis("cdn-api", TopologyEdgeEmphasis.Strong);
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
        Assert(scene.Edges[1].Shape == GraphEdgeShape.Polyline && scene.Edges[1].RoutePoints.Count >= 4, "Topology graph bridge should preserve orthogonal route geometry as reusable graph route points.");
        Assert(Math.Abs(scene.Edges[1].RoutePoints[0].X - (topology.Nodes[0].X + topology.Nodes[0].Width)) < 0.01, "Topology graph bridge should align prepared route endpoints to the visible card boundary.");
        Assert(scene.Nodes[0].Style.BackgroundColor == "#CCFBF1" && scene.Nodes[0].Style.BorderColor == "#0F766E" && scene.Nodes[1].Style.BorderColor == "#F97316" && scene.Edges[0].Style.Color == "#7C3AED" && scene.Edges[0].Style.Width.HasValue, "Topology graph bridge should map explicit, status-derived, and emphasis topology styles into reusable GraphScene styling, not only metadata.");
        Assert(scene.Edges[1].Metadata["topology.metric.transport"] == "tcp" && scene.Edges[1].Metadata["topology.secondaryLabel"] == "32 ms" && scene.Edges[1].Metadata["topology.routePointCount"] == scene.Edges[1].RoutePoints.Count.ToString(), "Topology graph edges should carry topology metrics, secondary labels, and route diagnostics.");
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
        Assert(html.Contains("data-node-background-color=\"#CCFBF1\"", StringComparison.Ordinal) && html.Contains("data-node-border-color=\"#0F766E\"", StringComparison.Ordinal) && html.Contains("--cfx-node-stroke:#0F766E", StringComparison.Ordinal) && html.Contains("data-edge-color=\"#7C3AED\"", StringComparison.Ordinal) && html.Contains("data-edge-width=", StringComparison.Ordinal) && html.Contains("data-edge-route-points=", StringComparison.Ordinal), "Topology graph explorer output should serialize topology style and route hints for SVG, Canvas, PNG, and export paths without hiding selected-node feedback.");

        var noClusterScene = topology.ToGraphScene(options => options.IncludeGroupsAsClusters = false);
        noClusterScene.Validate();
        Assert(noClusterScene.Clusters.Count == 0 && noClusterScene.GetEffectiveClusters().Count == 0 && noClusterScene.Nodes.All(node => string.IsNullOrWhiteSpace(node.ClusterId)), "Topology graph bridge should not assign dangling cluster ids or derive replacement clusters when group cluster rendering is disabled.");

        var leanManipulation = topology.ToGraphScene(options => {
            options.UseSuperTopologyDefaults = false;
            options.EnableManipulation = true;
        });
        leanManipulation.Validate();
        Assert(leanManipulation.Options.HasFeature(GraphSceneFeatures.IncrementalUpdates) && leanManipulation.Options.HasFeature(GraphSceneFeatures.Manipulation) && leanManipulation.Options.Manipulation.CanAddNodes && leanManipulation.Options.Manipulation.CanEditEdges && leanManipulation.Options.Manipulation.CanPersistPositions, "Topology graph bridge should honor valid opt-in manipulation even when large-topology defaults are disabled.");

        var anchored = TopologyChart.Create()
            .WithId("hidden-anchor")
            .AddNode("source", "Source", 20, 20, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("anchor", "Anchor", 140, 20, TopologyNodeKind.Network, TopologyHealthStatus.Unknown)
            .AddNode("target", "Target", 260, 20, TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
            .AddEdge("source-anchor", "source", "anchor", "route", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy)
            .AddEdge("anchor-target", "anchor", "target", "route", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional);
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
        Assert(!dashedScene.Edges.Single(edge => edge.Id == "queue-api").Dashed && dashedScene.Edges.Single(edge => edge.Id == "db-queue").Label == "dotted / queue 7 / 3m ago", "Topology graph bridge should keep muted auto-dash edges solid while preserving secondary and tertiary edge facts by default.");
        var compactLabels = dashedTopology.ToGraphScene(options => options.IncludeEdgeDetailInLabels = false);
        Assert(compactLabels.Edges.Single(edge => edge.Id == "db-queue").Label == "dotted", "Topology graph consumers should be able to opt into compact primary-only edge labels when their surface keeps detail in an inspector.");
        var dashedHtml = dashedTopology.ToGraphExplorerHtmlFragment();
        Assert(dashedHtml.Contains("data-edge-dash-pattern=\"8 5\"", StringComparison.Ordinal) && dashedHtml.Contains("data-edge-dash-pattern=\"2 5\"", StringComparison.Ordinal) && dashedHtml.Contains("stroke-dasharray:2 5", StringComparison.Ordinal) && dashedHtml.Contains("dashPattern: dashPattern(attr(el, 'data-edge-dash-pattern'), [8, 6])", StringComparison.Ordinal), "Graph explorer output should carry topology dash patterns into SVG and Canvas/PNG rendering state.");

        dashedTopology.WithEdgeEmphasis("db-queue", TopologyEdgeEmphasis.Subtle);
        var subtleScene = dashedTopology.ToGraphScene();
        Assert(subtleScene.Edges.Single(edge => edge.Id == "db-queue").Style.Width == 1.05 && subtleScene.Edges.Single(edge => edge.Id == "db-queue").Weight == 0.7, "Topology graph bridge should keep subtle topology edges visually thinner than normal graph edges while preserving low runtime force weight.");

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
            .AddAutoNode("auto", "Auto", TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddAutoNode("free-target", "Free target", TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
            .AddEdge("free-route", "auto", "free-target", "free", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal);
        var freeAutoOriginScene = freeAutoOrigin.ToGraphScene();
        Assert(!freeAutoOriginScene.Nodes.Single(node => node.Id == "auto").Fixed && !freeAutoOriginScene.Nodes.Single(node => node.Id == "auto").HasExplicitPosition, "Topology graph bridge should not pin untouched AddAutoNode placeholder coordinates at the origin.");
        Assert(freeAutoOriginScene.Edges.Single(edge => edge.Id == "free-route").RoutePoints.Count == 0 && freeAutoOriginScene.Edges.Single(edge => edge.Id == "free-route").Shape == GraphEdgeShape.Polyline, "Topology graph bridge should not emit prepared route points when generated node positions will use a different coordinate space.");

        var curvedWaypoint = TopologyChart.Create()
            .WithLayout(TopologyLayoutMode.Manual)
            .AddNode("left", "Left", 20, 40, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("right", "Right", 260, 40, TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
            .AddEdge("curved-route", "left", "right", "manual", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved)
            .WithEdgeWaypoints("curved-route", new ChartForgeX.Primitives.ChartPoint(130, 20), new ChartForgeX.Primitives.ChartPoint(170, 120));
        var curvedWaypointEdge = curvedWaypoint.ToGraphScene().Edges.Single(edge => edge.Id == "curved-route");
        Assert(curvedWaypointEdge.Shape == GraphEdgeShape.Polyline && curvedWaypointEdge.RoutePoints.Count >= 4, "Topology graph bridge should preserve caller-specified manual waypoints even when the edge routing remains curved.");

        var friendlyIds = TopologyChart.Create()
            .WithId("app map")
            .AddGroup("core services", "Core Services", 0, 0, 240, 160, TopologyHealthStatus.Healthy)
            .AddNode("app server", "App Server", 40, 50, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, groupId: "core services")
            .AddNode("sql db", "SQL DB", 160, 50, TopologyNodeKind.Database, TopologyHealthStatus.Warning, groupId: "core services")
            .AddEdge("app link", "app server", "sql db", "queries", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, VisualLinkDirection.Forward);
        var friendlyScene = friendlyIds.ToGraphScene();
        friendlyScene.Validate();
        Assert(friendlyScene.Id == "app-map" && friendlyScene.Nodes.Any(node => node.Id == "app-server" && node.Metadata["topology.id"] == "app server") && friendlyScene.Clusters.Any(cluster => cluster.Id == "core-services" && cluster.Metadata["topology.id"] == "core services"), "Topology graph bridge should normalize friendly topology ids while preserving original ids in metadata.");
        Assert(friendlyScene.Edges.Single(edge => edge.Metadata["topology.id"] == "app link").SourceNodeId == "app-server" && friendlyScene.Edges.Single(edge => edge.Metadata["topology.id"] == "app link").TargetNodeId == "sql-db", "Topology graph bridge should rewrite edge references to normalized node ids.");

        var emptyGroupScene = TopologyChart.Create().AddGroup("empty", "Empty", 0, 0, 120, 80, TopologyHealthStatus.Unknown).ToGraphScene();
        Assert(emptyGroupScene.Clusters.Count == 0, "Topology graph bridge should skip empty topology groups instead of rendering unrelated memberless cluster badges.");
        TopologyGraphExplorerScalesFromCardsToClusteredOverviews();
    }

    private static void TopologyGraphExplorerScalesFromCardsToClusteredOverviews() {
        var small = TopologyChart.Create()
            .WithId("relationship-overview")
            .WithTitle("Relationship overview")
            .WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddAutoIconNode("a", "alpha.example", "microsoft-ad:domain", TopologyHealthStatus.Healthy, subtitle: "Domain", width: 180, height: 72)
            .AddAutoIconNode("b", "beta.example", "microsoft-ad:domain", TopologyHealthStatus.Warning, subtitle: "Domain", width: 180, height: 72)
            .AddAutoNode("c", "gamma.example", TopologyNodeKind.Namespace, TopologyHealthStatus.Critical, subtitle: "Domain", width: 180, height: 72, symbol: "AD")
            .AddAutoNode("d", "KERBEROS.MICROSOFTONLINE.COM😀", TopologyNodeKind.Namespace, TopologyHealthStatus.Unknown, subtitle: "Domain", width: 180, height: 72, symbol: "AD")
            .AddEdge("a-b", "a", "b", "Forest · Bidirectional", TopologyEdgeKind.Trust, TopologyHealthStatus.Warning, VisualLinkDirection.Bidirectional)
            .AddEdge("a-c", "a", "c", "External · Outbound", TopologyEdgeKind.Trust, TopologyHealthStatus.Critical, VisualLinkDirection.Forward);
        small.WithNodeDisplay("a", TopologyNodeDisplayMode.Card)
            .WithNodeDisplay("b", TopologyNodeDisplayMode.Card);

        var smallScene = small.ToGraphScene();
        Assert(smallScene.Nodes.All(node => node.HasExplicitPosition && !node.Fixed), "Prepared topology layouts should seed stable opening coordinates while keeping interactive nodes movable.");
        Assert(smallScene.Nodes.All(node => node.Style.LabelColor == null), "Card topology titles should follow the active explorer theme instead of pinning an accent color that may become unreadable after a theme change.");
        Assert(!smallScene.Options.Physics.Stabilization.Enabled, "Prepared topology layouts should remain stable on load until the user explicitly starts browser stabilization.");
        Assert(small.ToGraphScene(options => options.StabilizePreparedLayoutOnLoad = true).Options.Physics.Stabilization.Enabled, "Topology callers should be able to opt into immediate browser stabilization for prepared layouts.");
        Assert(smallScene.Options.LevelOfDetail.DetailScaleThreshold <= 0.72, "Small relationship maps should preserve card subtitles at fitted overview scales instead of hiding the context that explains each object.");
        var radialRoot = smallScene.Nodes.Single(node => node.Id == "a");
        Assert(smallScene.Nodes.Where(node => node.Id != radialRoot.Id).All(node => !CardBoundsOverlap(radialRoot, node, 12)), "Relationship-radial layouts should use card dimensions to leave inspectable space between the root and first-hop relationships.");
        Assert(smallScene.Nodes.All(node => node.Shape == GraphNodeShape.Box && node.Size > 60 && node.Metadata["topology.card"] == "true"), "Card topology nodes should preserve their requested width and explicit card semantics instead of collapsing to tiny blank rectangles.");
        Assert(smallScene.Metadata["topology.preparedLayoutSeeded"] == "true", "Topology graph projection should expose when deterministic source layout seeded the explorer.");
        var embedded = small.ToGraphExplorerHtmlPage(
            configureHtml: options => {
                options.IncludeHeader = false;
                options.IncludeSearch = false;
                options.IncludeFilters = false;
                options.FillAvailableHeight = true;
            });
        Assert(embedded.Contains("cfx-graph-shell-embedded", StringComparison.Ordinal) && embedded.Contains("cfx-graph-fill-available", StringComparison.Ordinal), "Embedded graph pages should fill the host viewport without internal page padding or scrollbars.");
        Assert(!embedded.Contains("data-cfx-role=\"graph-header\"", StringComparison.Ordinal) && !embedded.Contains("data-cfx-role=\"graph-search\"", StringComparison.Ordinal), "Embedded graph pages should allow the host to own the title, search, and filters without duplicate controls.");
        Assert(embedded.Contains("height=\"72\" rx=\"10\"", StringComparison.Ordinal) && embedded.Contains("data-node-card=\"true\"", StringComparison.Ordinal) && embedded.Contains("cfx-graph-node-card-label", StringComparison.Ordinal), "Explicit topology cards should render as readable cards with internal labels across the explorer surface.");
        Assert(embedded.Contains("data-cfx-full-label=\"KERBEROS.MICROSOFTONLINE.COM&#128512;\"", StringComparison.Ordinal) && embedded.Contains("…", StringComparison.Ordinal) && embedded.Contains("COM&#128512;</text>", StringComparison.Ordinal) && !embedded.Contains("�", StringComparison.Ordinal), "Topology cards should retain the full label as metadata while fitting a Unicode-safe visible label inside the card.");
        Assert(embedded.Contains("data-edge-source-arrow=\"true\"", StringComparison.Ordinal) && embedded.Contains("data-edge-target-arrow=\"true\"", StringComparison.Ordinal), "Bidirectional relationships should preserve both arrowheads in the explorer contract.");
        var externalLabel = ExtractGraphEdgeLabelPoint(embedded, "a-c");
        Assert(!EdgeLabelBoundsOverlapCard(externalLabel, "External · Outbound", smallScene.Nodes.Single(node => node.Id == "a")) && !EdgeLabelBoundsOverlapCard(externalLabel, "External · Outbound", smallScene.Nodes.Single(node => node.Id == "c")), "Relationship labels should move away from endpoint cards when their readable bounds would overlap a node.");
        Assert(embedded.Contains("graphReadableNodeColors", StringComparison.Ordinal) && embedded.Contains("--cfx-node-label-halo", StringComparison.Ordinal) && embedded.Contains("--cfx-node-secondary-adaptive", StringComparison.Ordinal), "Node card typography should derive readable primary, secondary, and halo colors from each node surface across light and dark themes.");
        Assert(embedded.Contains("graphOverviewMinimumItems", StringComparison.Ordinal) && embedded.Contains("cfx-graph-overview-unneeded", StringComparison.Ordinal), "Compact relationship maps should suppress an unnecessary overview overlay while dense expanded graphs retain it.");

        var medium = BuildScaleTopology("medium", 40).ToGraphScene();
        var large = BuildScaleTopology("large", 120).ToGraphScene();
        Assert(medium.Nodes.Count == 40 && medium.Nodes.All(node => node.HasExplicitPosition), "Medium topologies should open from deterministic prepared coordinates.");
        Assert(medium.Options.Cluster.CollapseOnLoad && medium.GetEffectiveClusters().Count == 5 && medium.GetEffectiveClusters().All(cluster => cluster.Collapsed), "Medium grouped topologies should open as readable site summaries that users can expand for controller details.");
        Assert(medium.Options.LevelOfDetail.HideEdgeLabelsThreshold == 32, "Dense topology overviews should hide repetitive relationship labels while preserving them for focus, selection, and inspection.");
        var mediumHtml = BuildScaleTopology("medium-html", 40).ToGraphExplorerHtmlFragment();
        Assert(mediumHtml.Contains("data-cfx-status=\"healthy\"", StringComparison.Ordinal) && mediumHtml.Contains("graphClusterColors", StringComparison.Ordinal) && mediumHtml.Contains("graphPatchClusterStatus", StringComparison.Ordinal), "Collapsed topology group summaries should carry health through SVG, Canvas, WebGL, overview, export, and runtime patch rendering.");
        Assert(mediumHtml.Contains("data-cluster-node-count=\"8\"", StringComparison.Ordinal) && mediumHtml.Contains(">8 objects</text>", StringComparison.Ordinal) && mediumHtml.Contains("graphItemAccessible(root, item, collapsedNodeIds)", StringComparison.Ordinal) && mediumHtml.Contains("!collapsed.has(attr(item, 'data-source-node-id'))", StringComparison.Ordinal), "Collapsed topology groups should show their object counts and keep summarized member relationships out of the keyboard and screen-reader navigation surface until expanded.");
        Assert(mediumHtml.Contains("role === 'graph-cluster' && attr(item, 'data-cluster-collapsed') === 'true'", StringComparison.Ordinal) && mediumHtml.Contains("toggleGraphCluster(root, attr(item, 'data-cluster-id'))", StringComparison.Ordinal) && mediumHtml.Contains("reheatPhysics(root, 'cluster-drill', { rebuild: true, fit: true })", StringComparison.Ordinal) && mediumHtml.Contains("fitViewport(root);", StringComparison.Ordinal), "Cluster summaries should support keyboard drill-down, resolve visible-card collisions, and refit the investigation scope after stabilization.");
        Assert(large.Nodes.Count == 120 && large.Options.LevelOfDetail.ClusterNodeThreshold <= 120 && large.Options.LevelOfDetail.HideEdgeLabelsThreshold <= 120, "Large topologies should activate reusable clustering and semantic label reduction at 100-plus objects.");
        Assert(large.Options.Cluster.CollapseOnLoad && large.GetEffectiveClusters().Count == 5, "Large grouped topologies should start from aggregate summaries instead of a wall of unlabeled cards.");
        Assert(large.Options.LevelOfDetail.CanvasPreferredNodeThreshold > large.Nodes.Count, "Hundred-node topology views should retain rich SVG interaction until the shared Canvas threshold is reached.");

        var genericBox = GraphScene.Create("generic-box", "Generic box");
        genericBox.Nodes.Add(new GraphSceneNode { Id = "box", Label = "Large generic box", Shape = GraphNodeShape.Box, Size = 60 });
        var genericBoxHtml = genericBox.ToGraphExplorerHtmlFragment();
        Assert(genericBoxHtml.Contains("height=\"126\" rx=\"6\"", StringComparison.Ordinal) && genericBoxHtml.Contains("data-node-card=\"false\"", StringComparison.Ordinal), "Generic large box nodes should retain their public half-size geometry instead of implicitly becoming topology cards.");
    }

    private static TopologyChart BuildScaleTopology(string id, int nodeCount) {
        var chart = TopologyChart.Create().WithId(id).WithLayout(TopologyLayoutMode.ForceDirected);
        for (var site = 0; site < 5; site++) chart.AddGroup("site-" + site, "Site " + site, 0, 0, 320, 220, TopologyHealthStatus.Healthy);
        for (var index = 0; index < nodeCount; index++) {
            chart.AddAutoNode("node-" + index, "Node " + index, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, groupId: "site-" + index % 5, subtitle: "Site " + index % 5, width: 164, height: 72);
            if (index > 0) chart.AddEdge("edge-" + index, "node-" + (index - 1), "node-" + index, "Replication", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward);
        }
        return chart;
    }

    private static bool CardBoundsOverlap(GraphSceneNode first, GraphSceneNode second, double gap) {
        var firstHalfWidth = first.Size * 1.45;
        var secondHalfWidth = second.Size * 1.45;
        var firstHalfHeight = Math.Min(first.Size * 1.05, 36);
        var secondHalfHeight = Math.Min(second.Size * 1.05, 36);
        return Math.Abs(first.X - second.X) < firstHalfWidth + secondHalfWidth + gap
            && Math.Abs(first.Y - second.Y) < firstHalfHeight + secondHalfHeight + gap;
    }

    private static bool EdgeLabelBoundsOverlapCard((double X, double Y) labelPoint, string label, GraphSceneNode node) {
        var labelHalfWidth = Math.Max(14, label.Length * 6.1 / 2 + 4);
        var nodeHalfWidth = node.Size * 1.45;
        var nodeHalfHeight = Math.Min(node.Size * 1.05, 36);
        return Math.Abs(labelPoint.X - node.X) < labelHalfWidth + nodeHalfWidth + 6
            && Math.Abs(labelPoint.Y - 4 - node.Y) < 10 + nodeHalfHeight + 6;
    }
}

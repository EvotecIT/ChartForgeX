using System;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphSceneClusterPolicyRejectsInvalidOptions() {
        var unknownClusterMode = GraphScene.Create("bad-cluster-mode", "Bad cluster mode").AddNode("node", "Node");
        unknownClusterMode.Options.Cluster.Mode = (GraphClusterMode)999;
        AssertThrows<InvalidOperationException>(() => unknownClusterMode.Validate(), "Graph scenes should reject unknown cluster policy values before adapters serialize inconsistent cluster behavior.");

        var invalidClusterSizing = GraphScene.Create("bad-cluster-size", "Bad cluster size").AddNode("node", "Node");
        invalidClusterSizing.Options.Cluster.MinimumClusterSize = 5;
        invalidClusterSizing.Options.Cluster.TargetClusterSize = 4;
        AssertThrows<InvalidOperationException>(() => invalidClusterSizing.Validate(), "Graph scenes should reject cluster sizing policies where the target is smaller than the minimum.");
    }

    private static void GraphSceneClusterPolicyDerivesGroupClusters() {
        var scene = GraphScene.Create("group-derived", "Group-derived clusters")
            .AddNode("api", "API", node => {
                node.GroupId = "core";
                node.Kind = "service";
            })
            .AddNode("db", "Database", node => {
                node.GroupId = "core";
                node.Kind = "database";
            })
            .AddNode("cdn", "CDN", node => {
                node.GroupId = "edge";
                node.Kind = "network";
            })
            .AddNode("waf", "WAF", node => {
                node.GroupId = "edge";
                node.Kind = "network";
            })
            .AddEdge("api-db", "api", "db", "queries")
            .AddEdge("cdn-api", "cdn", "api", "routes");
        scene.Options.Cluster.Mode = GraphClusterMode.ByGroup;
        scene.Options.Cluster.CollapseOnLoad = true;
        scene.Options.Enable(GraphSceneFeatures.Export);

        scene.Validate();
        var clusters = scene.GetEffectiveClusters();
        Assert(scene.Clusters.Count == 0 && clusters.Count == 2, "Group-derived clustering should not require callers to duplicate node GroupId values into explicit cluster definitions.");
        Assert(clusters[0].Id == "group-core" && clusters[0].NodeIds.Count == 2 && clusters[0].Collapsed, "Group-derived clusters should expose stable ids, members, and collapse policy.");

        var html = scene.ToGraphExplorerHtmlFragment();
        Assert(html.Contains("data-cfx-graph-cluster-count=\"2\"", StringComparison.Ordinal), "Graph explorer output should count effective derived clusters.");
        Assert(html.Contains("data-cluster-id=\"group-core\"", StringComparison.Ordinal) && html.Contains("data-cluster-node-ids=\"api,db\"", StringComparison.Ordinal), "Graph explorer output should render group-derived cluster summaries.");
        Assert(html.Contains("data-node-id=\"api\"", StringComparison.Ordinal) && html.Contains("data-node-cluster=\"group-core\"", StringComparison.Ordinal), "Graph explorer output should map grouped nodes to their derived cluster id.");
        Assert(html.Contains("data-cfx-graph-action=\"clusters\"", StringComparison.Ordinal), "Group-derived clusters should enable the normal cluster control surface.");

        var explicitCollapse = GraphScene.Create("explicit-collapse", "Explicit collapse")
            .AddNode("root", "Root")
            .AddNode("child", "Child")
            .AddCluster("declared", "Declared", new[] { "root", "child" });
        explicitCollapse.Options.Cluster.CollapseOnLoad = true;
        explicitCollapse.Validate();
        var explicitCollapseHtml = explicitCollapse.ToGraphExplorerHtmlFragment();
        Assert(explicitCollapse.GetEffectiveClusters()[0].Collapsed && explicitCollapseHtml.Contains("data-cluster-id=\"declared\"", StringComparison.Ordinal) && explicitCollapseHtml.Contains("data-cluster-collapsed=\"true\"", StringComparison.Ordinal), "Cluster collapse defaults should apply to explicit clusters as well as derived group clusters.");

        var collision = GraphScene.Create("cluster-collision", "Cluster collision")
            .AddNode("explicit", "Explicit", node => node.ClusterId = "group-core")
            .AddNode("api", "API", node => node.GroupId = "core")
            .AddNode("db", "Database", node => node.GroupId = "core")
            .AddCluster("group-core", "Declared core", new[] { "explicit" });
        collision.Options.Cluster.Mode = GraphClusterMode.ByGroup;
        collision.Validate();
        var collisionClusters = collision.GetEffectiveClusters();
        Assert(collisionClusters.Count == 2 && collisionClusters.Any(cluster => cluster.Id == "group-core") && collisionClusters.Any(cluster => cluster.Id == "group-core-2"), "Derived group clusters should avoid colliding with declared cluster ids.");

        var adaptiveOnly = GraphScene.Create("adaptive-only", "Adaptive-only clusters")
            .AddNode("api", "API", node => node.GroupId = "core")
            .AddNode("db", "Database", node => node.GroupId = "core");
        adaptiveOnly.Options.Cluster.Mode = GraphClusterMode.Adaptive;
        adaptiveOnly.Options.Cluster.Adaptive = true;
        adaptiveOnly.Validate();
        Assert(adaptiveOnly.GetEffectiveClusters().Count == 0, "Adaptive-only clustering should leave group-derived clusters to runtime graph-structure adapters instead of materializing fixed group summaries.");
    }
}

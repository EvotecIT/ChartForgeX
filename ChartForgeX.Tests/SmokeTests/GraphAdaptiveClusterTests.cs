using System;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphAdaptiveClustersMaterializeDeterministically() {
        var scene = GraphScene.Create("adaptive", "Adaptive communities");
        for (var index = 0; index < 18; index++) scene.AddNode("node-" + index, "Node " + index);
        for (var index = 0; index < 17; index++) scene.AddEdge("edge-" + index, "node-" + index, "node-" + (index + 1));
        scene.Options.Cluster.Mode = GraphClusterMode.Adaptive;
        scene.Options.Cluster.Adaptive = true;
        scene.Options.Cluster.MinimumClusterSize = 2;
        scene.Options.Cluster.TargetClusterSize = 4;
        scene.Options.Cluster.CollapseOnLoad = true;
        scene.Options.LevelOfDetail.ClusterNodeThreshold = 10;
        scene.Validate();

        var first = scene.GetEffectiveClusters();
        var second = scene.GetEffectiveClusters();
        Assert(first.Count >= 4 && first.All(cluster => cluster.Collapsed && cluster.NodeIds.Count <= 4), "Adaptive clustering should materialize bounded collapsed communities once the scene crosses its clustering threshold.");
        Assert(first.Select(cluster => cluster.Id + ":" + string.Join(",", cluster.NodeIds)).SequenceEqual(second.Select(cluster => cluster.Id + ":" + string.Join(",", cluster.NodeIds))), "Adaptive cluster ids and membership should remain deterministic across repeated renders.");
        Assert(first.All(cluster => cluster.Metadata["cluster.source"] == "adaptive-structure"), "Adaptive summaries should identify their reusable structural source in metadata.");

        var html = scene.ToGraphExplorerHtmlFragment();
        Assert(html.Contains("data-cluster-id=\"adaptive-1\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-cluster-count=\"", StringComparison.Ordinal), "The HTML adapter should render materialized adaptive communities as normal per-cluster summaries.");
    }
}

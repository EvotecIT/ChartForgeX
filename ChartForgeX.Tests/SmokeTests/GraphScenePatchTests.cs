using System;
using System.Linq;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphScenePatchesApplyAtomically() {
        var scene = GraphScene.Create("patch", "Patch")
            .AddNode("api", "API")
            .AddNode("db", "Database")
            .AddEdge("api-db", "api", "db")
            .AddCluster("core", "Core", new[] { "api", "db" });
        scene.Validate();

        var patch = new GraphScenePatch();
        patch.RemoveNodeIds.Add("db");
        patch.UpsertNodes.Add(new GraphSceneNode { Id = "queue", Label = "Queue", ParentId = "api", Status = "warning" });
        patch.UpsertEdges.Add(new GraphSceneEdge { Id = "api-queue", SourceNodeId = "api", TargetNodeId = "queue", Directed = true });
        var result = scene.ApplyPatch(patch);

        Assert(result.NodeCount == 2 && result.EdgeCount == 1 && result.ClusterCount == 1, "Atomic graph patches should report the committed graph document size.");
        Assert(scene.Nodes.Any(node => node.Id == "queue" && node.ParentId == "api") && scene.Edges.Single().Id == "api-queue", "Graph patches should add parent-aware nodes and incident edges in one validated operation.");
        Assert(scene.Clusters.Single().NodeIds.SequenceEqual(new[] { "api" }), "Removing a node should remove stale cluster membership when incident cleanup is enabled.");

        var invalid = new GraphScenePatch();
        invalid.UpsertEdges.Add(new GraphSceneEdge { Id = "broken", SourceNodeId = "api", TargetNodeId = "missing" });
        AssertThrows<InvalidOperationException>(() => scene.ApplyPatch(invalid), "Invalid graph patches should fail validation.");
        Assert(scene.Nodes.Count == 2 && scene.Edges.Count == 1 && scene.Edges[0].Id == "api-queue", "A failed graph patch should restore the complete previous scene instead of leaving partial mutations.");

        var dangling = new GraphScenePatch { RemoveIncidentReferences = false };
        dangling.RemoveNodeIds.Add("queue");
        AssertThrows<InvalidOperationException>(() => scene.ApplyPatch(dangling), "Graph patches should reject node removal that leaves a surviving edge with a missing endpoint.");
        Assert(scene.Nodes.Count == 2 && scene.Edges.Single().Id == "api-queue", "Rejected dangling-edge patches should preserve the original graph document.");

        var clustered = GraphScene.Create("patch-clusters", "Patch cluster moves")
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddNode("c", "C")
            .AddCluster("left", "Left", new[] { "a", "b" })
            .AddCluster("right", "Right", new[] { "c" });
        clustered.Validate();
        var move = new GraphScenePatch();
        move.UpsertNodes.Add(new GraphSceneNode { Id = "a", Label = "A", ClusterId = "right" });
        clustered.ApplyPatch(move);
        Assert(clustered.Clusters.Single(cluster => cluster.Id == "left").NodeIds.SequenceEqual(new[] { "b" }), "Moving a node with a typed patch should remove stale declared membership from its previous cluster.");
        Assert(clustered.Clusters.Single(cluster => cluster.Id == "right").NodeIds.OrderBy(id => id).SequenceEqual(new[] { "a", "c" }), "Moving a node with a typed patch should add declared membership to its new cluster.");

        var detach = new GraphScenePatch();
        detach.UpsertNodes.Add(new GraphSceneNode { Id = "a", Label = "A" });
        clustered.ApplyPatch(detach);
        Assert(clustered.Clusters.All(cluster => !cluster.NodeIds.Contains("a")), "Replacing a patched node without a cluster id should detach it from declared cluster membership.");

        var declared = GraphScene.Create("patch-declared-membership", "Patch declared membership");
        var declaredPatch = new GraphScenePatch();
        declaredPatch.UpsertNodes.Add(new GraphSceneNode { Id = "new-node", Label = "New node" });
        var declaredCluster = new GraphSceneCluster { Id = "new-cluster", Label = "New cluster" };
        declaredCluster.NodeIds.Add("new-node");
        declaredPatch.UpsertClusters.Add(declaredCluster);
        declared.ApplyPatch(declaredPatch);
        Assert(declared.Clusters.Single().NodeIds.SequenceEqual(new[] { "new-node" }), "Atomic patches should preserve membership declared by a cluster upsert when the same patch adds its nodes without duplicating ClusterId values.");

        var nodeOwned = GraphScene.Create("patch-node-owned-cluster", "Patch node-owned cluster")
            .AddNode("member", "Member", node => node.ClusterId = "owned")
            .AddCluster("owned", "Owned", Array.Empty<string>())
            .AddCluster("owned-child", "Owned child", Array.Empty<string>(), cluster => cluster.ParentClusterId = "owned");
        nodeOwned.Validate();
        var failedClusterRemoval = new GraphScenePatch();
        failedClusterRemoval.RemoveClusterIds.Add("owned");
        failedClusterRemoval.UpsertEdges.Add(new GraphSceneEdge { Id = "broken-removal", SourceNodeId = "member", TargetNodeId = "missing" });
        AssertThrows<InvalidOperationException>(() => nodeOwned.ApplyPatch(failedClusterRemoval), "A cluster removal should remain atomic when another change makes the patch invalid.");
        Assert(nodeOwned.Clusters.Count == 2 && nodeOwned.Clusters.Single(cluster => cluster.Id == "owned-child").ParentClusterId == "owned" && nodeOwned.Nodes.Single().ClusterId == "owned", "A rejected cluster removal should restore node-side and nested-cluster references with the removed cluster.");

        var removeNodeOwnedCluster = new GraphScenePatch();
        removeNodeOwnedCluster.RemoveClusterIds.Add("owned");
        var nodeOwnedResult = nodeOwned.ApplyPatch(removeNodeOwnedCluster);
        Assert(nodeOwnedResult.ClusterCount == 1 && nodeOwned.Nodes.Single().ClusterId == null && nodeOwned.Clusters.Single().ParentClusterId == null, "Removing a cluster should clear node-side ClusterId and child ParentClusterId references within the same atomic patch.");
    }
}

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
    }
}

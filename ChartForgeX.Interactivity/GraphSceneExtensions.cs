using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Provides fluent helpers for building graph scenes without exposing adapter-specific DTOs.
/// </summary>
public static class GraphSceneExtensions {
    /// <summary>
    /// Adds a node to the graph scene.
    /// </summary>
    /// <param name="scene">The graph scene.</param>
    /// <param name="id">Stable node id.</param>
    /// <param name="label">Node label.</param>
    /// <param name="configure">Optional node configuration callback.</param>
    /// <returns>The current graph scene.</returns>
    public static GraphScene AddNode(this GraphScene scene, string id, string label, Action<GraphSceneNode>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var node = new GraphSceneNode { Id = id, Label = label };
        configure?.Invoke(node);
        scene.Nodes.Add(node);
        return scene;
    }

    /// <summary>
    /// Adds an edge to the graph scene.
    /// </summary>
    /// <param name="scene">The graph scene.</param>
    /// <param name="id">Stable edge id.</param>
    /// <param name="sourceNodeId">Source node id.</param>
    /// <param name="targetNodeId">Target node id.</param>
    /// <param name="label">Optional edge label.</param>
    /// <param name="configure">Optional edge configuration callback.</param>
    /// <returns>The current graph scene.</returns>
    public static GraphScene AddEdge(this GraphScene scene, string id, string sourceNodeId, string targetNodeId, string? label = null, Action<GraphSceneEdge>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var edge = new GraphSceneEdge { Id = id, SourceNodeId = sourceNodeId, TargetNodeId = targetNodeId, Label = label };
        configure?.Invoke(edge);
        scene.Edges.Add(edge);
        return scene;
    }

    /// <summary>
    /// Adds a cluster to the graph scene.
    /// </summary>
    /// <param name="scene">The graph scene.</param>
    /// <param name="id">Stable cluster id.</param>
    /// <param name="label">Cluster label.</param>
    /// <param name="nodeIds">Node ids contained by the cluster.</param>
    /// <param name="configure">Optional cluster configuration callback.</param>
    /// <returns>The current graph scene.</returns>
    public static GraphScene AddCluster(this GraphScene scene, string id, string label, string[] nodeIds, Action<GraphSceneCluster>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (nodeIds == null) throw new ArgumentNullException(nameof(nodeIds));
        var cluster = new GraphSceneCluster { Id = id, Label = label };
        cluster.NodeIds.AddRange(nodeIds);
        configure?.Invoke(cluster);
        scene.Clusters.Add(cluster);
        return scene;
    }
}

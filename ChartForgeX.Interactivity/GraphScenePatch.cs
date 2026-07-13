using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes an atomic set of graph document changes that hosts and interactive adapters can exchange.
/// </summary>
public sealed class GraphScenePatch {
    /// <summary>Gets nodes to add or replace by stable id.</summary>
    public List<GraphSceneNode> UpsertNodes { get; } = new();

    /// <summary>Gets edges to add or replace by stable id.</summary>
    public List<GraphSceneEdge> UpsertEdges { get; } = new();

    /// <summary>Gets clusters to add or replace by stable id.</summary>
    public List<GraphSceneCluster> UpsertClusters { get; } = new();

    /// <summary>Gets node ids to remove.</summary>
    public List<string> RemoveNodeIds { get; } = new();

    /// <summary>Gets edge ids to remove.</summary>
    public List<string> RemoveEdgeIds { get; } = new();

    /// <summary>Gets cluster ids to remove.</summary>
    public List<string> RemoveClusterIds { get; } = new();

    /// <summary>Gets or sets whether removing nodes also removes incident edges and cluster membership.</summary>
    public bool RemoveIncidentReferences { get; set; } = true;
}

/// <summary>
/// Reports the graph document size after an atomic patch succeeds.
/// </summary>
public readonly struct GraphScenePatchResult {
    internal GraphScenePatchResult(int nodeCount, int edgeCount, int clusterCount) {
        NodeCount = nodeCount;
        EdgeCount = edgeCount;
        ClusterCount = clusterCount;
    }

    /// <summary>Gets the resulting node count.</summary>
    public int NodeCount { get; }

    /// <summary>Gets the resulting edge count.</summary>
    public int EdgeCount { get; }

    /// <summary>Gets the resulting cluster count.</summary>
    public int ClusterCount { get; }
}

/// <summary>
/// Applies typed incremental changes to graph scenes.
/// </summary>
public static class GraphScenePatchExtensions {
    /// <summary>
    /// Applies a patch atomically and validates the complete scene before committing it.
    /// </summary>
    /// <param name="scene">The graph document to update.</param>
    /// <param name="patch">The requested additions, replacements, and removals.</param>
    /// <returns>The resulting graph document counts.</returns>
    public static GraphScenePatchResult ApplyPatch(this GraphScene scene, GraphScenePatch patch) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (patch == null) throw new ArgumentNullException(nameof(patch));
        ValidatePatchIds(patch);

        var originalNodes = scene.Nodes.ToArray();
        var originalEdges = scene.Edges.ToArray();
        var originalClusters = scene.Clusters.ToArray();
        var nodes = originalNodes.ToList();
        var edges = originalEdges.ToList();
        var clusters = originalClusters.Select(CloneCluster).ToList();

        var removedNodeIds = TokenSet(patch.RemoveNodeIds);
        var removedEdgeIds = TokenSet(patch.RemoveEdgeIds);
        var removedClusterIds = TokenSet(patch.RemoveClusterIds);
        nodes.RemoveAll(node => removedNodeIds.Contains(node.Id));
        edges.RemoveAll(edge => removedEdgeIds.Contains(edge.Id));
        clusters.RemoveAll(cluster => removedClusterIds.Contains(cluster.Id));

        if (patch.RemoveIncidentReferences && removedNodeIds.Count > 0) {
            edges.RemoveAll(edge => removedNodeIds.Contains(edge.SourceNodeId) || removedNodeIds.Contains(edge.TargetNodeId));
            foreach (var cluster in clusters) cluster.NodeIds.RemoveAll(removedNodeIds.Contains);
        }

        Upsert(nodes, patch.UpsertNodes, node => node.Id);
        Upsert(clusters, patch.UpsertClusters.Select(CloneCluster), cluster => cluster.Id);
        Upsert(edges, patch.UpsertEdges, edge => edge.Id);
        DetachRemovedClusterReferences(nodes, clusters, removedClusterIds);
        SynchronizeClusterMembership(clusters, patch.UpsertNodes, patch.UpsertClusters);

        try {
            Replace(scene.Nodes, nodes);
            Replace(scene.Edges, edges);
            Replace(scene.Clusters, clusters);
            scene.Validate();
            return new GraphScenePatchResult(scene.Nodes.Count, scene.Edges.Count, scene.Clusters.Count);
        } catch {
            Replace(scene.Nodes, originalNodes);
            Replace(scene.Edges, originalEdges);
            Replace(scene.Clusters, originalClusters);
            throw;
        }
    }

    private static void ValidatePatchIds(GraphScenePatch patch) {
        ValidateUnique(patch.UpsertNodes.Select(node => node?.Id), "upsert node");
        ValidateUnique(patch.UpsertEdges.Select(edge => edge?.Id), "upsert edge");
        ValidateUnique(patch.UpsertClusters.Select(cluster => cluster?.Id), "upsert cluster");
        ValidateUnique(patch.RemoveNodeIds, "removed node");
        ValidateUnique(patch.RemoveEdgeIds, "removed edge");
        ValidateUnique(patch.RemoveClusterIds, "removed cluster");
    }

    private static void ValidateUnique(IEnumerable<string?> ids, string kind) {
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids) {
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Graph scene patch contains a blank " + kind + " id.");
            if (!unique.Add(id!)) throw new InvalidOperationException("Graph scene patch contains a duplicate " + kind + " id: " + id);
        }
    }

    private static HashSet<string> TokenSet(IEnumerable<string> values) {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values) {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Graph scene patch removal ids must not be blank.");
            result.Add(value);
        }

        return result;
    }

    private static void Upsert<T>(List<T> target, IEnumerable<T> updates, Func<T, string> id) {
        foreach (var update in updates) {
            if (update == null) throw new InvalidOperationException("Graph scene patch upserts must not contain null items.");
            var updateId = id(update);
            var index = target.FindIndex(item => string.Equals(id(item), updateId, StringComparison.Ordinal));
            if (index < 0) target.Add(update);
            else target[index] = update;
        }
    }

    private static GraphSceneCluster CloneCluster(GraphSceneCluster source) {
        var clone = new GraphSceneCluster {
            Id = source.Id,
            Label = source.Label,
            Kind = source.Kind,
            ParentClusterId = source.ParentClusterId,
            Collapsed = source.Collapsed
        };
        clone.NodeIds.AddRange(source.NodeIds);
        foreach (var pair in source.Metadata) clone.Metadata[pair.Key] = pair.Value;
        return clone;
    }

    private static void DetachRemovedClusterReferences(IList<GraphSceneNode> nodes, IReadOnlyCollection<GraphSceneCluster> clusters, ISet<string> removedClusterIds) {
        if (removedClusterIds.Count == 0) return;
        var survivingClusterIds = new HashSet<string>(clusters.Select(cluster => cluster.Id), StringComparer.Ordinal);
        for (var index = 0; index < nodes.Count; index++) {
            var node = nodes[index];
            if (string.IsNullOrWhiteSpace(node.ClusterId) || !removedClusterIds.Contains(node.ClusterId!) || survivingClusterIds.Contains(node.ClusterId!)) continue;
            nodes[index] = CloneNodeWithoutCluster(node);
        }
        foreach (var cluster in clusters) {
            if (!string.IsNullOrWhiteSpace(cluster.ParentClusterId) && removedClusterIds.Contains(cluster.ParentClusterId!) && !survivingClusterIds.Contains(cluster.ParentClusterId!)) cluster.ParentClusterId = null;
        }
    }

    private static GraphSceneNode CloneNodeWithoutCluster(GraphSceneNode source) {
        var clone = new GraphSceneNode {
            Id = source.Id,
            Label = source.Label,
            Kind = source.Kind,
            GroupId = source.GroupId,
            ParentId = source.ParentId,
            Status = source.Status,
            Shape = source.Shape,
            Level = source.Level,
            ImageUrl = source.ImageUrl,
            ImageAlt = source.ImageAlt,
            IconText = source.IconText,
            SecondaryLabel = source.SecondaryLabel,
            BadgeText = source.BadgeText,
            Size = source.Size,
            Fixed = source.Fixed,
            Hidden = source.Hidden
        };
        if (source.HasExplicitPosition) { clone.X = source.X; clone.Y = source.Y; }
        clone.Style.BackgroundColor = source.Style.BackgroundColor;
        clone.Style.BorderColor = source.Style.BorderColor;
        clone.Style.LabelColor = source.Style.LabelColor;
        clone.Style.LabelBackgroundColor = source.Style.LabelBackgroundColor;
        clone.Style.Shadow = source.Style.Shadow;
        foreach (var pair in source.Metadata) clone.Metadata[pair.Key] = pair.Value;
        return clone;
    }

    private static void SynchronizeClusterMembership(IReadOnlyList<GraphSceneCluster> clusters, IEnumerable<GraphSceneNode> updatedNodes, IEnumerable<GraphSceneCluster> updatedClusters) {
        var declaredMembership = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var cluster in updatedClusters) {
            foreach (var nodeId in cluster.NodeIds) {
                if (!declaredMembership.TryGetValue(nodeId, out var clusterIds)) declaredMembership[nodeId] = clusterIds = new HashSet<string>(StringComparer.Ordinal);
                clusterIds.Add(cluster.Id);
            }
        }

        foreach (var node in updatedNodes) {
            declaredMembership.TryGetValue(node.Id, out var declaredClusterIds);
            foreach (var cluster in clusters) {
                var preserveDeclaredMembership = string.IsNullOrWhiteSpace(node.ClusterId) && declaredClusterIds != null && declaredClusterIds.Contains(cluster.Id);
                if (!preserveDeclaredMembership) cluster.NodeIds.RemoveAll(id => string.Equals(id, node.Id, StringComparison.Ordinal));
            }
            if (string.IsNullOrWhiteSpace(node.ClusterId)) continue;
            var target = clusters.FirstOrDefault(cluster => string.Equals(cluster.Id, node.ClusterId, StringComparison.Ordinal));
            if (target != null && !target.NodeIds.Contains(node.Id, StringComparer.Ordinal)) target.NodeIds.Add(node.Id);
        }
    }

    private static void Replace<T>(List<T> target, IEnumerable<T> values) {
        target.Clear();
        target.AddRange(values);
    }
}

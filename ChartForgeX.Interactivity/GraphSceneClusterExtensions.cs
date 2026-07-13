using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Provides reusable cluster materialization helpers for graph adapters.
/// </summary>
public static class GraphSceneClusterExtensions {
    /// <summary>
    /// Gets declared clusters plus any reusable clusters derived from the active cluster policy.
    /// </summary>
    /// <param name="scene">The source graph scene.</param>
    /// <returns>Cluster definitions that adapters should render, collapse, search, export, and expose through host events.</returns>
    public static IReadOnlyList<GraphSceneCluster> GetEffectiveClusters(this GraphScene scene) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var clusters = scene.Clusters.Select(CloneCluster).ToList();
        if (scene.Options.Cluster.CollapseOnLoad) {
            foreach (var cluster in clusters) cluster.Collapsed = true;
        }

        var clusterIds = new HashSet<string>(clusters.Select(cluster => cluster.Id), StringComparer.Ordinal);
        var clusteredNodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cluster in clusters) {
            foreach (var nodeId in cluster.NodeIds) clusteredNodeIds.Add(nodeId);
        }

        foreach (var node in scene.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.ClusterId)) clusteredNodeIds.Add(node.Id);
        }

        if (ShouldDeriveGroupClusters(scene)) {
            foreach (var group in scene.Nodes
                .Where(node => !string.IsNullOrWhiteSpace(node.GroupId) && !clusteredNodeIds.Contains(node.Id))
                .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)) {
                var members = group.Select(node => node.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
                if (members.Length < scene.Options.Cluster.MinimumClusterSize) continue;
                var clusterId = UniqueClusterId("group-" + group.Key, clusterIds);
                var cluster = CreateDerivedCluster(clusterId, group.Key, "group", "node-group", members, scene.Options.Cluster.CollapseOnLoad);
                cluster.Metadata["cluster.groupId"] = group.Key;
                clusters.Add(cluster);
                foreach (var member in members) clusteredNodeIds.Add(member);
            }
        }

        if (ShouldDeriveAdaptiveClusters(scene)) AddAdaptiveClusters(scene, clusters, clusterIds, clusteredNodeIds);

        return clusters;
    }

    private static bool ShouldDeriveGroupClusters(GraphScene scene) {
        if (!scene.Options.HasFeature(GraphSceneFeatures.Clustering)) return false;
        return scene.Options.Cluster.Mode == GraphClusterMode.ByGroup
            || scene.Options.Cluster.Mode == GraphClusterMode.Hybrid;
    }

    private static bool ShouldDeriveAdaptiveClusters(GraphScene scene) {
        if (!scene.Options.HasFeature(GraphSceneFeatures.Clustering) || !scene.Options.Cluster.Adaptive) return false;
        if (scene.Nodes.Count < scene.Options.LevelOfDetail.ClusterNodeThreshold) return false;
        return scene.Options.Cluster.Mode == GraphClusterMode.Adaptive || scene.Options.Cluster.Mode == GraphClusterMode.Hybrid;
    }

    private static void AddAdaptiveClusters(GraphScene scene, ICollection<GraphSceneCluster> clusters, ISet<string> clusterIds, ISet<string> clusteredNodeIds) {
        var candidates = scene.Nodes.Where(node => !clusteredNodeIds.Contains(node.Id)).ToDictionary(node => node.Id, StringComparer.Ordinal);
        if (candidates.Count < scene.Options.Cluster.MinimumClusterSize) return;
        var adjacency = candidates.Keys.ToDictionary(id => id, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var edge in scene.Edges) {
            if (!candidates.ContainsKey(edge.SourceNodeId) || !candidates.ContainsKey(edge.TargetNodeId)) continue;
            adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);
            adjacency[edge.TargetNodeId].Add(edge.SourceNodeId);
        }

        var unassigned = new HashSet<string>(candidates.Keys, StringComparer.Ordinal);
        var deferred = new List<string>();
        var clusterNumber = 1;
        while (unassigned.Count > 0) {
            var seed = unassigned.OrderByDescending(id => adjacency[id].Count).ThenBy(id => id, StringComparer.Ordinal).First();
            var members = GrowCommunity(seed, adjacency, unassigned, scene.Options.Cluster.TargetClusterSize);
            if (members.Count < scene.Options.Cluster.MinimumClusterSize) {
                deferred.AddRange(members);
                continue;
            }

            var clusterId = UniqueClusterId("adaptive-" + clusterNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), clusterIds);
            clusters.Add(CreateDerivedCluster(clusterId, "Community " + clusterNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), "community", "adaptive-structure", members, scene.Options.Cluster.CollapseOnLoad));
            clusterNumber++;
        }

        if (deferred.Count >= scene.Options.Cluster.MinimumClusterSize) {
            var members = deferred.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var clusterId = UniqueClusterId("adaptive-" + clusterNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), clusterIds);
            clusters.Add(CreateDerivedCluster(clusterId, "Community " + clusterNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), "community", "adaptive-structure", members, scene.Options.Cluster.CollapseOnLoad));
        }
    }

    private static IReadOnlyList<string> GrowCommunity(string seed, IReadOnlyDictionary<string, HashSet<string>> adjacency, ISet<string> unassigned, int targetSize) {
        var members = new List<string>();
        var queued = new HashSet<string>(StringComparer.Ordinal) { seed };
        var queue = new Queue<string>();
        queue.Enqueue(seed);
        while (queue.Count > 0 && members.Count < targetSize) {
            var id = queue.Dequeue();
            if (!unassigned.Remove(id)) continue;
            members.Add(id);
            foreach (var neighbor in adjacency[id].Where(unassigned.Contains).OrderByDescending(candidate => adjacency[candidate].Count).ThenBy(candidate => candidate, StringComparer.Ordinal)) {
                if (queued.Add(neighbor)) queue.Enqueue(neighbor);
            }
        }

        return members.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    private static GraphSceneCluster CreateDerivedCluster(string id, string label, string kind, string source, IEnumerable<string> members, bool collapsed) {
        var materialized = members.OrderBy(member => member, StringComparer.Ordinal).ToArray();
        var cluster = new GraphSceneCluster { Id = id, Label = label, Kind = kind, Collapsed = collapsed };
        cluster.NodeIds.AddRange(materialized);
        cluster.Metadata["cluster.source"] = source;
        cluster.Metadata["cluster.nodeCount"] = materialized.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return cluster;
    }

    private static string UniqueClusterId(string preferred, ISet<string> usedIds) {
        var id = preferred;
        var suffix = 2;
        while (!usedIds.Add(id)) id = preferred + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return id;
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
}

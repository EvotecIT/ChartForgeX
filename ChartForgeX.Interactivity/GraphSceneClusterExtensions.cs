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

        if (!ShouldDeriveGroupClusters(scene)) return clusters;
        var clusterIds = new HashSet<string>(clusters.Select(cluster => cluster.Id), StringComparer.Ordinal);

        var clusteredNodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cluster in clusters) {
            foreach (var nodeId in cluster.NodeIds) clusteredNodeIds.Add(nodeId);
        }

        foreach (var node in scene.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.ClusterId)) clusteredNodeIds.Add(node.Id);
        }

        foreach (var group in scene.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId) && !clusteredNodeIds.Contains(node.Id))
            .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)) {
            var members = group.Select(node => node.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (members.Length < scene.Options.Cluster.MinimumClusterSize) continue;
            var clusterId = UniqueClusterId("group-" + group.Key, clusterIds);
            var cluster = new GraphSceneCluster {
                Id = clusterId,
                Label = group.Key,
                Kind = "group",
                Collapsed = scene.Options.Cluster.CollapseOnLoad
            };
            cluster.NodeIds.AddRange(members);
            cluster.Metadata["cluster.source"] = "node-group";
            cluster.Metadata["cluster.groupId"] = group.Key;
            cluster.Metadata["cluster.nodeCount"] = members.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
            clusters.Add(cluster);
        }

        return clusters;
    }

    private static bool ShouldDeriveGroupClusters(GraphScene scene) {
        if (!scene.Options.HasFeature(GraphSceneFeatures.Clustering)) return false;
        return scene.Options.Cluster.Mode == GraphClusterMode.ByGroup
            || scene.Options.Cluster.Mode == GraphClusterMode.Hybrid;
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
            Collapsed = source.Collapsed
        };
        clone.NodeIds.AddRange(source.NodeIds);
        foreach (var pair in source.Metadata) clone.Metadata[pair.Key] = pair.Value;
        return clone;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Interactivity;

/// <summary>Plans bounded neighborhoods without changing scene geometry, identity, or membership.</summary>
public static class GraphSceneNeighborhoodPlanner {
    /// <summary>Creates a view centered on a node, with explicit node and relationship budgets.</summary>
    /// <param name="scene">The complete source scene. Replan after changing its nodes or relationships.</param>
    /// <param name="rootNodeId">The node retained on every neighborhood page.</param>
    /// <param name="configure">Optional distance, budget, and paging settings.</param>
    /// <returns>A stage consumable by the static graph exporters. Counts include everything omitted from the source scene.</returns>
    /// <remarks>Traversal treats directed relationships as undirected for discovery; exported relationships retain their direction. Node pages are ordered by hop distance then ordinal id. Relationships nearest the root are retained first, with ordinal edge ids breaking ties. Planning examines the source graph; the budgets bound output, not discovery work.</remarks>
    public static GraphSceneStage CreateNeighborhood(this GraphScene scene, string rootNodeId, Action<GraphSceneNeighborhoodOptions>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (rootNodeId == null) throw new ArgumentNullException(nameof(rootNodeId));
        scene.Validate();
        var options = new GraphSceneNeighborhoodOptions();
        configure?.Invoke(options);
        options.Validate();
        var adjacency = scene.Nodes.ToDictionary(node => node.Id, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        if (!adjacency.ContainsKey(rootNodeId)) throw new ArgumentException("The neighborhood root is not present in the scene.", nameof(rootNodeId));
        foreach (var edge in scene.Edges) {
            adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);
            adjacency[edge.TargetNodeId].Add(edge.SourceNodeId);
        }

        var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [rootNodeId] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(rootNodeId);
        while (queue.Count > 0) {
            var id = queue.Dequeue();
            var depth = distances[id];
            if (depth >= options.Hops) continue;
            foreach (var neighbor in adjacency[id]) {
                if (distances.ContainsKey(neighbor)) continue;
                distances.Add(neighbor, depth + 1);
                queue.Enqueue(neighbor);
            }
        }

        var visible = new HashSet<string>(distances.Where(pair => pair.Key != rootNodeId)
            .OrderBy(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Skip(options.NeighborOffset).Take(options.MaximumNodes - 1).Select(pair => pair.Key), StringComparer.Ordinal) { rootNodeId };
        var induced = scene.Edges.Where(edge => visible.Contains(edge.SourceNodeId) && visible.Contains(edge.TargetNodeId)).ToArray();
        var edges = induced.OrderBy(edge => Math.Min(distances[edge.SourceNodeId], distances[edge.TargetNodeId]))
            .ThenBy(edge => Math.Max(distances[edge.SourceNodeId], distances[edge.TargetNodeId]))
            .ThenBy(edge => edge.Id, StringComparer.Ordinal).Take(options.MaximumEdges).Select(edge => edge.Id).ToArray();
        var boundaryCount = scene.Edges.Count(edge => visible.Contains(edge.SourceNodeId) != visible.Contains(edge.TargetNodeId));
        return new GraphSceneStage(1, options.Hops, "neighborhood-" + rootNodeId,
            visible.Count == distances.Count && edges.Length == induced.Length, rootNodeId,
            visible.OrderBy(id => id, StringComparer.Ordinal).ToArray(), edges, Array.Empty<string>(),
            scene.Nodes.Count, scene.Edges.Count, boundaryCount, distances.Count, GraphSceneStageKind.Neighborhood);
    }
}

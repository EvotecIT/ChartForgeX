using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Interactivity;

/// <summary>Plans deterministic hierarchy snapshots without depending on a browser or rendering backend.</summary>
public static class GraphSceneStagePlanner {
    /// <summary>Creates overview-to-detail stages for a graph scene.</summary>
    /// <param name="scene">The graph scene to plan.</param>
    /// <param name="configure">Optional stage planning configuration.</param>
    /// <returns>Ordered deterministic graph stages.</returns>
    public static IReadOnlyList<GraphSceneStage> CreateStages(this GraphScene scene, Action<GraphSceneStageOptions>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        scene.Validate();
        var options = new GraphSceneStageOptions();
        configure?.Invoke(options);
        options.Validate();

        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(options.RootNodeId) && !nodesById.ContainsKey(options.RootNodeId!)) throw new InvalidOperationException("Graph scene stage root references a missing node: " + options.RootNodeId);
        var children = scene.Nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var node in scene.Nodes) if (!string.IsNullOrWhiteSpace(node.ParentId)) children[node.ParentId!].Add(node.Id);
        foreach (var list in children.Values) list.Sort(StringComparer.Ordinal);

        var roots = string.IsNullOrWhiteSpace(options.RootNodeId)
            ? scene.Nodes.Where(node => string.IsNullOrWhiteSpace(node.ParentId)).Select(node => node.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray()
            : new[] { options.RootNodeId! };
        var depths = ResolveNodeDepths(roots, children);
        var maximumDepth = depths.Count == 0 ? 0 : depths.Values.Max();
        var requestedDepths = ResolveStageDepths(options, maximumDepth);
        var stages = new List<GraphSceneStage>(requestedDepths.Count);
        for (var index = 0; index < requestedDepths.Count; index++) {
            var depth = requestedDepths[index];
            var visibleNodes = depths.Where(pair => pair.Value <= depth).Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var visibleSet = new HashSet<string>(visibleNodes, StringComparer.Ordinal);
            var visibleEdges = scene.Edges.Where(edge => visibleSet.Contains(edge.SourceNodeId) && visibleSet.Contains(edge.TargetNodeId)).Select(edge => edge.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var frontier = visibleNodes.Where(id => children[id].Any(child => !visibleSet.Contains(child))).ToArray();
            var full = depth >= maximumDepth;
            var name = full ? "full" : depth == 0 ? "overview" : "depth-" + depth;
            stages.Add(new GraphSceneStage(index + 1, depth, name, full, options.RootNodeId, visibleNodes, visibleEdges, frontier, scene.Nodes.Count));
        }

        return stages;
    }

    private static Dictionary<string, int> ResolveNodeDepths(IEnumerable<string> roots, IReadOnlyDictionary<string, List<string>> children) {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<KeyValuePair<string, int>>();
        foreach (var root in roots) queue.Enqueue(new KeyValuePair<string, int>(root, 0));
        while (queue.Count > 0) {
            var current = queue.Dequeue();
            if (result.ContainsKey(current.Key)) continue;
            result[current.Key] = current.Value;
            foreach (var child in children[current.Key]) queue.Enqueue(new KeyValuePair<string, int>(child, current.Value + 1));
        }
        return result;
    }

    private static List<int> ResolveStageDepths(GraphSceneStageOptions options, int maximumDepth) {
        var requested = options.Depths.Count > 0
            ? options.Depths.Select(depth => Math.Min(depth, maximumDepth)).Distinct().OrderBy(depth => depth).ToList()
            : EvenDepths(options.StageCount, maximumDepth);
        if (options.IncludeFullScene && !requested.Contains(maximumDepth)) requested.Add(maximumDepth);
        if (requested.Count == 0) requested.Add(maximumDepth);
        return requested.Distinct().OrderBy(depth => depth).ToList();
    }

    private static List<int> EvenDepths(int stageCount, int maximumDepth) {
        if (stageCount == 1 || maximumDepth == 0) return new List<int> { maximumDepth };
        var count = Math.Min(stageCount, maximumDepth + 1);
        var result = new List<int>(count);
        for (var index = 0; index < count; index++) {
            var depth = (int)Math.Round(index * maximumDepth / (double)(count - 1), MidpointRounding.AwayFromZero);
            if (!result.Contains(depth)) result.Add(depth);
        }
        return result;
    }
}

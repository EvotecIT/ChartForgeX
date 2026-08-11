using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void ApplyEdgeRankHints(TopologyChart chart) {
        if (chart.Edges.All(edge => edge.MinimumRankSpan <= 1)) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var edges = chart.Edges
            .Where(edge => nodes.ContainsKey(edge.SourceNodeId) && nodes.ContainsKey(edge.TargetNodeId))
            .OrderBy(edge => edge.Id, StringComparer.Ordinal)
            .ToList();
        var adjacency = nodes.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in edges) adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);

        var components = StronglyConnectedComponents(nodes.Keys, adjacency);
        var componentByNode = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var component = 0; component < components.Count; component++) {
            foreach (var nodeId in components[component]) componentByNode[nodeId] = component;
        }

        var layerOrder = chart.Nodes
            .Select(GetLayer)
            .Distinct()
            .OrderBy(layer => layer)
            .Select((layer, index) => new { layer, index })
            .ToDictionary(item => item.layer, item => item.index);
        var componentRanks = components
            .Select(component => component.Select(nodeId => layerOrder[GetLayer(nodes[nodeId])]).Max())
            .ToArray();
        var outgoing = Enumerable.Range(0, components.Count).ToDictionary(component => component, _ => new List<TopologyEdge>());
        var indegree = Enumerable.Range(0, components.Count).ToDictionary(component => component, _ => 0);
        foreach (var edge in edges) {
            var source = componentByNode[edge.SourceNodeId];
            var target = componentByNode[edge.TargetNodeId];
            if (source == target) continue;
            outgoing[source].Add(edge);
            indegree[target]++;
        }

        var ready = new SortedSet<int>(indegree.Where(item => item.Value == 0).Select(item => item.Key));
        while (ready.Count > 0) {
            var component = ready.Min;
            ready.Remove(component);
            foreach (var edge in outgoing[component].OrderBy(item => item.Id, StringComparer.Ordinal)) {
                var target = componentByNode[edge.TargetNodeId];
                componentRanks[target] = Math.Max(componentRanks[target], componentRanks[component] + edge.MinimumRankSpan);
                indegree[target]--;
                if (indegree[target] == 0) ready.Add(target);
            }
        }

        foreach (var node in chart.Nodes) {
            node.Metadata["layer"] = componentRanks[componentByNode[node.Id]].ToString(CultureInfo.InvariantCulture);
            node.Metadata["layout.rankHintsApplied"] = "true";
        }
    }

    private static List<List<string>> StronglyConnectedComponents(IEnumerable<string> nodeIds, IReadOnlyDictionary<string, List<string>> adjacency) {
        var nextIndex = 0;
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowLinks = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<List<string>>();

        void Visit(string nodeId) {
            indexes[nodeId] = nextIndex;
            lowLinks[nodeId] = nextIndex;
            nextIndex++;
            stack.Push(nodeId);
            onStack.Add(nodeId);
            foreach (var targetId in adjacency[nodeId].Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal)) {
                if (!indexes.ContainsKey(targetId)) {
                    Visit(targetId);
                    lowLinks[nodeId] = Math.Min(lowLinks[nodeId], lowLinks[targetId]);
                } else if (onStack.Contains(targetId)) {
                    lowLinks[nodeId] = Math.Min(lowLinks[nodeId], indexes[targetId]);
                }
            }
            if (lowLinks[nodeId] != indexes[nodeId]) return;
            var component = new List<string>();
            string member;
            do {
                member = stack.Pop();
                onStack.Remove(member);
                component.Add(member);
            } while (!string.Equals(member, nodeId, StringComparison.Ordinal));
            component.Sort(StringComparer.Ordinal);
            components.Add(component);
        }

        foreach (var nodeId in nodeIds.OrderBy(id => id, StringComparer.Ordinal)) {
            if (!indexes.ContainsKey(nodeId)) Visit(nodeId);
        }
        return components;
    }
}

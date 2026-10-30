using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup;

/// <summary>Chooses a viewport that fits an architecture diagram without large empty margins.</summary>
/// <remarks>The layered layout spreads layers across the viewport, so the viewport is sized from the layer count
/// along the flow and the widest layer across it.</remarks>
internal static class MarkupArchitectureViewport {
    public static (double Width, double Height) Estimate(TopologyChart chart) {
        var ranks = Ranks(chart);
        var layers = ranks.Count == 0 ? 1 : ranks.Values.Max() + 1;
        var widest = ranks.Count == 0 ? 1 : ranks.Values.GroupBy(rank => rank).Max(group => group.Count());
        var label = chart.Edges.Select(edge => (edge.Label ?? string.Empty).Length).DefaultIfEmpty(0).Max();
        var gap = Math.Max(110, label * 7 + 70);
        var nesting = chart.Groups.Count == 0 ? 0 : chart.Groups.Max(group => Depth(chart, group)) + 1;
        var horizontal = chart.LayoutDirection == TopologyLayoutDirection.LeftToRight || chart.LayoutDirection == TopologyLayoutDirection.RightToLeft;
        var along = layers * 150 + (layers - 1) * gap + nesting * 40;
        var across = widest * 72 + (widest - 1) * 56 + nesting * 64 + Math.Min(chart.Groups.Count, 4) * 24;
        if (!horizontal) {
            along = layers * 72 + (layers - 1) * Math.Max(90, gap / 2) + nesting * 64;
            across = widest * 150 + (widest - 1) * 48 + nesting * 40 + Math.Min(chart.Groups.Count, 4) * 24;
        }

        var title = (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72) + (string.IsNullOrWhiteSpace(chart.Subtitle) ? 0 : 20);
        var width = (horizontal ? along : across) + 56;
        var height = (horizontal ? across : along) + 56 + title;
        return (Clamp(width, 480, 3200), Clamp(height, 300, 2000));
    }

    private static Dictionary<string, int> Ranks(TopologyChart chart) {
        var ids = chart.Nodes.Select(node => node.Id).Distinct(StringComparer.Ordinal).ToList();
        var incoming = ids.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        var outgoing = ids.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!incoming.ContainsKey(edge.SourceNodeId) || !incoming.ContainsKey(edge.TargetNodeId) || edge.SourceNodeId == edge.TargetNodeId) continue;
            outgoing[edge.SourceNodeId].Add(edge.TargetNodeId);
            incoming[edge.TargetNodeId]++;
        }

        // Longest-path layering: a node's rank is one more than its deepest predecessor. When only cycles remain,
        // the unprocessed node with the fewest waiting predecessors is released so every node gets a rank.
        var ranks = ids.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        var done = new HashSet<string>(StringComparer.Ordinal);
        var ready = new Queue<string>(ids.Where(id => incoming[id] == 0));
        while (done.Count < ids.Count) {
            if (ready.Count == 0) ready.Enqueue(ids.Where(id => !done.Contains(id)).OrderBy(id => incoming[id]).First());
            var id = ready.Dequeue();
            if (!done.Add(id)) continue;
            foreach (var target in outgoing[id]) {
                if (done.Contains(target)) continue;
                ranks[target] = Math.Max(ranks[target], ranks[id] + 1);
                if (--incoming[target] == 0) ready.Enqueue(target);
            }
        }

        return ranks;
    }

    private static int Depth(TopologyChart chart, TopologyGroup group) {
        var depth = 0;
        var parent = group.ParentGroupId;
        while (!string.IsNullOrWhiteSpace(parent) && depth < 32) {
            depth++;
            parent = chart.Groups.FirstOrDefault(candidate => candidate.Id == parent)?.ParentGroupId;
        }

        return depth;
    }

    private static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(maximum, value));
}

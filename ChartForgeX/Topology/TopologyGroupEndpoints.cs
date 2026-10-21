using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>Resolves routing-only boundary geometry without adding semantic or visible nodes.</summary>
internal static class TopologyGroupEndpoints {
    internal static Dictionary<string, TopologyNode> Resolve(TopologyChart chart) {
        var endpoints = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var group in chart.Groups) {
            if (endpoints.ContainsKey(group.Id)) continue;
            endpoints.Add(group.Id, new TopologyNode {
                Id = group.Id, Label = group.Label, GroupId = group.Id,
                X = group.X, Y = group.Y, Width = Math.Max(1, group.Width), Height = Math.Max(1, group.Height),
                Shape = TopologyNodeShape.Rectangle, ShowStatusBadge = false
            });
        }
        return endpoints;
    }

    /// <summary>Paints ancestors before descendants even when the caller supplied groups in another order.</summary>
    internal static IEnumerable<TopologyGroup> InPaintOrder(TopologyChart chart) {
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        return chart.Groups.OrderBy(Depth);

        int Depth(TopologyGroup group) {
            var depth = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal) { group.Id };
            while (group.ParentGroupId != null && seen.Add(group.ParentGroupId) && groups.TryGetValue(group.ParentGroupId, out var parent)) {
                depth++; group = parent;
            }
            return depth;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidC4Rendering {
    /// <summary>
    /// Lays a boundary's internal relationship chain along the target's long axis when the chain is deeper than
    /// it is broad, and runs the outer flow (people, external systems) across it. A C4 container view then keeps
    /// a balanced aspect instead of one tall or one wide strip.
    /// </summary>
    private static void OrientBoundaries(TopologyChart chart, MermaidC4Document document, TopologyLayoutDirection chain) {
        if (chart.LayoutMode != TopologyLayoutMode.Layered || document.Boundaries.Count == 0) return;
        var oriented = false;
        var parents = document.Boundaries.ToDictionary(boundary => boundary.Id, boundary => boundary.ParentId, StringComparer.Ordinal);
        foreach (var group in chart.Groups) {
            if (group.LayoutDirection.HasValue) continue;
            var members = new HashSet<string>(document.Elements.Where(element => Within(element.BoundaryId, group.Id, parents)).Select(element => element.Alias), StringComparer.Ordinal);
            var links = document.Relationships.Where(link => link.SourceAlias != link.TargetAlias && members.Contains(link.SourceAlias) && members.Contains(link.TargetAlias)).ToArray();
            if (links.Length == 0) continue;
            // Longest-path layers, bounded by the member count so a cycle cannot loop.
            var layer = members.ToDictionary(member => member, _ => 0, StringComparer.Ordinal);
            for (var pass = 0; pass < members.Count; pass++) {
                var changed = false;
                foreach (var link in links) if (layer[link.TargetAlias] < layer[link.SourceAlias] + 1 && layer[link.SourceAlias] + 1 < members.Count) { layer[link.TargetAlias] = layer[link.SourceAlias] + 1; changed = true; }
                if (!changed) break;
            }
            var depth = layer.Values.Max() + 1;
            var breadth = layer.Values.GroupBy(value => value).Max(items => items.Count());
            if (depth >= 2 && depth > breadth) { group.LayoutDirection = chain; oriented = true; }
        }
        if (oriented) chart.LayoutDirection = chain == TopologyLayoutDirection.LeftToRight ? TopologyLayoutDirection.TopToBottom : TopologyLayoutDirection.LeftToRight;
    }

    private static bool Within(string? boundary, string group, IReadOnlyDictionary<string, string?> parents) {
        for (var guard = 0; boundary != null && guard <= parents.Count; guard++) {
            if (string.Equals(boundary, group, StringComparison.Ordinal)) return true;
            boundary = parents.TryGetValue(boundary, out var parent) ? parent : null;
        }
        return false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Filters constraints, not semantic edges, through the existing component/longest-path rank owner.</summary>
    private static void ApplySwimlaneRankHints(TopologyChart chart, TopologyRenderOptions? options) {
        var constraint = SwimlaneRankConstraint(chart, options);
        var optimize = options?.LayeredOptimizeRanksByCrossings == true;
        if (constraint == null) {
            ApplyEdgeRankHints(chart, force: optimize, preserveAuthoredRanks: optimize, optimizeCrossings: optimize);
            return;
        }
        ApplyEdgeRankHints(chart, force: true, rankConstraint: constraint, preserveAuthoredRanks: true, optimizeCrossings: optimize);
    }

    /// <summary>Uses outer responsibility membership while retaining constraints with any ungrouped endpoint.</summary>
    private static Func<TopologyEdge, bool>? SwimlaneRankConstraint(TopologyChart chart, TopologyRenderOptions? options) {
        if (options?.SwimlaneIgnoreCrossLaneRankConstraints != true) return null;
        var roots = SwimlaneRootIds(chart);
        return edge => {
            roots.TryGetValue(edge.SourceNodeId, out var source);
            roots.TryGetValue(edge.TargetNodeId, out var target);
            return source == null || target == null || string.Equals(source, target, StringComparison.Ordinal);
        };
    }

    /// <summary>Resolves shared outer responsibility identity without changing graph membership.</summary>
    private static Dictionary<string, string?> SwimlaneRootIds(TopologyChart chart) {
        var groups = SwimlaneGroups(chart);
        var roots = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var group in chart.Groups) roots[group.Id] = Root(group.Id);
        foreach (var node in chart.Nodes) roots[node.Id] = Root(node.GroupId);
        return roots;
        string? Root(string? id) {
            while (id != null && groups.TryGetValue(id, out var group) && group.ParentGroupId != null) id = group.ParentGroupId;
            return id;
        }
    }

    /// <summary>Keeps the incumbent shared-rank lane path unless an authored local direction changes it.</summary>
    private static bool HasEffectiveSwimlaneDirection(TopologyChart chart) {
        if (!chart.Groups.Any(group => group.LayoutDirection.HasValue)) return false;
        var groups = SwimlaneGroups(chart);
        foreach (var group in chart.Groups) {
            var inherited = group.ParentGroupId == null ? chart.LayoutDirection
                : TopologyGroupHierarchy.ResolveDirection(groups[group.ParentGroupId], id => groups[id], chart.LayoutDirection);
            if (group.LayoutDirection.HasValue && group.LayoutDirection.Value != inherited) return true;
        }
        return false;
    }

    private static Dictionary<string, TopologyGroup> SwimlaneGroups(TopologyChart chart) {
        var validation = TopologyGroupHierarchy.Validate(chart.Groups.Select(group => (group.Id, group.ParentGroupId)));
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        return chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
    }
}

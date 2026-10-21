using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void IncludeSelectedGroupDescendants(TopologyChart chart, HashSet<string> selected) {
        bool changed;
        do {
            changed = false;
            foreach (var group in chart.Groups) {
                if (group.ParentGroupId != null && selected.Contains(group.ParentGroupId) && selected.Add(group.Id)) changed = true;
            }
        } while (changed);
    }

    internal static void DetachOmittedSourceGroups(TopologyChart source, TopologyChart prepared) {
        var sourceGroupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in source.Groups) sourceGroupIds.Add(group.Id);
        var preparedGroupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in prepared.Groups) preparedGroupIds.Add(group.Id);
        foreach (var node in prepared.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.GroupId) && sourceGroupIds.Contains(node.GroupId!) && !preparedGroupIds.Contains(node.GroupId!)) {
                node.GroupId = null;
            }
        }
        foreach (var group in prepared.Groups) {
            if (group.ParentGroupId != null && sourceGroupIds.Contains(group.ParentGroupId) && !preparedGroupIds.Contains(group.ParentGroupId))
                group.ParentGroupId = null;
        }
    }

}

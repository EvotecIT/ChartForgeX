using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
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
    }

}

using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Projects only the cloned visible model; raw source declarations remain unchanged.</summary>
    private static void ProjectAlignments(TopologyChart chart) {
        var visible = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) visible.Add(node.Id);
        for (var i = chart.Alignments.Count - 1; i >= 0; i--) {
            var declaration = chart.Alignments[i];
            declaration.NodeIds.RemoveAll(id => !visible.Contains(id));
            if (declaration.NodeIds.Count < 2) chart.Alignments.RemoveAt(i);
        }
    }
}

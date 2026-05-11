using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static Dictionary<string, (double X, double Y)> ExplicitGroupPositions(IEnumerable<TopologyGroup> groups) {
        var result = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        foreach (var group in groups) {
            if (!HasExplicitGroupPlacement(group) || result.ContainsKey(group.Id)) continue;
            result[group.Id] = (group.X, group.Y);
        }

        return result;
    }

    private static void RestoreExplicitDenseGroupPositions(TopologyChart chart, IReadOnlyDictionary<string, (double X, double Y)> positions) {
        if (positions.Count == 0) return;
        foreach (var group in chart.Groups) {
            if (!positions.TryGetValue(group.Id, out var position)) continue;
            var dx = position.X - group.X;
            var dy = position.Y - group.Y;
            if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001) continue;
            group.X = position.X;
            group.Y = position.Y;
            foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal))) {
                node.X += dx;
                node.Y += dy;
            }
        }
    }

    private static bool HasExplicitGroupPlacement(TopologyGroup group) =>
        !IsUnset(group.X) || !IsUnset(group.Y) || group.Width > 0 || group.Height > 0;
}

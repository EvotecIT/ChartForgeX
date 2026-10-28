using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Counts proper intersections of stable normalized slot segments, without constructing routes or expanding long rank spans.</summary>
    private static bool TryRankCrossingScore(RankPreparation prepared, int[] ranks, ref int visits, out long crossings) {
        crossings = 0;
        var count = prepared.ObservedEdges.Length;
        var work = (long)prepared.Nodes.Count + count + (long)count * (count - 1) / 2;
        if (work > RankOptimizationMaximumVisits - visits) return false;
        visits += (int)work;
        var coordinates = new Dictionary<string, (double Rank, double Slot)>(StringComparer.Ordinal);
        foreach (var row in prepared.Nodes.GroupBy(node => ranks[prepared.ComponentByNode[node.Id]])) {
            var index = 0; var total = row.Count();
            foreach (var node in row) coordinates[node.Id] = (row.Key, (index++ + .5) / total);
        }
        for (var first = 0; first < count; first++) {
            var a = prepared.ObservedEdges[first];
            for (var second = first + 1; second < count; second++) {
                var b = prepared.ObservedEdges[second];
                if (a.SourceNodeId == b.SourceNodeId || a.SourceNodeId == b.TargetNodeId || a.TargetNodeId == b.SourceNodeId || a.TargetNodeId == b.TargetNodeId) continue;
                if (Cross(coordinates[a.SourceNodeId], coordinates[a.TargetNodeId], coordinates[b.SourceNodeId], coordinates[b.TargetNodeId])) crossings++;
            }
        }
        return true;

        bool Cross((double Rank, double Slot) a, (double Rank, double Slot) b, (double Rank, double Slot) c, (double Rank, double Slot) d) {
            if (a.Rank == b.Rank || c.Rank == d.Rank) return false;
            var dx = b.Rank - a.Rank; var dy = b.Slot - a.Slot; var ex = d.Rank - c.Rank; var ey = d.Slot - c.Slot;
            var denominator = dx * ey - dy * ex;
            if (Math.Abs(denominator) < 1e-12) return false;
            var t = ((c.Rank - a.Rank) * ey - (c.Slot - a.Slot) * ex) / denominator;
            var u = ((c.Rank - a.Rank) * dy - (c.Slot - a.Slot) * dx) / denominator;
            return t > 1e-9 && t < 1 - 1e-9 && u > 1e-9 && u < 1 - 1e-9;
        }
    }
}

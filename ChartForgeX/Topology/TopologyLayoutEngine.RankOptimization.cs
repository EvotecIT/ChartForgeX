using System;
using System.Collections.Generic;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private const int RankOptimizationMaximumPasses = 4;
    private const int RankOptimizationMaximumCandidates = 65536;
    private const int RankOptimizationMaximumVisits = 1048576;

    /// <summary>Shares the existing SCC/constraint preparation; this is not a second rank algorithm.</summary>
    private sealed class RankPreparation {
        internal readonly IReadOnlyList<TopologyNode> Nodes;
        internal readonly IReadOnlyList<List<string>> Components;
        internal readonly IReadOnlyDictionary<string, int> ComponentByNode;
        internal readonly IReadOnlyList<TopologyEdge> Constraints;
        internal readonly TopologyEdge[] ObservedEdges;
        internal readonly int[] MinimumRanks;
        internal RankPreparation(TopologyChart chart, IReadOnlyList<List<string>> components, IReadOnlyDictionary<string, int> componentByNode,
            IReadOnlyList<TopologyEdge> constraints, int[] minimumRanks) {
            Nodes = chart.Nodes; Components = components; ComponentByNode = componentByNode; Constraints = constraints; MinimumRanks = minimumRanks;
            ObservedEdges = chart.Edges.Where(edge => componentByNode.ContainsKey(edge.SourceNodeId) && componentByNode.ContainsKey(edge.TargetNodeId)).ToArray();
        }
    }

    private static void ValidateRankOptimizationLayout(TopologyChart chart, TopologyRenderOptions? options) {
        if (options?.LayeredOptimizeRanksByCrossings != true) return;
        if (chart.LayoutMode is not (TopologyLayoutMode.Layered or TopologyLayoutMode.Swimlane)
            || chart.Nodes.Any(node => node.HasPositionOverride || !IsUnset(node.X) || !IsUnset(node.Y))
            || chart.Groups.Any(group => group.HasPositionOverride || group.X != 0 || group.Y != 0 || group.Width > 0 || group.Height > 0))
            throw new NotSupportedException("Rank crossing optimization requires automatic Layered or Swimlane coordinates.");
    }

    /// <summary>Moves whole components within retained precedence/minimum-span bounds, accepting only a lower stated objective.</summary>
    private static void OptimizeRankCrossings(RankPreparation prepared, int[] ranks) {
        var visits = 0; var candidates = 0;
        if (!TryRankCrossingScore(prepared, ranks, ref visits, out var best)) return;
        var maximum = ranks.DefaultIfEmpty(0).Max();
        var sourceIndex = prepared.Nodes.Select((node, index) => (node.Id, index)).ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);
        var sourceOrder = prepared.Components.Select(component => component.Min(id => sourceIndex[id])).ToArray();
        for (var pass = 0; pass < RankOptimizationMaximumPasses; pass++) {
            if (!Visit(ranks.Length)) return;
            var order = Enumerable.Range(0, ranks.Length).OrderByDescending(component => ranks[component]).ThenBy(component => sourceOrder[component]).ToArray();
            var changed = false;
            foreach (var component in order) {
                if (!Visit(prepared.Constraints.Count + ranks.Length + 1)) return;
                long lower = prepared.MinimumRanks[component]; long upper = maximum;
                foreach (var edge in prepared.Constraints) {
                    var from = prepared.ComponentByNode[RankSource(edge)]; var to = prepared.ComponentByNode[RankTarget(edge)];
                    if (from == to) continue;
                    if (to == component) lower = Math.Max(lower, (long)ranks[from] + edge.MinimumRankSpan);
                    if (from == component) upper = Math.Min(upper, (long)ranks[to] - edge.MinimumRankSpan);
                }
                if (lower > upper || lower > int.MaxValue || upper < int.MinValue) continue;
                // Occupied boundaries and feasible bounds avoid a scan through arbitrary authored integer rank holes.
                var positions = ranks.Concat(new[] { (int)lower, (int)upper }).Distinct().Where(rank => rank >= lower && rank <= upper).OrderBy(rank => rank).ToArray();
                foreach (var candidate in positions) {
                    if (candidate == ranks[component]) continue;
                    if (candidates++ >= RankOptimizationMaximumCandidates) return;
                    var previous = ranks[component]; ranks[component] = candidate;
                    if (!TryRankCrossingScore(prepared, ranks, ref visits, out var score)) { ranks[component] = previous; return; }
                    if (score < best) { best = score; changed = true; }
                    else ranks[component] = previous;
                }
            }
            if (!changed) return;
        }
        bool Visit(int count) {
            if (count < 0 || count > RankOptimizationMaximumVisits - visits) return false;
            visits += count; return true;
        }
    }
}

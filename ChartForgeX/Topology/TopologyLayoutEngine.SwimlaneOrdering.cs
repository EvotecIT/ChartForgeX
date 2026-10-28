using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private const int SwimlaneOrderingRestarts = 8;
    private const int SwimlaneOrderingMaximumSweeps = 32;
    private const int SwimlaneOrderingMaximumSwaps = 65536;
    private const int SwimlaneOrderingMaximumWeightVisits = 1048576;

    /// <summary>Returns a temporary root-responsibility order; authored graph collections and child order remain intact.</summary>
    private static IReadOnlyList<TopologyGroup> SwimlaneRootGroups(TopologyChart chart, TopologyRenderOptions? options) =>
        SwimlaneRootGroups(chart, options, out _);

    private static IReadOnlyList<TopologyGroup> SwimlaneRootGroups(TopologyChart chart, TopologyRenderOptions? options, out bool weightedOrdering) {
        weightedOrdering = false;
        var roots = chart.Groups.Where(group => group.ParentGroupId == null).ToArray();
        if (options?.SwimlaneAutomaticLaneOrdering != true || roots.Length < 2) return roots;
        var responsibilities = SwimlaneRootIds(chart);
        var sourceIndex = roots.Select((group, index) => (group.Id, index)).ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);
        var weights = new Dictionary<(int First, int Second), long>();
        foreach (var edge in chart.Edges) {
            if (!responsibilities.TryGetValue(edge.SourceNodeId, out var source) || source == null
                || !responsibilities.TryGetValue(edge.TargetNodeId, out var target) || target == null
                || source == target || !sourceIndex.TryGetValue(source, out var first) || !sourceIndex.TryGetValue(target, out var second)) continue;
            var key = first < second ? (first, second) : (second, first);
            weights.TryGetValue(key, out var count); weights[key] = count + 1;
        }
        if (weights.Count == 0) return roots;
        var pairs = weights.OrderBy(pair => pair.Key.First).ThenBy(pair => pair.Key.Second).ToArray();
        var visits = 0; var attempts = 0;
        var best = Enumerable.Range(0, roots.Length).ToArray();
        if (!TryCost(best, out var bestCost)) return roots;
        weightedOrdering = true;
        long bestDistance = 0;
        Search(best.ToArray());
        for (var restart = 0; restart < SwimlaneOrderingRestarts && attempts < SwimlaneOrderingMaximumSwaps && visits < SwimlaneOrderingMaximumWeightVisits; restart++) {
            // Native deterministic rotations/reversals avoid importing an external random search implementation.
            var offset = 1 + restart % (roots.Length - 1);
            var start = Enumerable.Range(0, roots.Length).Select(index => (index + offset) % roots.Length).ToArray();
            if (restart >= SwimlaneOrderingRestarts / 2) Array.Reverse(start);
            Search(start);
        }
        return best.Select(index => roots[index]).ToArray();

        void Search(int[] order) {
            if (!TryCost(order, out var cost)) return;
            Consider(order, cost);
            for (var sweep = 0; sweep < Math.Min(order.Length, SwimlaneOrderingMaximumSweeps); sweep++) {
                var changed = false;
                for (var index = 0; index + 1 < order.Length && attempts < SwimlaneOrderingMaximumSwaps; index++) {
                    attempts++;
                    Swap(order, index);
                    if (!TryCost(order, out var candidate)) { Swap(order, index); return; }
                    if (candidate < cost) { cost = candidate; changed = true; Consider(order, cost); }
                    else Swap(order, index);
                }
                if (!changed) return;
            }
        }
        void Consider(int[] order, long cost) {
            long distance = 0;
            for (var index = 0; index < order.Length; index++) distance += Math.Abs((long)index - order[index]);
            if (cost < bestCost || cost == bestCost && distance < bestDistance) {
                best = order.ToArray(); bestCost = cost; bestDistance = distance;
            }
        }
        bool TryCost(int[] order, out long cost) {
            cost = 0;
            if ((long)pairs.Length + order.Length > SwimlaneOrderingMaximumWeightVisits - visits) return false;
            visits += order.Length;
            var positions = new int[order.Length];
            for (var index = 0; index < order.Length; index++) positions[order[index]] = index;
            foreach (var pair in pairs) {
                visits++;
                cost += pair.Value * Math.Abs((long)positions[pair.Key.First] - positions[pair.Key.Second]);
            }
            return true;
        }
        void Swap(int[] order, int index) {
            var value = order[index]; order[index] = order[index + 1]; order[index + 1] = value;
        }
    }
}

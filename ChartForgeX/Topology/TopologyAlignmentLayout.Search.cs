using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal sealed partial class TopologyAlignmentLayout {
    private sealed class Trial {
        internal Item First = null!, Second = null!;
        internal int XCount, YCount, Next;
        internal int[] Directions = null!;
    }

    private void Solve() {
        var trials = new List<Trial>();
        while (true) {
            var horizontal = x.Solve(); var vertical = y.Solve();
            if (horizontal != null && vertical != null) {
                var pair = FindOverlap(horizontal, vertical);
                if (pair == null) { Commit(horizontal, vertical); return; }
                budget.Work();
                trials.Add(new Trial { First = pair.Value.First, Second = pair.Value.Second,
                    XCount = x.Count, YCount = y.Count, Directions = Preferences(pair.Value.First, pair.Value.Second) });
            }
            var resumed = false;
            // Failed branches retain their work charge. Only their trial edges are
            // rolled back; no recursion depth grows with the decision budget.
            while (trials.Count > 0) {
                budget.Work(); var trial = trials[trials.Count - 1];
                x.Rollback(trial.XCount); y.Rollback(trial.YCount);
                if (trial.Next < trial.Directions.Length) {
                    budget.Decision(); Separate(trial.First, trial.Second, trial.Directions[trial.Next++]); resumed = true; break;
                }
                trials.RemoveAt(trials.Count - 1);
            }
            if (!resumed) throw Conflict(x.ConflictDeclarationId ?? y.ConflictDeclarationId ?? chart.Alignments[0].Id);
        }
    }

    private (Item First, Item Second)? FindOverlap(double[] horizontal, double[] vertical) {
        // Auxiliary sweep order never changes authored nodes, groups or declarations.
        // Choose the earliest source-order collision, independently of sort ties.
        Item? first = null, second = null;
        foreach (var set in siblings) {
            var ordered = SweepOrder(set, horizontal);
            for (var i = 0; i < ordered.Length; i++) {
                var a = ordered[i]; var right = horizontal[a.High] + a.Right + TopologyLayoutNormalizer.NodeGap;
                for (var j = i + 1; j < ordered.Length; j++) {
                    budget.Work(); var b = ordered[j];
                    if (horizontal[b.Low] + b.Left >= right - 0.000001) break;
                    if (vertical[a.High] + a.Bottom + TopologyLayoutNormalizer.NodeGap <= vertical[b.Low] + b.Top + 0.000001
                        || vertical[b.High] + b.Bottom + TopologyLayoutNormalizer.NodeGap <= vertical[a.Low] + a.Top + 0.000001) continue;
                    var low = a.Order < b.Order ? a : b; var high = ReferenceEquals(low, a) ? b : a;
                    if (first == null || low.Order < first.Order || low.Order == first.Order && high.Order < second!.Order) { first = low; second = high; }
                }
            }
        }
        return first == null ? null : (first, second!);
    }

    private Item[] SweepOrder(List<Item> set, double[] horizontal) {
        budget.Work((long)set.Count * 2); var source = set.ToArray(); var target = new Item[source.Length];
        for (var width = 1; width < source.Length; width = width > source.Length / 2 ? source.Length : width * 2) {
            for (var start = 0; start < source.Length; start += width * 2) {
                var middle = Math.Min(start + width, source.Length); var end = Math.Min(start + width * 2, source.Length);
                var a = start; var b = middle;
                for (var output = start; output < end; output++) {
                    budget.Work();
                    var takeFirst = b >= end || a < middle && Before(source[a], source[b]);
                    target[output] = takeFirst ? source[a++] : source[b++];
                }
            }
            var swap = source; source = target; target = swap;
        }
        return source;
        bool Before(Item a, Item b) {
            var compare = (horizontal[a.Low] + a.Left).CompareTo(horizontal[b.Low] + b.Left);
            return compare < 0 || compare == 0 && a.Order < b.Order;
        }
    }

    private int[] Preferences(Item first, Item second) {
        budget.Work(4);
        var dx = second.PreferredX - first.PreferredX; var dy = second.PreferredY - first.PreferredY;
        var horizontal = dx >= 0 ? 0 : 1; var vertical = dy >= 0 ? 2 : 3;
        return Math.Abs(dx) >= Math.Abs(dy)
            ? new[] { horizontal, vertical, vertical == 2 ? 3 : 2, horizontal == 0 ? 1 : 0 }
            : new[] { vertical, horizontal, horizontal == 0 ? 1 : 0, vertical == 2 ? 3 : 2 };
    }

    private void Commit(double[] horizontal, double[] vertical) {
        foreach (var item in items) {
            budget.Work();
            if (item.Node != null) { item.Node.X = horizontal[item.Low] - item.Node.Width / 2; item.Node.Y = vertical[item.Low] - item.Node.Height / 2; }
            else {
                var group = item.Group!;
                group.X = horizontal[item.Low]; group.Y = vertical[item.Low];
                group.Width = horizontal[item.High] - group.X; group.Height = vertical[item.High] - group.Y;
            }
        }
    }
}

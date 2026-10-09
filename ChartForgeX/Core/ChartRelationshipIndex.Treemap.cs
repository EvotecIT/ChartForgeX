using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

internal sealed partial class ChartRelationshipIndex {
    internal static ChartRelationshipIndex Treemap(IEnumerable<ChartTreemapItem> items) {
        if (items == null) throw new ArgumentNullException(nameof(items));
        var authored = items.ToArray();
        if (authored.Length == 0) throw new ArgumentException("Treemaps require at least one item.", nameof(items));
        var nodes = SnapshotNodes(authored.Select(item => item.Node), out var indexes, nameof(items));
        var sources = new List<int>(); var targets = new List<int>();
        foreach (var item in authored) if (item.ParentId != null) {
            var edge = Resolve(indexes, item.ParentId, item.Id, nameof(items));
            sources.Add(edge.Source); targets.Add(edge.Target);
        }
        var from = sources.ToArray(); var to = targets.ToArray();
        var order = TopologicalOrder(nodes.Length, from, to, true, out var root, out var depths, allowForest: true, parameter: nameof(items));
        var children = ChildrenFor(nodes.Length, from, to);
        var values = new double[nodes.Length];
        for (var i = 0; i < authored.Length; i++) {
            if (children[i].Count == 0) {
                if (!authored[i].Value.HasValue) throw new ArgumentException("Treemap leaves require a finite non-negative size.", nameof(items));
                values[i] = authored[i].Value!.Value;
            } else if (authored[i].Value.HasValue) throw new ArgumentException("Treemap groups require a null size; their areas aggregate leaf sizes.", nameof(items));
        }
        AggregateLeaves(values, from, to, order, nameof(items));
        var total = 0d;
        foreach (var index in order) if (authored[index].ParentId == null) total += values[index];
        if (double.IsInfinity(total)) throw new ArgumentException("Treemap forest leaf aggregates must remain finite.", nameof(items));
        return new ChartRelationshipIndex(nodes, Array.Empty<ChartFlowLink>(), Array.Empty<ChartTreeLink>(), indexes, from, to, root, values, treemapItems: authored, depths: depths);
    }
}

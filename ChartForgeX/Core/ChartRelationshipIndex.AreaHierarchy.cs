using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal sealed partial class ChartRelationshipIndex {
    internal static ChartRelationshipIndex AreaHierarchy(IEnumerable<ChartHierarchyItem> items, bool allowForest, bool requireNullGroups) {
        if (items == null) throw new ArgumentNullException(nameof(items));
        var authored = items.ToArray();
        if (authored.Length == 0) throw new ArgumentException("Area hierarchies require at least one item.", nameof(items));
        var nodes = SnapshotNodes(authored.Select(item => item.Node), out var indexes, nameof(items));
        var sources = new List<int>(); var targets = new List<int>();
        foreach (var item in authored) if (item.ParentId != null) {
            var edge = Resolve(indexes, item.ParentId, item.Id, nameof(items));
            sources.Add(edge.Source); targets.Add(edge.Target);
        }
        var from = sources.ToArray(); var to = targets.ToArray();
        var order = TopologicalOrder(nodes.Length, from, to, true, out var root, out var depths, allowForest: allowForest, parameter: nameof(items));
        var children = ChildrenFor(nodes.Length, from, to);
        var values = new double[nodes.Length];
        for (var i = 0; i < authored.Length; i++) {
            if (children[i].Count == 0) {
                if (!authored[i].Value.HasValue) throw new ArgumentException("Hierarchy leaves require a finite non-negative size.", nameof(items));
                values[i] = authored[i].Value!.Value;
            } else if (requireNullGroups && authored[i].Value.HasValue) throw new ArgumentException("Treemap groups require a null size; their areas aggregate leaf sizes.", nameof(items));
        }
        AggregateLeaves(values, from, to, order, nameof(items));
        var total = 0d;
        foreach (var index in order) if (authored[index].ParentId == null) total += values[index];
        if (double.IsInfinity(total)) throw new ArgumentException("Hierarchy forest leaf aggregates must remain finite.", nameof(items));
        return new ChartRelationshipIndex(nodes, Array.Empty<ChartFlowLink>(), Array.Empty<ChartTreeLink>(), indexes, from, to, root, values,
            hierarchyItems: authored, depths: depths, hierarchyOrder: order);
    }

    /// <summary>Resolves geometry values from immutable facts without changing the authored snapshot or earlier preparations.</summary>
    internal IReadOnlyList<double> ResolveHierarchyValues(ChartHierarchyValuePolicy policy) {
        if (policy == ChartHierarchyValuePolicy.LeafAggregate) return HierarchyValues;
        if (policy != ChartHierarchyValuePolicy.AuthoredTotal) throw new ArgumentOutOfRangeException(nameof(policy));
        var values = new double[HierarchyItems.Count];
        for (var i = _hierarchyOrder.Length - 1; i >= 0; i--) {
            var node = _hierarchyOrder[i];
            var children = Children(node);
            if (children.Count == 0) { values[node] = HierarchyItems[node].Value!.Value; continue; }
            var total = 0d;
            foreach (var child in children) total += values[child];
            if (double.IsInfinity(total)) throw new ArgumentException("Resolved hierarchy child totals must remain finite.", "items");
            var authored = HierarchyItems[node].Value;
            // ULP proximity alone is too permissive for subnormal values: two minimum
            // positive doubles still cannot fit an authored total of one such double.
            var closes = authored.HasValue && total > authored.Value && authored.Value > 0 && ChartMath.SameCoordinate(total, authored.Value)
                && (total - authored.Value) / total <= 8.881784197001252e-16;
            if (authored.HasValue && total > authored.Value && !closes)
                throw new ArgumentException("Authored group total for '" + HierarchyItems[node].Id + "' is smaller than its resolved child total.", "items");
            values[node] = authored ?? total;
        }
        return Array.AsReadOnly(values);
    }
}

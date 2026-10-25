using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>Retains one full raw source and derives only the detached visible hierarchy.</summary>
internal sealed class TopologyHierarchyPreparation {
    private readonly TopologyChart _source;
    internal TopologyHierarchyPreparation(TopologyChart source, TopologyHierarchyView view) {
        _source = TopologyLayoutEngine.Clone(source); _source.HierarchyPreparation = null;
        View = view;
    }
    internal TopologyHierarchyView View { get; }
    internal int SourceNodeCount => _source.Nodes.Count;
    internal TopologyChart CopySource() => TopologyLayoutEngine.Clone(_source);
    internal IReadOnlyDictionary<string, IReadOnlyList<string>> Contributors { get; private set; } =
        new ReadOnlyDictionary<string, IReadOnlyList<string>>(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));

    internal static void Apply(TopologyChart working, TopologyChart source, TopologyHierarchyView view, TopologyRenderOptions options) {
        if (source.LayoutMode != TopologyLayoutMode.Layered) throw new NotSupportedException("Hierarchy views require an existing layered hierarchy.");
        var validity = new TopologyChartValidator().ValidateAuthoredHierarchySource(source);
        if (!validity.IsValid) throw new TopologyValidationException(validity);
        var hierarchy = source.Nodes.Where(node => node.Metadata.ContainsKey("hierarchy.level")).ToArray();
        if (hierarchy.Length == 0) throw new NotSupportedException("Hierarchy views require nodes created from an explicit hierarchy.");
        var hierarchyIds = new HashSet<string>(hierarchy.Select(node => node.Id), StringComparer.Ordinal);
        foreach (var id in view.CollapsedNodeIds.Concat(view.NodeOrder))
            if (!hierarchyIds.Contains(id)) throw new ArgumentException("Hierarchy view node '" + id + "' is not an existing hierarchy ID.", nameof(view));
        var parents = new Dictionary<string, string?>(StringComparer.Ordinal);
        var children = hierarchy.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var node in hierarchy) {
            var parent = node.Metadata.TryGetValue("hierarchy.parentId", out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
            // AddHierarchy may already have excluded ancestor context. The retained source starts at that boundary;
            // keep its raw parent fact while treating an absent ancestor as outside this captured projection.
            if (parent != null && !hierarchyIds.Contains(parent)) parent = null;
            parents.Add(node.Id, parent); if (parent != null) children[parent].Add(node.Id);
        }
        ValidateParents(parents);
        var collapsed = new HashSet<string>(view.CollapsedNodeIds, StringComparer.Ordinal);
        var hidden = new HashSet<string>(StringComparer.Ordinal);
        var contributors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var pending = new Stack<(string Id, string? Owner)>();
        for (var index = hierarchy.Length - 1; index >= 0; index--) {
            var node = hierarchy[index];
            if (parents[node.Id] == null) pending.Push((node.Id, null));
        }
        while (pending.Count > 0) {
            var next = pending.Pop(); var owner = next.Owner;
            if (owner != null) { hidden.Add(next.Id); contributors[owner].Add(next.Id); }
            else if (collapsed.Contains(next.Id)) { owner = next.Id; contributors.Add(next.Id, new List<string>()); }
            var descendants = children[next.Id];
            for (var index = descendants.Count - 1; index >= 0; index--) pending.Push((descendants[index], owner));
        }
        // All source/view validation above precedes changes to the detached working layout.
        var preparation = new TopologyHierarchyPreparation(source, view);
        preparation.Contributors = new ReadOnlyDictionary<string, IReadOnlyList<string>>(contributors.ToDictionary(pair => pair.Key,
            pair => (IReadOnlyList<string>)Array.AsReadOnly(pair.Value.ToArray()), StringComparer.Ordinal));
        working.Nodes.RemoveAll(node => hidden.Contains(node.Id));
        working.Edges.RemoveAll(edge => hidden.Contains(edge.SourceNodeId) || hidden.Contains(edge.TargetNodeId));
        working.Annotations.RemoveAll(note => note.TargetId != null && hidden.Contains(note.TargetId));
        foreach (var node in working.Nodes) {
            if (!contributors.TryGetValue(node.Id, out var ids)) continue;
            node.Metadata["hierarchy.collapsed"] = "true";
            node.Metadata["hierarchy.hiddenCount"] = ids.Count.ToString(CultureInfo.InvariantCulture);
            if (ids.Count == 0) continue;
            var mode = node.DisplayMode ?? options.NodeDisplayMode;
            if (mode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard) {
                var detail = new TopologyNodeDetail { Label = source.Labels.HiddenDescendants, Value = ids.Count.ToString(CultureInfo.InvariantCulture) };
                detail.Metadata["hierarchy.summary"] = "descendants"; node.Details.Add(detail);
            }
        }
        if (view.LayoutDirection.HasValue) working.LayoutDirection = view.LayoutDirection.Value;
        working.HierarchyPreparation = preparation;
    }

    private static void ValidateParents(IReadOnlyDictionary<string, string?> parents) {
        var complete = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in parents.Keys) {
            if (complete.Contains(id)) continue;
            var chain = new HashSet<string>(StringComparer.Ordinal); var path = new List<string>(); string? current = id;
            while (current != null && !complete.Contains(current)) {
                if (!chain.Add(current)) throw new InvalidOperationException("Hierarchy parent relationships contain a cycle.");
                path.Add(current); current = parents[current];
            }
            foreach (var member in path) complete.Add(member);
        }
    }
}

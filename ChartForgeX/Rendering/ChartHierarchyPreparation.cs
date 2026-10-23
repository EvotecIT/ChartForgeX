using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves a transient view against one immutable original relationship index.</summary>
internal sealed class ChartHierarchyPreparation {
    private readonly ChartRelationshipIndex _source;
    private readonly bool[]? _included;
    private readonly string?[]? _branches;
    internal ChartHierarchyView? Request { get; }
    internal ChartSeriesKind Kind { get; }
    internal string SeriesKey { get; }
    internal int RootIndex { get; }
    internal IReadOnlyList<int> Roots { get; }
    internal IReadOnlyList<ChartNode> Path { get; }
    internal bool Contains(int index) => _included == null || _included[index];
    internal int Depth(int index) => _source.Depths[index] - (Request == null ? 0 : _source.Depths[RootIndex]);
    internal string? BranchRoot(int index) {
        if (Request == null) return _source.HierarchyBranchRoot(index);
        return _branches![index];
    }
    private ChartHierarchyPreparation(ChartSeries series, ChartHierarchyView? request) {
        _source = series.Relationships!; Request = request; Kind = series.Kind; SeriesKey = series.InteractionIdentityKey;
        RootIndex = request == null ? _source.Root : Find(request.RootId);
        Roots = request == null ? _source.Roots : Array.AsReadOnly(new[] { RootIndex });
        if (request == null) { Path = Array.Empty<ChartNode>(); return; }
        _included = new bool[_source.Nodes.Count]; _branches = new string?[_source.Nodes.Count];
        var pending = new Stack<int>(Roots);
        while (pending.Count > 0) {
            var index = pending.Pop(); _included[index] = true;
            foreach (var child in _source.Children(index)) {
                _branches[child] = index == RootIndex ? _source.Nodes[child].Id : _branches[index];
                pending.Push(child);
            }
        }
        var path = new List<ChartNode>();
        if (request != null) for (var index = RootIndex; index >= 0; index = _source.Parent(index)) path.Add(_source.Nodes[index]);
        path.Reverse(); Path = path.AsReadOnly();
    }
    private int Find(string id) {
        if (_source.TryNodeIndex(id, out var index)) return index;
        throw new ArgumentException("The hierarchy has no item with identity '" + id + "'.", "view");
    }
    internal static ChartHierarchyPreparation? Resolve(Chart chart, ChartHierarchyView? request, bool required) {
        var supported = chart.Series.Count == 1 && (chart.Series[0].Kind is ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst);
        if (!supported) {
            if (required || request != null) throw new NotSupportedException("Hierarchy views require one Treemap or Sunburst series.");
            return null;
        }
        var series = chart.Series[0];
        if (series.Relationships == null || series.HierarchyItems.Count == 0) {
            if (request != null) throw new ArgumentException("An empty hierarchy has no requested root.", "view");
            return null;
        }
        return new ChartHierarchyPreparation(series, request);
    }
}

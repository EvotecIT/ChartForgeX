using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves a transient view against one immutable original relationship index.</summary>
internal sealed class ChartHierarchyPreparation {
    private readonly ChartRelationshipIndex _source;
    private readonly bool[]? _included;
    private readonly string?[]? _branches;
    private readonly bool[]? _rootIncluded;
    private readonly bool[]? _blocked;
    internal bool VisibilityBlocked(int index) => !WithinRoot(index) || (_blocked != null && _blocked[index]);
    internal IReadOnlyList<double> Values { get; }
    internal bool WithinRoot(int index) => _rootIncluded == null || _rootIncluded[index];
    internal bool HasHiddenMembership { get; }
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
    private ChartHierarchyPreparation(Chart chart, ChartHierarchyView? request, ChartVisibilityView? visibility) {
        var series = chart.Series[0];
        _source = series.Relationships!; Request = request; Kind = series.Kind; SeriesKey = series.InteractionIdentityKey;
        RootIndex = request == null ? _source.Root : Find(request.RootId);
        Roots = request == null ? _source.Roots : Array.AsReadOnly(new[] { RootIndex });
        var policy = series.Kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.ParentValuePolicy : ChartHierarchyValuePolicy.LeafAggregate;
        // Validate the full original source before any view can exclude a child or authored total.
        var fullValues = _source.ResolveHierarchyValues(policy);
        HasHiddenMembership = visibility != null && (visibility.IsHidden(series.InteractionIdentityKey) || visibility.HiddenTargets.Count > 0);
        if (request == null && !HasHiddenMembership) { Path = Array.Empty<ChartNode>(); Values = fullValues; return; }
        _included = new bool[_source.Nodes.Count]; _branches = new string?[_source.Nodes.Count];
        var pending = new Stack<int>(Roots);
        while (pending.Count > 0) {
            var index = pending.Pop(); _included[index] = true;
            foreach (var child in _source.Children(index)) {
                _branches[child] = index == RootIndex ? _source.Nodes[child].Id : _branches[index];
                pending.Push(child);
            }
        }
        _rootIncluded = (bool[])_included.Clone();
        if (visibility != null) {
            _blocked = new bool[_source.Nodes.Count];
            foreach (var target in visibility.HiddenTargets) {
                if (target.SeriesId != series.Id || target.Kind is not (ChartVisibilityTargetKind.Series or ChartVisibilityTargetKind.HierarchyItem))
                    throw new ArgumentException("Hierarchy membership requires exact authored series/node targets.", nameof(visibility));
                if (target.Kind == ChartVisibilityTargetKind.Series) foreach (var root in _source.Roots) _blocked[root] = true;
                var hidden = target.Kind == ChartVisibilityTargetKind.Series ? new Stack<int>(_source.Roots) : new Stack<int>(new[] { Find(target.NodeId!) });
                while (hidden.Count > 0) { var index = hidden.Pop(); _included[index] = false; foreach (var child in _source.Children(index)) { _blocked[child] = true; hidden.Push(child); } }
            }
        }
        Values = HasHiddenMembership ? _source.ResolveHierarchyValues(policy, Contains) : fullValues;
        var path = new List<ChartNode>();
        if (request != null) for (var index = RootIndex; index >= 0; index = _source.Parent(index)) path.Add(_source.Nodes[index]);
        path.Reverse(); Path = path.AsReadOnly();
    }
    private int Find(string id) {
        if (_source.TryNodeIndex(id, out var index)) return index;
        throw new ArgumentException("The hierarchy has no item with identity '" + id + "'.", "view");
    }
    internal static ChartHierarchyPreparation? Resolve(Chart chart, ChartHierarchyView? request, bool required, ChartVisibilityView? visibility = null) {
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
        return new ChartHierarchyPreparation(chart, request, visibility);
    }
}

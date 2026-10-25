using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

internal sealed partial class ChartVisibilitySource {
    internal IReadOnlyList<ChartVisibilityTarget> TargetCatalog() => _targets;
    internal bool TryTarget(ChartVisibilityTargetKind kind, string series, string? observation, out ChartVisibilityTarget? target) => _targetIndex.TryGetValue((kind, series, observation), out target);
    private IReadOnlyList<ChartVisibilityTarget> CaptureTargetCatalog() {
        var targets = new List<ChartVisibilityTarget>();
        foreach (var series in _series) {
            targets.Add(ChartVisibilityTarget.Series(series.Id));
            if (_radialOther.TryGetValue(series.Id.Value, out var members) && members.Count > 0) targets.Add(ChartVisibilityTarget.RadialOther(series.Id));
            if (series.IsHierarchy) { foreach (var item in series.HierarchyItems) targets.Add(ChartVisibilityTarget.HierarchyItem(series.Id, item.Id)); continue; }
            var ids = series.Raw ? series.GroupIds : series.ObservationIds;
            foreach (var pair in ids.OrderBy(pair => pair.Key)) if (pair.Key >= 0 && pair.Key < series.DisplayCount)
                targets.Add(series.Raw ? ChartVisibilityTarget.BoxPlotGroup(series.Id, pair.Value) : ChartVisibilityTarget.Observation(series.Id, pair.Value));
        }
        return Array.AsReadOnly(targets.ToArray());
    }
    internal void ValidateView(ChartVisibilityView view) {
        if (view.Layout != ChartVisibilityLayout.Reflow) throw new ArgumentException("A detached source view requires explicit Reflow.", nameof(view));
        if (view.HierarchyView != null && (_hierarchy == null || !_series[0].HierarchyItems.Any(item => item.Id == view.HierarchyView.RootId)))
            throw new ArgumentException("The full source hierarchy has no requested root.", nameof(view));
        if (_hierarchy != null && view.LabelLayering != ChartVisibilityLabelLayering.PreserveOverlay)
            throw new NotSupportedException("Hierarchy Reflow retains native per-node label ownership; GroupWithSeries is a Cartesian policy.");
        foreach (var hidden in view.HiddenTargets) if (!_targetIndex.ContainsKey((hidden.Kind, hidden.SeriesId.Value, hidden.Identity)))
            throw new ArgumentException("The native Reflow view references an unknown or unsupported authored source target; only captured authored identities belong to this native membership catalog.", nameof(view));
        if (_presentation.ShowPointLegend && _series.Count == 1 && !_series[0].IsHierarchy && _series[0].ShowInLegend && ChartSeriesKindTraits.SupportsPointLegend(_series[0].Kind)) {
            var series = _series[0]; var ids = series.Raw ? series.GroupIds : series.ObservationIds;
            if (Enumerable.Range(0, series.DisplayCount).Any(index => !ids.ContainsKey(index)))
                throw new NotSupportedException("Native point/group legend Reflow requires an authored identity for every retained legend target.");
        }
    }
    internal IReadOnlyList<int> RenderedSourceIndices(int series, ChartVisibilityView view) =>
        _series[series].Raw || _series[series].IsHierarchy ? Array.Empty<int>() : VisibleIndices(_series[series], view);
    internal IReadOnlyList<int> RadialOtherMembers(string series) => _radialOther.TryGetValue(series, out var members) ? members : Array.Empty<int>();
    private IReadOnlyList<int> VisibleIndices(Series series, ChartVisibilityView? view) {
        var selected = series.VisibleIndices(view);
        if (view == null || !view.IsHidden(ChartVisibilityTargetKind.RadialOther, series.Id)) return selected;
        if (!_radialOtherIndex.TryGetValue(series.Id.Value, out var excluded)) return selected;
        return Array.AsReadOnly(selected.Where(index => !excluded.Contains(index)).ToArray());
    }
    internal static bool SameTarget(ChartVisibilityTarget first, ChartVisibilityTarget second) =>
        first.Kind == second.Kind && first.SeriesId == second.SeriesId && first.ObservationId == second.ObservationId && first.NodeId == second.NodeId;
}

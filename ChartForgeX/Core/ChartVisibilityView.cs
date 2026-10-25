using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>Native ownership of series labels in a retained full-data visibility view.</summary>
public enum ChartVisibilityLabelLayering {
    /// <summary>Keeps global label overlay order. Enabled series labels require explicit grouping for partial visibility motion.</summary>
    PreserveOverlay = 0,
    /// <summary>Groups each series plot and its already placed labels into one native composite, changing inter-series/context paint order.</summary>
    GroupWithSeries = 1
}

/// <summary>An immutable absolute native visibility view with explicit layout and label ownership.</summary>
/// <remarks>Preserve retains full-source Line/Bar domains and layout. Reflow prepares detached visible Line/Bar
/// observations or supplied/raw BoxPlot groups through their native domain, slot and statistics owners. The full
/// source catalog and legend remain retained. Pie/Donut Other uses retained contributor membership; authored Treemap/Sunburst items use their actual subtree owner. Raw samples are not group targets.</remarks>
public sealed class ChartVisibilityView {
    private readonly HashSet<string> _hidden;
    private readonly HashSet<(ChartVisibilityTargetKind Kind, string Series, string? Observation)> _hiddenTargets;
    /// <summary>Creates a native visibility view.</summary>
    /// <param name="hiddenSeries">Authored series to hide; null or empty keeps every source series visible.</param>
    /// <param name="labelLayering">Retain global overlay by default, or explicitly group existing placed labels with their series.</param>
    public ChartVisibilityView(IEnumerable<ChartSeriesId>? hiddenSeries = null,
        ChartVisibilityLabelLayering labelLayering = ChartVisibilityLabelLayering.PreserveOverlay)
        : this(hiddenSeries, labelLayering, ChartVisibilityLayout.Preserve) { }

    /// <summary>Creates a whole-series native view with explicit Preserve or Reflow layout.</summary>
    public ChartVisibilityView(IEnumerable<ChartSeriesId>? hiddenSeries, ChartVisibilityLabelLayering labelLayering, ChartVisibilityLayout layout)
        : this((hiddenSeries ?? Array.Empty<ChartSeriesId>()).Select(ChartVisibilityTarget.Series), layout, labelLayering) { }

    /// <summary>Creates an absolute view over real authored series, observations or raw BoxPlot groups.</summary>
    /// <param name="hiddenTargets">Detached source identities to hide. Different series can reuse an observation name.</param>
    /// <param name="layout">Preserve native full-source domains/layout, or explicitly recompute supported visible-source layout.</param>
    /// <param name="labelLayering">The incumbent native label ownership policy; defaults remain unchanged.</param>
    public ChartVisibilityView(IEnumerable<ChartVisibilityTarget> hiddenTargets, ChartVisibilityLayout layout,
        ChartVisibilityLabelLayering labelLayering = ChartVisibilityLabelLayering.PreserveOverlay)
        : this(hiddenTargets, layout, labelLayering, null) { }

    /// <summary>Combines absolute hierarchy membership with an existing native source root. Null keeps the full source root.</summary>
    /// <remarks>Hierarchy roots require Reflow. Hiding the requested root yields the native no-data view; it does not select another root.</remarks>
    public ChartVisibilityView(IEnumerable<ChartVisibilityTarget> hiddenTargets, ChartVisibilityLayout layout,
        ChartVisibilityLabelLayering labelLayering, ChartHierarchyView? hierarchyView) {
        if (hierarchyView != null && layout != ChartVisibilityLayout.Reflow) throw new ArgumentException("A hierarchy root and membership require explicit Reflow.", nameof(layout));
        HierarchyView = hierarchyView == null ? null : new ChartHierarchyView(hierarchyView.RootId);
        if (hiddenTargets == null) throw new ArgumentNullException(nameof(hiddenTargets));
        if (!Enum.IsDefined(typeof(ChartVisibilityLabelLayering), labelLayering)) throw new ArgumentOutOfRangeException(nameof(labelLayering));
        if (!Enum.IsDefined(typeof(ChartVisibilityLayout), layout)) throw new ArgumentOutOfRangeException(nameof(layout));
        var targets = hiddenTargets.ToArray(); var distinct = new HashSet<(ChartVisibilityTargetKind Kind, string Series, string? Observation)>();
        foreach (var target in targets) {
            if (target == null) throw new ArgumentException("A native visibility target cannot be null.", nameof(hiddenTargets));
            if (!distinct.Add((target.Kind, target.SeriesId.Value, target.Identity)))
                throw new ArgumentException("A visibility view cannot repeat a typed source identity.", nameof(hiddenTargets));
        }
        _hiddenTargets = distinct; HiddenTargets = Array.AsReadOnly(targets); HiddenSeries = Array.AsReadOnly(targets.Where(target => target.Kind == ChartVisibilityTargetKind.Series).Select(target => target.SeriesId).ToArray());
        _hidden = new HashSet<string>(HiddenSeries.Select(id => id.Value), StringComparer.Ordinal); LabelLayering = labelLayering; Layout = layout;
    }
    /// <summary>Gets every detached typed source identity hidden by this absolute view.</summary>
    public IReadOnlyList<ChartVisibilityTarget> HiddenTargets { get; }
    /// <summary>Gets the detached native hierarchy root, or null for the complete original hierarchy.</summary>
    public ChartHierarchyView? HierarchyView { get; }
    internal bool IsHiddenHierarchy(ChartSeriesId series, string nodeId) => _hiddenTargets.Contains((ChartVisibilityTargetKind.HierarchyItem, series.Value, nodeId));
    /// <summary>Gets the explicit native layout/domain policy.</summary>
    public ChartVisibilityLayout Layout { get; }
    /// <summary>Gets the detached authored identities hidden by this absolute view.</summary>
    public IReadOnlyList<ChartSeriesId> HiddenSeries { get; }
    /// <summary>Gets the explicit native series label paint policy.</summary>
    public ChartVisibilityLabelLayering LabelLayering { get; }
    internal bool IsHidden(ChartVisibilityTargetKind kind, ChartSeriesId series) => _hiddenTargets.Contains((kind, series.Value, null));
    internal bool IsHidden(ChartVisibilityTarget target) => _hiddenTargets.Contains((target.Kind, target.SeriesId.Value, target.Identity));
    internal bool IsHidden(ChartVisibilityTargetKind kind, ChartSeriesId series, ChartObservationId observation) => _hiddenTargets.Contains((kind, series.Value, observation.Value));
    internal bool IsHidden(string seriesKey) => _hidden.Contains(seriesKey);
}

using System;

namespace ChartForgeX.Core;

/// <summary>The real authored source unit selected by a native visibility view.</summary>
public enum ChartVisibilityTargetKind {
    /// <summary>One complete authored series.</summary>
    Series = 0,
    /// <summary>One real authored observation, independently of its display position.</summary>
    Observation = 1,
    /// <summary>One authored raw BoxPlot group, independently of its raw sample observation identities.</summary>
    BoxPlotGroup = 2,
    /// <summary>The actual Other contributor set of the complete captured Pie/Donut source.</summary>
    RadialOther = 3,
    /// <summary>One authored hierarchy item and its actual descendant subtree.</summary>
    HierarchyItem = 4
}

/// <summary>An immutable source visibility selector. RadialOther uses captured native contributor membership; HierarchyItem uses the native authored subtree owner.</summary>
public sealed class ChartVisibilityTarget {
    private ChartVisibilityTarget(ChartVisibilityTargetKind kind, ChartSeriesId series, ChartObservationId? observation, string? nodeId = null) {
        series.Validate(); if (series.IsAutomatic) throw new ArgumentException("Native visibility requires an authored series identity.", nameof(series));
        if (observation.HasValue) _ = observation.Value.Value;
        if (kind == ChartVisibilityTargetKind.HierarchyItem && (string.IsNullOrWhiteSpace(nodeId) || observation.HasValue)) throw new ArgumentException("A hierarchy target requires its authored node ID and no observation ID.", nameof(nodeId));
        Kind = kind; SeriesId = series; ObservationId = observation; NodeId = nodeId;
    }
    /// <summary>Gets the explicit source unit; raw BoxPlot groups are never ordinary sample observations.</summary>
    public ChartVisibilityTargetKind Kind { get; }
    /// <summary>Gets the authored series identity.</summary>
    public ChartSeriesId SeriesId { get; }
    /// <summary>Gets the authored observation or raw BoxPlot-group identity; null for a whole series, captured RadialOther group, or hierarchy item.</summary>
    public ChartObservationId? ObservationId { get; }
    /// <summary>Gets the authored hierarchy item ID; null for every other target kind.</summary>
    public string? NodeId { get; }
    internal string? Identity => NodeId ?? ObservationId?.Value;
    /// <summary>Selects an authored hierarchy item and its actual descendants, preserving their source identities.</summary>
    public static ChartVisibilityTarget HierarchyItem(ChartSeriesId series, string nodeId) => new(ChartVisibilityTargetKind.HierarchyItem, series, null, nodeId);
    /// <summary>Selects one complete authored series.</summary>
    public static ChartVisibilityTarget Series(ChartSeriesId series) => new(ChartVisibilityTargetKind.Series, series, null);
    /// <summary>Selects one real authored observation in its series.</summary>
    public static ChartVisibilityTarget Observation(ChartSeriesId series, ChartObservationId observation) => new(ChartVisibilityTargetKind.Observation, series, observation);
    /// <summary>Selects the complete captured source series' actual native Other group. Reflow may derive a different visible Other; this target keeps its original contributors.</summary>
    public static ChartVisibilityTarget RadialOther(ChartSeriesId series) => new(ChartVisibilityTargetKind.RadialOther, series, null);
    /// <summary>Selects an authored raw group from the existing BoxPlotGroupIds namespace.</summary>
    public static ChartVisibilityTarget BoxPlotGroup(ChartSeriesId series, ChartObservationId group) => new(ChartVisibilityTargetKind.BoxPlotGroup, series, group);
}

/// <summary>How a visibility view obtains native domains and layout.</summary>
public enum ChartVisibilityLayout {
    /// <summary>Retains the complete source domains/layout and captured full catalog.</summary>
    Preserve = 0,
    /// <summary>Recomputes supported visible data through the existing native domain/slot/statistics/family owners.</summary>
    Reflow = 1
}

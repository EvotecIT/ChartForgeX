using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private readonly List<GanttSeriesDependency> _ganttDependencies = new();

    /// <summary>Gets a detached, ordered snapshot of authored links identified by stable series IDs, including links whose data is temporarily empty.</summary>
    public IReadOnlyList<ChartGanttDependency> GanttDependencies {
        get {
            var result = new List<ChartGanttDependency>(_ganttDependencies.Count);
            foreach (var link in _ganttDependencies) result.Add(new ChartGanttDependency(link.Predecessor.Id, link.Successor.Id));
            return result.AsReadOnly();
        }
    }

    /// <summary>Adds a finish-to-start visual link after both Gantt tasks have been added.</summary>
    /// <remarks>Both handles must belong to this chart. Reordering retains the relationship; removing either series removes it.
    /// Temporarily clearing observations suppresses the connector until both endpoints have data again. Links involving a
    /// vertical marker retain metadata without painting a row connector. A task can have several predecessors. Links do not reschedule dates.</remarks>
    public Chart AddGanttDependency(ChartSeries predecessor, ChartSeries successor) {
        EnsureCanAddSeries();
        ValidateGanttEndpoint(predecessor, nameof(predecessor)); ValidateGanttEndpoint(successor, nameof(successor));
        RequireGanttDependencyRows();
        if (ReferenceEquals(predecessor, successor)) throw new ArgumentException("A Gantt dependency requires two distinct series.", nameof(successor));
        foreach (var link in _ganttDependencies)
            if (ReferenceEquals(link.Predecessor, predecessor) && ReferenceEquals(link.Successor, successor)) return this;
        _ganttDependencies.Add(new GanttSeriesDependency(predecessor, successor));
        return this;
    }

    internal List<ChartGanttOrdinalDependency> ResolveGanttDependencies() {
        var result = new List<ChartGanttOrdinalDependency>(_ganttDependencies.Count);
        if (_ganttDependencies.Count > 0) RequireGanttDependencyRows();
        var indices = new Dictionary<ChartSeries, int>();
        for (var index = 0; index < Series.Count; index++) indices.Add(Series[index], index);
        foreach (var link in _ganttDependencies) {
            if (!indices.TryGetValue(link.Predecessor, out var predecessor) || !indices.TryGetValue(link.Successor, out var successor))
                throw new InvalidOperationException("Gantt dependency endpoints must remain attached to their chart.");
            result.Add(new ChartGanttOrdinalDependency(predecessor, successor));
        }
        return result;
    }

    private void ValidateGanttEndpoint(ChartSeries series, string parameterName) {
        if (series == null) throw new ArgumentNullException(parameterName);
        if (!ReferenceEquals(series.Owner, this)) throw new ArgumentException("A Gantt endpoint must be attached to this chart.", parameterName);
        if (series.Kind != ChartSeriesKind.Gantt) throw new ArgumentException("A Gantt endpoint must be a task, milestone or marker series.", parameterName);
    }

    private void RemoveGanttDependencies(ChartSeries series) => _ganttDependencies.RemoveAll(link => ReferenceEquals(link.Predecessor, series) || ReferenceEquals(link.Successor, series));

    private readonly struct GanttSeriesDependency {
        internal GanttSeriesDependency(ChartSeries predecessor, ChartSeries successor) { Predecessor = predecessor; Successor = successor; }
        internal ChartSeries Predecessor { get; }
        internal ChartSeries Successor { get; }
    }

    internal void ValidateExplicitGanttDependencies() {
        if (_ganttDependencies.Count > 0) ResolveGanttDependencies();
    }

    private void RequireGanttDependencyRows() {
        foreach (var series in Series) if (series.Kind != ChartSeriesKind.Gantt)
            throw new InvalidOperationException("Gantt dependency links require a chart containing only Gantt tasks.");
    }
}

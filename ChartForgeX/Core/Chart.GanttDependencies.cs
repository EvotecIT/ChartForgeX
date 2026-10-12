using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private readonly List<ChartGanttDependency> _ganttDependencies = new();
    private readonly HashSet<ChartGanttDependency> _ganttDependencySet = new();

    /// <summary>Gets a detached, ordered snapshot of Gantt links, including dependencies supplied when adding tasks.</summary>
    public IReadOnlyList<ChartGanttDependency> GanttDependencies => ResolveGanttDependencies().AsReadOnly();

    /// <summary>Adds a finish-to-start visual link after both Gantt tasks have been added.</summary>
    /// <remarks>Indices address Gantt series, including vertical markers, in their authored order. Links involving a
    /// vertical marker remain available as metadata but do not paint a row connector. A successor can precede its predecessor visually,
    /// and a task can have several predecessors. The link does not reschedule task dates. Repeated links render once.</remarks>
    public Chart AddGanttDependency(int predecessorIndex, int successorIndex) {
        RequireGanttDependencyRows();
        if (predecessorIndex < 0 || predecessorIndex >= Series.Count) throw new ArgumentOutOfRangeException(nameof(predecessorIndex));
        if (successorIndex < 0 || successorIndex >= Series.Count) throw new ArgumentOutOfRangeException(nameof(successorIndex));
        var link = new ChartGanttDependency(predecessorIndex, successorIndex);
        if (_ganttDependencySet.Add(link)) _ganttDependencies.Add(link);
        return this;
    }

    internal List<ChartGanttDependency> ResolveGanttDependencies() {
        var result = new List<ChartGanttDependency>();
        var seen = new HashSet<ChartGanttDependency>();
        if (_ganttDependencies.Count > 0) RequireGanttDependencyRows();
        var taskIndex = 0;
        foreach (var series in Series) {
            if (series.Kind != ChartSeriesKind.Gantt) continue;
            if (series.Points.Count < 2) throw new InvalidOperationException("Gantt dependency metadata is missing.");
            var predecessor = series.Points[1].Y;
            if (!ChartMath.IsFinite(predecessor) || predecessor < -1 || predecessor >= taskIndex || predecessor != Math.Truncate(predecessor))
                throw new InvalidOperationException("A task's inline Gantt dependency must reference an earlier task.");
            if (predecessor >= 0) {
                var link = new ChartGanttDependency((int)predecessor, taskIndex);
                if (seen.Add(link)) result.Add(link);
            }
            taskIndex++;
        }
        foreach (var link in _ganttDependencies) {
            if (link.PredecessorIndex >= taskIndex || link.SuccessorIndex >= taskIndex)
                throw new InvalidOperationException("Gantt dependency indices must reference existing tasks.");
            if (seen.Add(link)) result.Add(link);
        }
        return result;
    }

    internal void ValidateExplicitGanttDependencies() {
        if (_ganttDependencies.Count > 0) ResolveGanttDependencies();
    }

    private void RequireGanttDependencyRows() {
        foreach (var series in Series) if (series.Kind != ChartSeriesKind.Gantt)
            throw new InvalidOperationException("Gantt dependency links require a chart containing only Gantt tasks.");
    }
}

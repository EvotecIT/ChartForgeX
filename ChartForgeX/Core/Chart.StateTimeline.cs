using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Registers the categorical states used by state timeline lanes and categorical heatmap cells. States keep the given order in the legend,
    /// and each state's colour is used as supplied; state colours are never taken from the series palette.
    /// </summary>
    /// <param name="states">The states in legend order. Keys must be unique.</param>
    /// <returns>The current chart.</returns>
    public Chart WithStateCategories(IEnumerable<ChartStateCategory> states) {
        if (states == null) throw new ArgumentNullException(nameof(states));
        var materialized = new List<ChartStateCategory>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in states) {
            if (state == null) throw new ArgumentException("States must not contain null entries.", nameof(states));
            if (!keys.Add(state.Key)) throw new ArgumentException("State keys must be unique: " + state.Key, nameof(states));
            materialized.Add(state);
        }

        Options.StateCategories.Clear();
        Options.StateCategories.AddRange(materialized);
        return this;
    }

    /// <summary>Registers the categorical states used by state timelines and categorical heatmaps, in legend order.</summary>
    /// <param name="states">The states in legend order. Keys must be unique.</param>
    /// <returns>The current chart.</returns>
    public Chart WithStateCategories(params ChartStateCategory[] states) => WithStateCategories((IEnumerable<ChartStateCategory>)states);

    /// <summary>
    /// Adds one state timeline lane: an entity such as a server or service with its state intervals over time.
    /// Lanes render top to bottom in the order they are added. Time not covered by a segment is shown as a gap.
    /// </summary>
    /// <param name="name">The lane label.</param>
    /// <param name="segments">The lane's intervals. They are sorted by start, then end, then input order; overlaps are drawn
    /// in that order, and rendered point indices (<c>data-cfx-point</c>) follow the sorted order.</param>
    /// <param name="summary">Optional text for the summary column, for example <c>99.2%</c>.</param>
    /// <param name="group">Optional group the lane is listed under. Consecutive lanes with the same group share one
    /// header row; a lane without a group after a grouped one is set apart by a small gap. Group names are lane labels and are
    /// hidden with the y-axis.</param>
    /// <param name="id">Optional stable series identity. When omitted, the chart assigns a local ID.</param>
    /// <returns>The current chart.</returns>
    public Chart AddStateTimelineLane(string name, IEnumerable<ChartStateTimelineSegment> segments, string? summary = null, string? group = null, ChartSeriesId? id = null) {
        AppendSeries(id.HasValue ? ChartSeries.CreateStateTimelineLane(id.Value, name, segments, summary, group)
            : ChartSeries.CreateStateTimelineLane(name, segments, summary, group));
        return this;
    }
}

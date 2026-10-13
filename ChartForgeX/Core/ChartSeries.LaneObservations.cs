using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartStateTimelineSegment> _stateTimelineSegments = Array.Empty<ChartStateTimelineSegment>();
    /// <summary>Gets complete state intervals, including state keys and tooltip details. Numeric points are derived internally.</summary>
    public IReadOnlyList<ChartStateTimelineSegment> StateTimelineSegments => _stateTimelineSegments;

    /// <summary>Replaces all intervals atomically, ordered by start, end and input order. Clears observation IDs and presentation overrides.</summary>
    public ChartSeries ReplaceStateTimelineSegments(IEnumerable<ChartStateTimelineSegment> values) {
        RequireObservationKind(ChartSeriesKind.StateTimeline);
        if (values == null) throw new ArgumentNullException(nameof(values));
        ReplaceObservations(ref _stateTimelineSegments, values.OrderBy(value => value.Start).ThenBy(value => value.End), ValidateStateTimelineSegment);
        return this;
    }

    /// <summary>Updates one complete interval while preserving its observation index, ID and presentation overrides. Rendering orders intervals by time.</summary>
    public ChartSeries UpdateStateTimelineSegment(int index, ChartStateTimelineSegment value) {
        RequireObservationKind(ChartSeriesKind.StateTimeline);
        UpdateObservation(ref _stateTimelineSegments, index, value, ValidateStateTimelineSegment);
        return this;
    }

    private static ChartStateTimelineSegment ValidateStateTimelineSegment(ChartStateTimelineSegment value) {
        if (value.State == null) throw new ArgumentException("Segments must be created with a state.", nameof(value));
        return new(value.Start, value.End, value.State, value.Detail);
    }

    private IReadOnlyList<ChartGanttLaneItem> _ganttLaneItems = Array.Empty<ChartGanttLaneItem>();
    /// <summary>Gets complete lane items, including category, label, detail and an optional open end. Numeric points are derived internally.</summary>
    public IReadOnlyList<ChartGanttLaneItem> GanttLaneItems => _ganttLaneItems;

    /// <summary>Replaces all lane items atomically, ordered by start, end and input order. Clears observation IDs and presentation overrides.</summary>
    public ChartSeries ReplaceGanttLaneItems(IEnumerable<ChartGanttLaneItem> values) {
        RequireObservationKind(ChartSeriesKind.GanttLane);
        if (values == null) throw new ArgumentNullException(nameof(values));
        ReplaceObservations(ref _ganttLaneItems, values.OrderBy(value => value.Start).ThenBy(value => value.End ?? double.MaxValue), ValidateGanttLaneItem);
        return this;
    }

    /// <summary>Updates one complete lane item while preserving its observation index, ID and presentation overrides.</summary>
    public ChartSeries UpdateGanttLaneItem(int index, ChartGanttLaneItem value) {
        RequireObservationKind(ChartSeriesKind.GanttLane);
        UpdateObservation(ref _ganttLaneItems, index, value, ValidateGanttLaneItem);
        return this;
    }

    private static ChartGanttLaneItem ValidateGanttLaneItem(ChartGanttLaneItem value) =>
        new(value.Start, value.End, value.Category, value.Label, value.Detail);
}

namespace ChartForgeX.Rendering;

/// <summary>
/// Stable <c>data-cfx-role</c> values and data attributes that hosts may select in rendered SVG and HTML.
/// </summary>
/// <remarks>
/// Only the names listed here are a supported contract; other roles are renderer details and can change.
/// A renamed contract role is a breaking change and must be announced with a migration note.
/// See <c>docs/dom-contract.md</c>.
/// </remarks>
public static class ChartDomRoles {
    /// <summary>The attribute that carries a role name on rendered elements.</summary>
    public const string RoleAttribute = "data-cfx-role";

    /// <summary>The attribute that carries a lane's series identifier on schedule rows, lane labels and their marks.</summary>
    public const string LaneIdAttribute = "data-cfx-lane-id";

    /// <summary>The attribute that carries a group name on schedule group headers and on the rows inside that group.</summary>
    public const string LaneGroupAttribute = "data-cfx-lane-group";

    /// <summary>A group wrapping one schedule lane's label and summary text.</summary>
    public const string ScheduleRow = "schedule-row";

    /// <summary>The label text of a state-timeline or Gantt-lane row.</summary>
    public const string ScheduleRowLabel = "schedule-row-label";

    /// <summary>The optional right-hand summary text of a schedule lane.</summary>
    public const string LaneSummary = "lane-summary";

    /// <summary>A group header above the lanes of a state timeline.</summary>
    public const string StateTimelineGroup = "state-timeline-group";

    /// <summary>A group header above the lanes of a Gantt-lane chart.</summary>
    public const string GanttLaneGroup = "gantt-lane-group";

    /// <summary>A state-timeline segment target; carries the state, start, end and source metadata.</summary>
    public const string StateTimelineSegment = "state-timeline-segment";

    /// <summary>The painted shape inside a state-timeline segment.</summary>
    public const string StateTimelineSegmentShape = "state-timeline-segment-shape";

    /// <summary>A Gantt-lane item target.</summary>
    public const string GanttLaneItem = "gantt-lane-item";

    /// <summary>A Cartesian data point target.</summary>
    public const string Point = "point";

    /// <summary>A chart legend entry; carries <c>data-cfx-series-key</c> and an accessible label.</summary>
    public const string LegendEntry = "legend-entry";

    /// <summary>A value-axis tick label on the primary Y axis.</summary>
    public const string AxisYLabel = "axis-y-label";

    /// <summary>A topology node.</summary>
    public const string TopologyNode = "topology-node";

    /// <summary>A topology edge.</summary>
    public const string TopologyEdge = "topology-edge";
}

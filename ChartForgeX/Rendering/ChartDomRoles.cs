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

    /// <summary>The series ordinal on series groups, marks and their focus targets.</summary>
    public const string SeriesAttribute = "data-cfx-series";

    /// <summary>The ordinal of a data mark within its series; elements carrying it and <see cref="SeriesAttribute"/> are marks.</summary>
    public const string PointAttribute = "data-cfx-point";

    /// <summary>The stable series identifier on series groups, marks and legend entries.</summary>
    public const string SeriesKeyAttribute = "data-cfx-series-key";

    /// <summary>The series display name on a series group.</summary>
    public const string SeriesNameAttribute = "data-cfx-series-name";

    /// <summary>A mark's raw category-axis coordinate (an OLE date on time axes).</summary>
    public const string XAttribute = "data-cfx-x";

    /// <summary>A time-axis mark's coordinate as an ISO 8601 instant.</summary>
    public const string XInstantAttribute = "data-cfx-x-instant";

    /// <summary>A mark's value as the chart formats it, including its unit.</summary>
    public const string FormattedValueAttribute = "data-cfx-formatted-value";

    /// <summary>A mark's category-axis coordinate as the axis formats it.</summary>
    public const string FormattedXAttribute = "data-cfx-formatted-x";

    /// <summary>The caption a reader sees for a mark's category: the authored label or the formatted coordinate.</summary>
    public const string DisplayLabelAttribute = "data-cfx-display-label";

    /// <summary>A formatted interval such as "Jan 26 – May 26" on range, schedule and bucket marks.</summary>
    public const string RangeLabelAttribute = "data-cfx-range-label";

    /// <summary>The record keys a mark represents, as a JSON array of strings.</summary>
    public const string KeysAttribute = "data-cfx-keys";

    /// <summary>The field values that define a mark, as a JSON object of strings, numbers and booleans.</summary>
    public const string FieldsAttribute = "data-cfx-fields";

    /// <summary>The root attribute written by <see cref="VisualSvgOptions.WithEnhanceHooks"/>; its value is the hook version.</summary>
    public const string EnhanceAttribute = "data-cfx-enhance";

    /// <summary>The root's view id for linked-view payloads: the export's ID namespace.</summary>
    public const string ViewAttribute = "data-cfx-view";

    /// <summary>The root's logical width and height, separated by a space.</summary>
    public const string SizeAttribute = "data-cfx-size";

    /// <summary>The root's logical plot rectangle: x, y, width and height.</summary>
    public const string PlotAttribute = "data-cfx-plot";

    /// <summary>The root's category-axis domain: minimum and maximum raw coordinates.</summary>
    public const string XDomainAttribute = "data-cfx-x-domain";

    /// <summary>The root's category-axis scale: <c>linear</c>, <c>time</c>, <c>log</c> or <c>symlog</c>.</summary>
    public const string XKindAttribute = "data-cfx-x-kind";

    /// <summary>The root's orientation when categories run vertically: <c>horizontal</c>.</summary>
    public const string OrientationAttribute = "data-cfx-orientation";

    /// <summary>Present on the root when a shared-X crosshair applies (line and area families).</summary>
    public const string CrosshairAttribute = "data-cfx-crosshair";

    /// <summary>Present on the root when dragging across a continuous X axis selects a range.</summary>
    public const string BrushAttribute = "data-cfx-brush";

    /// <summary>Written by enhance hooks on every interaction target in any chart family: the innermost series group that identifies one datum.</summary>
    public const string MarkAttribute = "data-cfx-mark";

    /// <summary>A mark's logical semantic extent (x, y, width, height), written by enhance hooks so marks without painted geometry still have an anchor.</summary>
    public const string BoundsAttribute = "data-cfx-bounds";

    /// <summary>The series ordinal a legend entry controls.</summary>
    public const string LegendForAttribute = "data-cfx-legend-for";

    /// <summary>Marks the optional <c>&lt;metadata&gt;</c> JSON data island.</summary>
    public const string EnhanceDataAttribute = "data-cfx-enhance-data";

    /// <summary>A shared-domain axis strip without plot marks or frame.</summary>
    public const string StripAxis = "strip-axis";
    /// <summary>The full-width horizontal line in an axis strip.</summary>
    public const string StripAxisLine = "strip-axis-line";
    /// <summary>A shared-domain tick line in an axis strip.</summary>
    public const string StripAxisTick = "strip-axis-tick";
    /// <summary>A text group for an axis-strip caption.</summary>
    public const string StripAxisLabel = "strip-axis-label";

    /// <summary>A group wrapping one schedule lane's label and summary text. It identifies the lane but is not an interaction target.</summary>
    public const string ScheduleRow = "schedule-row";

    /// <summary>The label text of a state-timeline or Gantt-lane row.</summary>
    public const string ScheduleRowLabel = "schedule-row-label";

    /// <summary>The optional right-hand summary text of a schedule lane.</summary>
    public const string LaneSummary = "lane-summary";

    /// <summary>A group header above the lanes of a state timeline.</summary>
    public const string StateTimelineGroup = "state-timeline-group";

    /// <summary>A group header above the lanes of a Gantt-lane chart.</summary>
    public const string GanttLaneGroup = "gantt-lane-group";

    /// <summary>A group header in a mixed schedule that combines state lanes, Gantt lanes or ranges.</summary>
    public const string ScheduleGroup = "schedule-group";

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

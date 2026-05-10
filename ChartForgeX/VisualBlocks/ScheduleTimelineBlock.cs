using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A dense dashboard schedule timeline with time-of-day axis, grid lines, lanes, and event pills.
/// </summary>
public sealed class ScheduleTimelineBlock : VisualBlock<ScheduleTimelineBlock> {
    private readonly List<ScheduleTimelineEvent> _events = new();
    private readonly List<string> _headerActions = new();
    private double _start = 8;
    private double _end = 17;
    private double _tickInterval = 1;
    private double? _currentTime;

    /// <summary>Gets schedule events.</summary>
    public IReadOnlyList<ScheduleTimelineEvent> Events => _events;

    /// <summary>Gets optional header action chips.</summary>
    public IReadOnlyList<string> HeaderActions => _headerActions;

    /// <summary>Gets or sets the visible start time as numeric hours.</summary>
    public double Start { get => _start; set => _start = value; }

    /// <summary>Gets or sets the visible end time as numeric hours.</summary>
    public double End { get => _end; set => _end = value; }

    /// <summary>Gets or sets the time-axis tick interval in hours.</summary>
    public double TickInterval { get => _tickInterval; set => _tickInterval = value; }

    /// <summary>Gets or sets the optional current-time marker.</summary>
    public double? CurrentTime { get => _currentTime; set => _currentTime = value; }

    /// <summary>Gets or sets whether vertical time grid lines are shown.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>Creates a new schedule timeline block.</summary>
    public static ScheduleTimelineBlock Create() => new();

    /// <summary>Sets the visible time range and tick interval.</summary>
    public ScheduleTimelineBlock WithTimeRange(double start, double end, double tickInterval = 1) {
        Start = start;
        End = end;
        TickInterval = tickInterval;
        return this;
    }

    /// <summary>Sets the optional current-time marker.</summary>
    public ScheduleTimelineBlock WithCurrentTime(double? value) { CurrentTime = value; return this; }

    /// <summary>Sets whether vertical time grid lines are shown.</summary>
    public ScheduleTimelineBlock WithGrid(bool enabled = true) { ShowGrid = enabled; return this; }

    /// <summary>Sets optional header action chips.</summary>
    public ScheduleTimelineBlock WithHeaderActions(params string[] actions) {
        _headerActions.Clear();
        if (actions != null) foreach (var action in actions) _headerActions.Add(action ?? string.Empty);
        return this;
    }

    /// <summary>Adds a schedule event.</summary>
    public ScheduleTimelineBlock AddEvent(string title, double start, double end, int lane = 0, ChartColor? color = null, VisualStatus status = VisualStatus.Neutral, string? badge = null, string[]? avatars = null) {
        _events.Add(new ScheduleTimelineEvent(title, start, end, lane, color, status, badge, avatars));
        return this;
    }
}

/// <summary>
/// One event pill in a schedule timeline.
/// </summary>
public sealed class ScheduleTimelineEvent {
    private string _title;
    private string _badge = string.Empty;
    private VisualStatus _status;
    private readonly List<string> _avatars = new();

    /// <summary>Initializes a schedule event.</summary>
    public ScheduleTimelineEvent(string title, double start, double end, int lane = 0, ChartColor? color = null, VisualStatus status = VisualStatus.Neutral, string? badge = null, string[]? avatars = null) {
        _title = title ?? throw new ArgumentNullException(nameof(title));
        Start = start;
        End = end;
        Lane = lane;
        Color = color;
        Status = status;
        _badge = badge ?? string.Empty;
        if (avatars != null) foreach (var avatar in avatars) _avatars.Add(avatar ?? string.Empty);
    }

    /// <summary>Gets or sets the event title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the event start time as numeric hours.</summary>
    public double Start { get; set; }

    /// <summary>Gets or sets the event end time as numeric hours.</summary>
    public double End { get; set; }

    /// <summary>Gets or sets the vertical lane index.</summary>
    public int Lane { get; set; }

    /// <summary>Gets or sets an optional event color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets semantic event status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets optional compact badge text.</summary>
    public string Badge { get => _badge; set => _badge = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets compact avatar labels shown at the trailing edge of the event.</summary>
    public IReadOnlyList<string> Avatars => _avatars;

    /// <summary>Adds an avatar label.</summary>
    public ScheduleTimelineEvent AddAvatar(string label) {
        _avatars.Add(label ?? throw new ArgumentNullException(nameof(label)));
        return this;
    }
}

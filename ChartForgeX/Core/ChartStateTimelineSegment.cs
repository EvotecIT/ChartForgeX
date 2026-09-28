using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one interval of a state timeline lane. Start and end are OLE Automation dates holding UTC instants.
/// Time between segments is rendered as a gap (no data).
/// </summary>
public readonly struct ChartStateTimelineSegment {
    /// <summary>Initializes a segment from OLE Automation date values.</summary>
    /// <param name="start">The inclusive start instant.</param>
    /// <param name="end">The exclusive end instant; must be later than <paramref name="start"/>.</param>
    /// <param name="state">The key of the state that applies during the interval.</param>
    /// <param name="detail">Optional detail text shown in tooltips.</param>
    public ChartStateTimelineSegment(double start, double end, string state, string? detail = null) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        if (end <= start) throw new ArgumentOutOfRangeException(nameof(end), end, "Segment end must be later than its start.");
        if (string.IsNullOrWhiteSpace(state)) throw new ArgumentException("Segment state must not be empty.", nameof(state));
        Start = start;
        End = end;
        State = state;
        Detail = detail;
    }

    /// <summary>Initializes a segment from date/time values. <see cref="DateTimeKind.Local"/> values are converted to UTC;
    /// <see cref="DateTimeKind.Unspecified"/> values are treated as UTC. Both instants must be on or after 1899-12-30 UTC.
    /// DateTime inputs are rounded to 100-microsecond time-axis resolution; shorter intervals are rejected.</summary>
    /// <param name="start">The inclusive start.</param>
    /// <param name="end">The exclusive end; must be later than <paramref name="start"/>.</param>
    /// <param name="state">The key of the state that applies during the interval.</param>
    /// <param name="detail">Optional detail text shown in tooltips.</param>
    public ChartStateTimelineSegment(DateTime start, DateTime end, string state, string? detail = null) : this(ChartDateTime.ToOrderedOaDate(start, nameof(start)), ChartDateTime.ToOrderedOaEnd(start, end, nameof(end)), state, detail) {
    }

    /// <summary>Gets the inclusive start instant as an OLE Automation date.</summary>
    public double Start { get; }

    /// <summary>Gets the exclusive end instant as an OLE Automation date.</summary>
    public double End { get; }

    /// <summary>Gets the state key.</summary>
    public string State { get; }

    /// <summary>Gets optional detail text shown in tooltips.</summary>
    public string? Detail { get; }

}

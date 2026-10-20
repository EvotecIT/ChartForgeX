using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>One included interval of ordered UTC instants on a compressed time X axis.</summary>
/// <remarks>Endpoints are inclusive. Adjacent sessions may share an endpoint. Gaps between sessions
/// occupy no axis width; this explicit calendar does not infer market hours, holidays or weekdays.</remarks>
public readonly struct ChartTimeSession {
    /// <summary>Creates a session in the chart's ordered OLE Automation day coordinates.</summary>
    /// <param name="minimum">The inclusive opening instant on or after 1899-12-30 UTC.</param>
    /// <param name="maximum">The inclusive closing instant, greater than the opening instant.</param>
    /// <exception cref="ArgumentOutOfRangeException">A bound is not a finite representable ordered instant, or the interval is empty.</exception>
    public ChartTimeSession(double minimum, double maximum) {
        if (!ChartDateTime.TryFromOrderedOaDate(minimum, out _))
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Time session compression requires ordered OLE dates on or after 1899-12-30 UTC.");
        if (!ChartDateTime.TryFromOrderedOaDate(maximum, out _) || maximum <= minimum
            || ChartDateTime.CollapsesAtDisplayResolution(minimum, maximum))
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "A time session requires increasing ordered UTC instants.");
        Minimum = minimum; Maximum = maximum;
    }
    /// <summary>Creates a UTC interval. Local inputs convert to UTC; unspecified inputs are treated as UTC.</summary>
    /// <param name="minimum">The opening instant.</param>
    /// <param name="maximum">The closing instant, at least one canonical 100-microsecond display step later.</param>
    public ChartTimeSession(DateTime minimum, DateTime maximum)
        : this(ChartDateTime.ToOrderedOaDate(minimum, nameof(minimum)), ChartDateTime.ToOrderedOaEnd(minimum, maximum, nameof(maximum))) { }
    /// <summary>Creates a session from absolute instants, preserving their UTC identity.</summary>
    /// <param name="minimum">The opening instant.</param>
    /// <param name="maximum">The closing instant.</param>
    public ChartTimeSession(DateTimeOffset minimum, DateTimeOffset maximum) : this(minimum.UtcDateTime, maximum.UtcDateTime) { }
    /// <summary>Gets the inclusive opening instant in ordered OLE Automation days.</summary>
    public double Minimum { get; }
    /// <summary>Gets the inclusive closing instant in ordered OLE Automation days.</summary>
    public double Maximum { get; }
}

/// <summary>An immutable, explicitly ordered set of included time intervals shared by every series on one X axis.</summary>
/// <remarks>Elapsed time inside each session retains its duration. Closed gaps collapse to one position.
/// A closing instant and the next opening instant share that position; inverse projection selects the next opening.
/// The final closing instant retains its own identity. Values outside the first/last session extrapolate for native axis padding.</remarks>
public sealed class ChartTimeSessionDomain {
    private readonly ChartTimeSession[] _sessions;
    private readonly double[] _starts, _offsets;
    private readonly double _duration;

    /// <summary>Captures included intervals in source order without sorting or merging them.</summary>
    /// <param name="sessions">A nonempty sequence of increasing intervals; overlaps and reversed source order are rejected.</param>
    /// <exception cref="ArgumentNullException">The sequence is null.</exception>
    /// <exception cref="ArgumentException">The sequence is empty, overlapping or unordered.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An interval is invalid.</exception>
    public ChartTimeSessionDomain(IEnumerable<ChartTimeSession> sessions) {
        if (sessions == null) throw new ArgumentNullException(nameof(sessions));
        _sessions = sessions.ToArray();
        if (_sessions.Length == 0) throw new ArgumentException("A time session domain requires at least one included interval.", nameof(sessions));
        _offsets = new double[_sessions.Length];
        _starts = new double[_sessions.Length];
        for (var index = 0; index < _sessions.Length; index++) {
            var session = _sessions[index];
            _ = new ChartTimeSession(session.Minimum, session.Maximum);
            if (index > 0 && session.Minimum < _sessions[index - 1].Maximum)
                throw new ArgumentException("Time session " + index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + " overlaps the previous interval or is out of source order.", nameof(sessions));
            _offsets[index] = _duration;
            _starts[index] = session.Minimum;
            _duration += session.Maximum - session.Minimum;
        }
        Sessions = Array.AsReadOnly(_sessions);
    }
    /// <summary>Gets the captured intervals in their authored order.</summary>
    public IReadOnlyList<ChartTimeSession> Sessions { get; }
    internal IReadOnlyList<double> Offsets => _offsets;
    internal int IndexAtOrBefore(double value) => Find(_starts, value);
    internal double NextOpening(double value) {
        var index = Find(_starts, value);
        return index < 0 ? _sessions[0].Minimum : index < _sessions.Length - 1 ? _sessions[index + 1].Minimum : _sessions[index].Maximum;
    }

    internal bool Includes(double value) {
        var index = Find(_starts, value);
        return index >= 0 && value <= _sessions[index].Maximum;
    }
    internal bool IsClosed(double value) {
        var index = Find(_starts, value);
        return index >= 0 && index < _sessions.Length - 1 && value > _sessions[index].Maximum;
    }
    internal double Forward(double value) {
        var index = Find(_starts, value);
        if (index < 0) return value - _sessions[0].Minimum;
        if (value > _sessions[index].Maximum)
            return index < _sessions.Length - 1 ? _offsets[index + 1] : _duration + (value - _sessions[index].Maximum);
        return _offsets[index] + (value - _sessions[index].Minimum);
    }
    internal double Inverse(double value) {
        if (value < 0) return _sessions[0].Minimum + value;
        if (value >= _duration) return _sessions[_sessions.Length - 1].Maximum + (value - _duration);
        // Upper-bound search makes a collapsed join select the following opening.
        var index = Find(_offsets, value);
        return _sessions[index].Minimum + (value - _offsets[index]);
    }
    private static int Find(double[] values, double value) {
        var low = 0; var high = values.Length - 1;
        while (low <= high) {
            var middle = low + (high - low) / 2;
            if (values[middle] <= value) low = middle + 1;
            else high = middle - 1;
        }
        return high;
    }
}

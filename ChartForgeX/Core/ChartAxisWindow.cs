using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable, increasing source-coordinate window for a source axis.</summary>
/// <remarks>Time coordinates use OLE Automation date units. The containing Cartesian or polar viewport selects the coordinate axis and value-axis slot.</remarks>
public sealed class ChartAxisWindow {
    /// <summary>Creates a finite, nonempty source window.</summary>
    /// <param name="minimum">The inclusive lower source-coordinate bound.</param>
    /// <param name="maximum">The upper source-coordinate bound.</param>
    /// <param name="maximumInclusive">Whether an observation exactly at maximum belongs to the window. Defaults to true.</param>
    /// <exception cref="ArgumentOutOfRangeException">A bound is not finite or maximum is not greater than minimum.</exception>
    public ChartAxisWindow(double minimum, double maximum, bool maximumInclusive = true) {
        ChartPrimitiveGuards.Finite(minimum, nameof(minimum));
        ChartPrimitiveGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "An axis window requires increasing bounds.");
        Minimum = minimum; Maximum = maximum; MaximumInclusive = maximumInclusive;
    }
    /// <summary>Gets the inclusive lower source bound.</summary>
    public double Minimum { get; }
    /// <summary>Gets the upper source bound.</summary>
    public double Maximum { get; }
    /// <summary>Gets whether an observation exactly at Maximum belongs to the window.</summary>
    /// <remarks>Calendar gestures use an exclusive canonical upper boundary. Authored two-argument windows remain inclusive.
    /// Excluding an endpoint does not shorten interval geometry that approaches or crosses it.
    /// Unit time spacing requires an exclusive maximum to equal a captured canonical calendar boundary;
    /// an interior exclusive maximum is rejected rather than inventing partial-band geometry.</remarks>
    public bool MaximumInclusive { get; }

    internal bool Overlaps(double first, double last) => Math.Max(first, last) >= Minimum
        && (MaximumInclusive ? Math.Min(first, last) <= Maximum : Math.Min(first, last) < Maximum);
}

using System;

namespace ChartForgeX.Primitives;

/// <summary>
/// Represents one data point in chart coordinate space.
/// </summary>
public readonly struct ChartPoint {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the vertical coordinate.
    /// </summary>
    public readonly double Y;

    /// <summary>Gets whether a new disconnected line or area segment starts at this point.</summary>
    public bool BreakBefore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPoint"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    public ChartPoint(double x, double y) : this(x, y, false) { }

    /// <summary>Creates a finite point and optionally starts a disconnected line or area segment.</summary>
    public ChartPoint(double x, double y, bool breakBefore) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(y, nameof(y));
        X = x;
        Y = y;
        BreakBefore = breakBefore;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPoint"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="y">The vertical coordinate.</param>
    public ChartPoint(DateTime x, double y) : this(x.ToOADate(), y) { }

    /// <summary>Creates a date/time point and optionally starts a disconnected line or area segment.</summary>
    public ChartPoint(DateTime x, double y, bool breakBefore) : this(x.ToOADate(), y, breakBefore) { }

    /// <summary>
    /// Creates a chart point from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <returns>A chart point using the date/time as the x value.</returns>
    public static ChartPoint FromDateTime(DateTime x, double y) => new(x, y);
}

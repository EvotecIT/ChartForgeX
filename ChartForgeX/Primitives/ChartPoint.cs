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

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPoint"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    public ChartPoint(double x, double y) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(y, nameof(y));
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPoint"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="y">The vertical coordinate.</param>
    public ChartPoint(DateTime x, double y) {
        ChartPrimitiveGuards.Finite(y, nameof(y));
        X = x.ToOADate();
        Y = y;
    }

    /// <summary>
    /// Creates a chart point from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <returns>A chart point using the date/time as the x value.</returns>
    public static ChartPoint FromDateTime(DateTime x, double y) => new(x, y);
}

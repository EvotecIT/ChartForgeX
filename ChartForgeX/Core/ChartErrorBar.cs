using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one point estimate with lower and upper bounds.
/// </summary>
public readonly struct ChartErrorBar {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the point estimate.
    /// </summary>
    public readonly double Y;

    /// <summary>
    /// Gets the lower bound.
    /// </summary>
    public readonly double Lower;

    /// <summary>
    /// Gets the upper bound.
    /// </summary>
    public readonly double Upper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartErrorBar"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The point estimate.</param>
    /// <param name="lower">The lower bound.</param>
    /// <param name="upper">The upper bound.</param>
    public ChartErrorBar(double x, double y, double lower, double upper) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(y, nameof(y));
        ChartPrimitiveGuards.Finite(lower, nameof(lower));
        ChartPrimitiveGuards.Finite(upper, nameof(upper));
        if (lower > y) throw new ArgumentOutOfRangeException(nameof(lower), lower, "Error-bar lower bound must be less than or equal to the point estimate.");
        if (upper < y) throw new ArgumentOutOfRangeException(nameof(upper), upper, "Error-bar upper bound must be greater than or equal to the point estimate.");
        X = x;
        Y = y;
        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartErrorBar"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="y">The point estimate.</param>
    /// <param name="lower">The lower bound.</param>
    /// <param name="upper">The upper bound.</param>
    public ChartErrorBar(DateTime x, double y, double lower, double upper) : this(ChartDateTime.ToOADate(x), y, lower, upper) {
    }
}

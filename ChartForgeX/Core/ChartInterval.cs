using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one category or x-value with a start and end value.
/// </summary>
public readonly struct ChartInterval {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the interval start value.
    /// </summary>
    public readonly double Start;

    /// <summary>
    /// Gets the interval end value.
    /// </summary>
    public readonly double End;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartInterval"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="start">The interval start value.</param>
    /// <param name="end">The interval end value.</param>
    public ChartInterval(double x, double start, double end) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(start, nameof(start));
        ChartPrimitiveGuards.Finite(end, nameof(end));
        X = x;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartInterval"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="start">The interval start value.</param>
    /// <param name="end">The interval end value.</param>
    public ChartInterval(DateTime x, double start, double end) : this(ChartDateTime.ToOADate(x), start, end) {
    }
}

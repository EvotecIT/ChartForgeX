using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one x position with two comparable values.
/// </summary>
public readonly struct ChartDumbbell {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the first comparison value.
    /// </summary>
    public readonly double Start;

    /// <summary>
    /// Gets the second comparison value.
    /// </summary>
    public readonly double End;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartDumbbell"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="start">The first comparison value.</param>
    /// <param name="end">The second comparison value.</param>
    public ChartDumbbell(double x, double start, double end) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(start, nameof(start));
        ChartPrimitiveGuards.Finite(end, nameof(end));
        X = x;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartDumbbell"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="start">The first comparison value.</param>
    /// <param name="end">The second comparison value.</param>
    public ChartDumbbell(DateTime x, double start, double end) : this(ChartDateTime.ToOADate(x), start, end) {
    }
}

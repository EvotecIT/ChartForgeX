using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Two named observations at one X coordinate, retained in authored order even when they cross.</summary>
public readonly struct ChartDifferencePoint {
    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; }
    /// <summary>Gets the first authored value; it is not necessarily the lower value.</summary>
    public double First { get; }
    /// <summary>Gets the second authored value; it is not necessarily the upper value.</summary>
    public double Second { get; }
    /// <summary>Gets whether both boundaries and their fill start a disconnected segment here.</summary>
    public bool BreakBefore { get; }

    /// <summary>Creates a finite pair. Positive fill means First is greater than Second.</summary>
    /// <param name="x">The numeric horizontal coordinate.</param>
    /// <param name="first">The first authored value.</param>
    /// <param name="second">The second authored value.</param>
    /// <param name="breakBefore">Whether a new disconnected segment starts at this pair.</param>
    public ChartDifferencePoint(double x, double first, double second, bool breakBefore = false) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(first, nameof(first));
        ChartPrimitiveGuards.Finite(second, nameof(second));
        X = x; First = first; Second = second; BreakBefore = breakBefore;
    }

    /// <summary>Creates a date/time pair. Local dates convert to UTC; unspecified dates are treated as UTC.</summary>
    /// <param name="x">The date/time horizontal coordinate.</param>
    /// <param name="first">The first authored value.</param>
    /// <param name="second">The second authored value.</param>
    /// <param name="breakBefore">Whether a new disconnected segment starts at this pair.</param>
    public ChartDifferencePoint(DateTime x, double first, double second, bool breakBefore = false)
        : this(ChartDateTime.ToOADate(x), first, second, breakBefore) { }
}

using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one bubble point with an x value, y value, and positive size value.
/// </summary>
public readonly struct ChartBubble {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the vertical coordinate.
    /// </summary>
    public readonly double Y;

    /// <summary>
    /// Gets the positive value used to scale the marker area.
    /// </summary>
    public readonly double Size;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartBubble"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <param name="size">The positive size value.</param>
    public ChartBubble(double x, double y, double size) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(y, nameof(y));
        ChartPrimitiveGuards.Finite(size, nameof(size));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), size, "Bubble size must be greater than zero.");
        X = x;
        Y = y;
        Size = size;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartBubble"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <param name="size">The positive size value.</param>
    public ChartBubble(DateTime x, double y, double size) : this(ChartDateTime.ToOADate(x), y, size) {
    }
}

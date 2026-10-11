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
    /// Gets the positive source value mapped to a logical marker radius by <see cref="ChartOptions.Bubble"/>.
    /// </summary>
    public readonly double Size;

    /// <summary>Gets the independent numeric color measurement, or null when no measurement is supplied.</summary>
    public double? ColorValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartBubble"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <param name="size">The positive size value.</param>
    public ChartBubble(double x, double y, double size) : this(x, y, size, null) { }

    private ChartBubble(double x, double y, double size, double? colorValue) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(y, nameof(y));
        ChartPrimitiveGuards.Finite(size, nameof(size));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), size, "Bubble size must be greater than zero.");
        if (colorValue.HasValue) ChartPrimitiveGuards.Finite(colorValue.Value, nameof(colorValue));
        X = x;
        Y = y;
        Size = size;
        ColorValue = colorValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartBubble"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <param name="size">The positive size value.</param>
    public ChartBubble(DateTime x, double y, double size) : this(ChartDateTime.ToOADate(x), y, size) {
    }

    /// <summary>Returns an independent bubble with the same coordinates and size and a replacement color measurement.</summary>
    /// <param name="value">The finite numeric color measurement, or null to clear it.</param>
    /// <returns>A new bubble. The current bubble is unchanged.</returns>
    public ChartBubble WithColorValue(double? value) => new(X, Y, Size, value);
}

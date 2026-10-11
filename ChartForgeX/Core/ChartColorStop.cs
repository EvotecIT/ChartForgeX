using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>An immutable color anchored at an absolute finite numeric value.</summary>
public sealed class ChartColorStop {
    /// <summary>Creates a continuous color anchor in the data value's units.</summary>
    /// <param name="value">The finite numeric value at which the color is exact.</param>
    /// <param name="color">The color at the anchor.</param>
    public ChartColorStop(double value, ChartColor color) {
        if (!ChartMath.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "Color stop values must be finite.");
        Value = value;
        Color = color;
    }

    /// <summary>Gets the absolute numeric anchor, rather than a normalized offset.</summary>
    public double Value { get; }

    /// <summary>Gets the color assigned exactly at the anchor.</summary>
    public ChartColor Color { get; }
}

using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one x position with lower and upper band values.
/// </summary>
public readonly struct ChartRangeBand {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the lower band value.
    /// </summary>
    public readonly double Lower;

    /// <summary>
    /// Gets the upper band value.
    /// </summary>
    public readonly double Upper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartRangeBand"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="lower">The lower band value.</param>
    /// <param name="upper">The upper band value.</param>
    public ChartRangeBand(double x, double lower, double upper) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(lower, nameof(lower));
        ChartPrimitiveGuards.Finite(upper, nameof(upper));
        if (lower > upper) throw new ArgumentOutOfRangeException(nameof(lower), lower, "Range-band lower value must be less than or equal to the upper value.");
        X = x;
        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartRangeBand"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="lower">The lower band value.</param>
    /// <param name="upper">The upper band value.</param>
    public ChartRangeBand(DateTime x, double lower, double upper) : this(ChartDateTime.ToOADate(x), lower, upper) {
    }
}

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

    /// <summary>Gets whether both boundaries begin a disconnected segment at this observation.</summary>
    public bool BreakBefore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartRangeBand"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="lower">The lower band value.</param>
    /// <param name="upper">The upper band value.</param>
    /// <param name="breakBefore">Whether both boundaries begin a disconnected segment.</param>
    public ChartRangeBand(double x, double lower, double upper, bool breakBefore = false) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(lower, nameof(lower));
        ChartPrimitiveGuards.Finite(upper, nameof(upper));
        if (lower > upper) throw new ArgumentOutOfRangeException(nameof(lower), lower, "Range-band lower value must be less than or equal to the upper value.");
        X = x;
        Lower = lower;
        Upper = upper;
        BreakBefore = breakBefore;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartRangeBand"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="lower">The lower band value.</param>
    /// <param name="upper">The upper band value.</param>
    /// <param name="breakBefore">Whether both boundaries begin a disconnected segment.</param>
    public ChartRangeBand(DateTime x, double lower, double upper, bool breakBefore = false) : this(ChartDateTime.ToOADate(x), lower, upper, breakBefore) {
    }

    /// <summary>Gets optional immutable navigation, accessibility and host metadata.</summary>
    public ChartMarkOptions? MarkOptions { get; }

    /// <summary>Returns a copy with replacement mark metadata, retaining all authored values.</summary>
    /// <param name="options">Immutable metadata, or null to clear it.</param>
    /// <returns>A new observation. The current observation is unchanged.</returns>
    public ChartRangeBand WithMarkOptions(ChartMarkOptions? options) => new(this, options);

    private ChartRangeBand(ChartRangeBand source, ChartMarkOptions? options) {
        this = source;
        MarkOptions = options;
    }
}

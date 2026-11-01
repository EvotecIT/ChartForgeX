using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one category or x-value with a start and end value.
/// </summary>
public readonly struct ChartInterval {
    /// <summary>
    /// Gets the category coordinate, mapped to X for vertical range bars and Y for horizontal range bars.
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
    /// <param name="x">The category coordinate.</param>
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
    /// <param name="x">The date/time category coordinate.</param>
    /// <param name="start">The interval start value.</param>
    /// <param name="end">The interval end value.</param>
    public ChartInterval(DateTime x, double start, double end) : this(ChartDateTime.ToOADate(x), start, end) {
    }

    /// <summary>Gets optional immutable navigation, accessibility and host metadata.</summary>
    public ChartMarkOptions? MarkOptions { get; }

    /// <summary>Returns a copy with replacement mark metadata, retaining all authored values.</summary>
    /// <param name="options">Immutable metadata, or null to clear it.</param>
    /// <returns>A new observation. The current observation is unchanged.</returns>
    public ChartInterval WithMarkOptions(ChartMarkOptions? options) => new(this, options);

    private ChartInterval(ChartInterval source, ChartMarkOptions? options) {
        this = source;
        MarkOptions = options;
    }
}

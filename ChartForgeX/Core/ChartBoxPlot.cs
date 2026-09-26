using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one box plot summary.
/// </summary>
public readonly struct ChartBoxPlot {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the minimum whisker value.
    /// </summary>
    public readonly double Minimum;

    /// <summary>
    /// Gets the first quartile value.
    /// </summary>
    public readonly double Q1;

    /// <summary>
    /// Gets the median value.
    /// </summary>
    public readonly double Median;

    /// <summary>
    /// Gets the third quartile value.
    /// </summary>
    public readonly double Q3;

    /// <summary>
    /// Gets the maximum whisker value.
    /// </summary>
    public readonly double Maximum;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartBoxPlot"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="minimum">The minimum whisker value.</param>
    /// <param name="q1">The first quartile value.</param>
    /// <param name="median">The median value.</param>
    /// <param name="q3">The third quartile value.</param>
    /// <param name="maximum">The maximum whisker value.</param>
    public ChartBoxPlot(double x, double minimum, double q1, double median, double q3, double maximum) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(minimum, nameof(minimum));
        ChartPrimitiveGuards.Finite(q1, nameof(q1));
        ChartPrimitiveGuards.Finite(median, nameof(median));
        ChartPrimitiveGuards.Finite(q3, nameof(q3));
        ChartPrimitiveGuards.Finite(maximum, nameof(maximum));
        if (minimum > q1 || q1 > median || median > q3 || q3 > maximum) {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Box plot values must be ordered as minimum <= q1 <= median <= q3 <= maximum.");
        }

        X = x;
        Minimum = minimum;
        Q1 = q1;
        Median = median;
        Q3 = q3;
        Maximum = maximum;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartBoxPlot"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="minimum">The minimum whisker value.</param>
    /// <param name="q1">The first quartile value.</param>
    /// <param name="median">The median value.</param>
    /// <param name="q3">The third quartile value.</param>
    /// <param name="maximum">The maximum whisker value.</param>
    public ChartBoxPlot(DateTime x, double minimum, double q1, double median, double q3, double maximum) : this(ChartDateTime.ToOADate(x), minimum, q1, median, q3, maximum) {
    }
}

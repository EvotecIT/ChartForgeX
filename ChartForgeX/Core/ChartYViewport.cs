using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable source-coordinate primary Y window for a supported vertical Cartesian chart.</summary>
/// <remarks>An explicit window keeps the full-data stack calculations and source observations. It does not autoscale Y from visible X data.</remarks>
public sealed class ChartYViewport {
    /// <summary>Creates a finite, increasing primary Y window.</summary>
    /// <param name="minimum">The inclusive lower source-coordinate bound.</param>
    /// <param name="maximum">The inclusive upper source-coordinate bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">A bound is not finite or the window is empty.</exception>
    public ChartYViewport(double minimum, double maximum) {
        ChartPrimitiveGuards.Finite(minimum, nameof(minimum));
        ChartPrimitiveGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The Y viewport requires increasing bounds.");
        Minimum = minimum; Maximum = maximum;
    }
    /// <summary>Gets the inclusive lower source bound.</summary>
    public double Minimum { get; }
    /// <summary>Gets the inclusive upper source bound.</summary>
    public double Maximum { get; }
}

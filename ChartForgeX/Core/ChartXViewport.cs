using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable numeric X window used when preparing a line chart.</summary>
public sealed class ChartXViewport {
    /// <summary>Creates a finite, increasing X window.</summary>
    /// <param name="minimum">The inclusive lower source-coordinate bound.</param>
    /// <param name="maximum">The inclusive upper source-coordinate bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">A bound is not finite, or maximum is not greater than minimum.</exception>
    public ChartXViewport(double minimum, double maximum) {
        ChartPrimitiveGuards.Finite(minimum, nameof(minimum));
        ChartPrimitiveGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The X viewport requires increasing bounds.");
        Minimum = minimum; Maximum = maximum;
    }

    /// <summary>Gets the inclusive lower source-coordinate bound.</summary>
    public double Minimum { get; }
    /// <summary>Gets the inclusive upper source-coordinate bound.</summary>
    public double Maximum { get; }
}

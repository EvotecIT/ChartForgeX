using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable, increasing source-coordinate window for any physical Cartesian axis.</summary>
/// <remarks>Time coordinates use OLE Automation date units. The containing viewport selects the physical axis and value-axis slot.</remarks>
public sealed class ChartAxisWindow {
    /// <summary>Creates a finite, nonempty source window.</summary>
    /// <param name="minimum">The inclusive lower source-coordinate bound.</param>
    /// <param name="maximum">The inclusive upper source-coordinate bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">A bound is not finite or maximum is not greater than minimum.</exception>
    public ChartAxisWindow(double minimum, double maximum) {
        ChartPrimitiveGuards.Finite(minimum, nameof(minimum));
        ChartPrimitiveGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "An axis window requires increasing bounds.");
        Minimum = minimum; Maximum = maximum;
    }
    /// <summary>Gets the inclusive lower source bound.</summary>
    public double Minimum { get; }
    /// <summary>Gets the inclusive upper source bound.</summary>
    public double Maximum { get; }
}

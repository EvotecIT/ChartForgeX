using System;

namespace ChartForgeX.Core;

/// <summary>Captures physical Cartesian axis windows and the value-domain policy for one native preparation.</summary>
/// <remarks>Null windows retain full domains unless <see cref="ValueDomain"/> fits an unrequested value axis.
/// Pass a null viewport to Chart.Prepare to restore every full domain. Physical secondary X is available on horizontal charts;
/// physical secondary Y is available on vertical charts.</remarks>
public sealed class ChartCartesianViewport {
    /// <summary>Creates a request containing at least one physical axis window.</summary>
    /// <param name="x">The primary horizontal window.</param>
    /// <param name="y">The primary vertical window.</param>
    /// <param name="secondaryX">The secondary horizontal value window on a horizontal chart.</param>
    /// <param name="secondaryY">The secondary vertical value window on a vertical chart.</param>
    /// <param name="valueDomain">How unrequested value-axis domains are resolved.</param>
    /// <exception cref="ArgumentException">Every window is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value-domain policy is unknown.</exception>
    public ChartCartesianViewport(ChartAxisWindow? x = null, ChartAxisWindow? y = null,
        ChartAxisWindow? secondaryX = null, ChartAxisWindow? secondaryY = null, ChartValueDomain valueDomain = ChartValueDomain.FullData) {
        if (x == null && y == null && secondaryX == null && secondaryY == null) throw new ArgumentException("A Cartesian viewport requires a physical axis window.");
        if (!Enum.IsDefined(typeof(ChartValueDomain), valueDomain)) throw new ArgumentOutOfRangeException(nameof(valueDomain));
        X = x; Y = y; SecondaryX = secondaryX; SecondaryY = secondaryY; ValueDomain = valueDomain;
    }
    /// <summary>Gets the primary horizontal window.</summary>
    public ChartAxisWindow? X { get; }
    /// <summary>Gets the primary vertical window.</summary>
    public ChartAxisWindow? Y { get; }
    /// <summary>Gets the secondary horizontal value window.</summary>
    public ChartAxisWindow? SecondaryX { get; }
    /// <summary>Gets the secondary vertical value window.</summary>
    public ChartAxisWindow? SecondaryY { get; }
    /// <summary>Gets the policy for unrequested value-axis domains.</summary>
    public ChartValueDomain ValueDomain { get; }
}

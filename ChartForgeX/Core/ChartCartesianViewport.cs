using System;

namespace ChartForgeX.Core;

/// <summary>Explicit source-coordinate X, primary Y, or XY windows for native Cartesian preparation.</summary>
/// <remarks>A null axis keeps that axis's resolved full-data domain. Primary Y windows require charts without secondary-axis series.</remarks>
public sealed class ChartCartesianViewport {
    /// <summary>Creates one or two explicit windows.</summary>
    /// <param name="x">An X window, or null to retain the full X domain.</param>
    /// <param name="y">A primary Y window, or null to retain its full domain.</param>
    /// <exception cref="ArgumentException">Both windows are null.</exception>
    public ChartCartesianViewport(ChartXViewport? x = null, ChartYViewport? y = null) {
        if (x == null && y == null) throw new ArgumentException("A Cartesian viewport requires an X or primary Y window.");
        X = x; Y = y;
    }
    /// <summary>Gets the explicit X window, or null for the full X domain.</summary>
    public ChartXViewport? X { get; }
    /// <summary>Gets the explicit primary Y window, or null for the full primary Y domain.</summary>
    public ChartYViewport? Y { get; }
}

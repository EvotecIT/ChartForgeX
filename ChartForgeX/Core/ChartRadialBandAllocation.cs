using System;

namespace ChartForgeX.Core;

/// <summary>Allocates equal-width Pie/Donut bands in source series order without mutating authored bands.</summary>
/// <remarks>Automatic allocation requires every series to leave RadialBand unset. Null chart configuration retains existing defaults.
/// Disjoint allocation describes unshifted bands. Authored slice explosion may overlap them; inner external captions use overlaid leaders.</remarks>
public sealed class ChartRadialBandAllocation {
    /// <summary>Creates immutable normalized geometry; preparation checks that all requested gaps and positive-width bands fit.</summary>
    /// <param name="innerRatio">The shared hole radius, from zero inclusive to one exclusive.</param>
    /// <param name="gapRatio">The gap between neighboring bands, from zero inclusive to one exclusive.</param>
    public ChartRadialBandAllocation(double innerRatio = 0, double gapRatio = 0) {
        ChartGuards.UnitInterval(innerRatio, nameof(innerRatio)); ChartGuards.UnitInterval(gapRatio, nameof(gapRatio));
        if (innerRatio == 1) throw new ArgumentOutOfRangeException(nameof(innerRatio));
        if (gapRatio == 1) throw new ArgumentOutOfRangeException(nameof(gapRatio));
        InnerRatio = innerRatio; GapRatio = gapRatio;
    }
    /// <summary>Gets the chart's normalized shared hole radius.</summary>
    public double InnerRatio { get; }
    /// <summary>Gets the normalized spacing between bands.</summary>
    public double GapRatio { get; }
}

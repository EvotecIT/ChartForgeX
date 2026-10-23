using System;

namespace ChartForgeX.Core;

/// <summary>An immutable annular band expressed as ratios of the pie chart's shared available radius.</summary>
/// <remarks>An explicit band overrides Pie/Donut's default inner radius. Multiple pie-like series require disjoint explicit bands.</remarks>
public sealed class ChartRadialBand {
    /// <summary>Creates a finite positive-width band within the available radius.</summary>
    /// <param name="innerRatio">The inner ratio from zero inclusive to the outer ratio exclusive.</param>
    /// <param name="outerRatio">The outer ratio, greater than the inner ratio and at most one.</param>
    public ChartRadialBand(double innerRatio, double outerRatio) {
        if (double.IsNaN(innerRatio) || double.IsInfinity(innerRatio) || innerRatio < 0 || innerRatio >= 1)
            throw new ArgumentOutOfRangeException(nameof(innerRatio));
        if (double.IsNaN(outerRatio) || double.IsInfinity(outerRatio) || outerRatio <= innerRatio || outerRatio > 1)
            throw new ArgumentOutOfRangeException(nameof(outerRatio));
        InnerRatio = innerRatio; OuterRatio = outerRatio;
    }
    /// <summary>Gets the inner-radius ratio.</summary>
    public double InnerRatio { get; }
    /// <summary>Gets the outer-radius ratio.</summary>
    public double OuterRatio { get; }
}

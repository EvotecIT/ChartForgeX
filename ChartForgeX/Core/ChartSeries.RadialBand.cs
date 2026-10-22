using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartRadialBand? _radialBand;
    /// <summary>Gets or sets the explicit pie/donut band, or null for the existing single-series default geometry.</summary>
    /// <remarks>Multiple series require disjoint bands. Only the outermost band may paint external labels; positive slice offsets are unsupported across multiple bands.</remarks>
    public ChartRadialBand? RadialBand {
        get => _radialBand;
        set {
            if (value != null && !ChartSeriesKindTraits.IsPieLikeKind(Kind)) throw new InvalidOperationException("Explicit radial bands require Pie or Donut series.");
            _radialBand = value;
        }
    }
    /// <summary>Sets an immutable explicit pie/donut band; null restores the single-series default.</summary>
    public ChartSeries WithRadialBand(ChartRadialBand? band) { RadialBand = band; return this; }
}

using System;

namespace ChartForgeX.Core;

/// <summary>Selects the source text used for region callouts. Full region facts remain available independently.</summary>
public enum ChartMapRegionLabel {
    /// <summary>Uses the canonical region code.</summary>
    Code,
    /// <summary>Uses the complete authored region name.</summary>
    Name,
    /// <summary>Uses the name followed by its canonical code.</summary>
    NameAndCode
}

/// <summary>Opts map captions into shared collision-aware placement and bounded straight leaders.</summary>
/// <remarks>Null on the chart preserves existing map labels. This policy does not enable labels that the chart or series has disabled.
/// Region captions use existing data-label typography/placement; omitted or shortened captions retain their complete source text.</remarks>
public sealed class ChartMapCallouts {
    /// <summary>Creates a detached immutable callout policy.</summary>
    /// <param name="maximumDistance">Maximum anchor-to-label-edge distance in logical scene units. Must be positive and finite.</param>
    /// <param name="regionLabel">Region text. Existing point/route captions retain their authored text.</param>
    public ChartMapCallouts(double maximumDistance = 48, ChartMapRegionLabel regionLabel = ChartMapRegionLabel.Code) {
        ChartGuards.Finite(maximumDistance, nameof(maximumDistance));
        if (maximumDistance <= 0) throw new ArgumentOutOfRangeException(nameof(maximumDistance));
        if (!Enum.IsDefined(typeof(ChartMapRegionLabel), regionLabel)) throw new ArgumentOutOfRangeException(nameof(regionLabel));
        MaximumDistance = maximumDistance; RegionLabel = regionLabel;
    }
    /// <summary>Gets the maximum straight-leader reach in logical scene units. Crowded labels may be shortened or omitted.</summary>
    public double MaximumDistance { get; }
    /// <summary>Gets the source text selected for region captions.</summary>
    public ChartMapRegionLabel RegionLabel { get; }
}

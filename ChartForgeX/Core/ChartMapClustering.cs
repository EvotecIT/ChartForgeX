using System;

namespace ChartForgeX.Core;

/// <summary>Opts one DottedMap dataset into deterministic screen-grid count badges.</summary>
/// <remarks>Only geographically visible points in this dataset participate. Cells are anchored to the fitted map's upper-left corner,
/// so different output sizes or viewports can produce different membership. This is grid aggregation, not a distance-radius or geographic clustering algorithm.
/// Independent source weights, color domains, feature metadata and observation IDs are retained; badges count contributors and do not average their measurements.</remarks>
public sealed class ChartMapClustering {
    /// <summary>Creates an immutable clustering policy. Null on the series preserves individual source marks.</summary>
    /// <param name="cellSize">Positive finite cell side in logical scene units.</param>
    /// <param name="minimumCount">Minimum contributors per badge, at least two.</param>
    /// <param name="radius">Positive finite count-badge radius in logical scene units.</param>
    public ChartMapClustering(double cellSize = 48, int minimumCount = 2, double radius = 12) {
        ChartGuards.Finite(cellSize, nameof(cellSize)); ChartGuards.Finite(radius, nameof(radius));
        if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
        if (minimumCount < 2) throw new ArgumentOutOfRangeException(nameof(minimumCount));
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        CellSize = cellSize; MinimumCount = minimumCount; Radius = radius;
    }
    /// <summary>Gets the logical cell side. Cells never join across datasets.</summary>
    public double CellSize { get; }
    /// <summary>Gets the minimum contributors required for a count badge.</summary>
    public int MinimumCount { get; }
    /// <summary>Gets the fixed count-badge radius, independent of raw weight and color values.</summary>
    public double Radius { get; }
}

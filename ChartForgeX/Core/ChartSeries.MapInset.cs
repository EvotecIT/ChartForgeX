using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets this TileMap's optional independent inset within the final shared map plot.</summary>
    /// <remarks>Coordinates are normalized from zero to one. The inset has its own tile layout and is not geographically registered.</remarks>
    public ChartRect? MapInset { get; private set; }

    /// <summary>Places a TileMap inside a multi-map scene without changing its tile coordinates or observation identity.</summary>
    /// <param name="bounds">A finite positive rectangle contained in [0,1] on both axes, or null to clear the placement.</param>
    /// <returns>The current series.</returns>
    /// <remarks>Insets require at least one RegionMap or DottedMap peer. Standalone TileMaps retain their existing layout.</remarks>
    public ChartSeries WithMapInset(ChartRect? bounds) {
        if (Kind != ChartSeriesKind.TileMap) throw new InvalidOperationException("Only TileMap datasets accept a map inset.");
        if (bounds.HasValue) {
            var value = bounds.Value;
            ChartGuards.Finite(value.Left, nameof(bounds)); ChartGuards.Finite(value.Top, nameof(bounds));
            ChartGuards.Finite(value.Width, nameof(bounds)); ChartGuards.Finite(value.Height, nameof(bounds));
            if (value.Left < 0 || value.Top < 0 || value.Width <= 0 || value.Height <= 0 || value.Right > 1 || value.Bottom > 1)
                throw new ArgumentOutOfRangeException(nameof(bounds), "Map inset bounds must be positive and contained in [0,1].");
        }
        MapInset = bounds; return this;
    }
}

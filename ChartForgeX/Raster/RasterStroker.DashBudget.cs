using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static partial class RasterStroker {
    internal const int DashTileSize = 2048;

    internal static double DashPadding(double width, double miter) =>
        width * (miter >= 1 && !double.IsInfinity(miter) ? miter : 4);

    /// <summary>Bounds outline memory per tile and total work across one painted stroke.</summary>
    internal sealed class DashBudget {
        private readonly double _maximumWork;
        private long _work;
        private int _pieces;
        internal DashBudget(int pixelWidth, int pixelHeight, double padding) {
            var tiles = Math.Ceiling(pixelWidth / (double)DashTileSize) * Math.Ceiling(pixelHeight / (double)DashTileSize);
            // Every tile repeats neighboring geometry within the miter/cap padding.
            // Reserve four painted entries per device pixel on both sides, while the
            // per-tile piece limit also bounds the aggregate allowance.
            var bleedWork = 8 * Math.Max(0, padding) * tiles;
            _maximumWork = Math.Min(tiles * MaximumDashPieces,
                Math.Max(500000, 4d * Math.Max(pixelWidth, pixelHeight)) + bleedWork);
        }
        internal void BeginTile() => _pieces = 0;
        internal void Consume(int count = 1) {
            _work += count; _pieces += count;
            if (_pieces > MaximumDashPieces || _work > _maximumWork)
                throw new InvalidOperationException("The stroke exceeds the raster dash-work limit.");
        }
    }

    /// <summary>Keeps small outlines whole and tiles strokes whose authored dash count is large.</summary>
    internal static bool ShouldTileDashes(IReadOnlyList<IReadOnlyList<ChartPoint>> paths, IReadOnlyList<double>? pattern,
        double width, double miter, double left, double top, double right, double bottom) {
        if (pattern == null || !TryDashPattern(pattern, out var lengths, out var total)) return false;
        var padding = DashPadding(width, miter);
        double pieces = 0;
        foreach (var path in paths) {
            if (path == null) continue;
            for (var i = 1; i < path.Count; i++) {
                if (!ClipDashSegment(path[i - 1], path[i], left - padding, top - padding, right + padding, bottom + padding,
                    out var first, out var last)) continue;
                var length = Distance(path[i - 1], path[i]) * (last - first);
                if (!(length > 0) || double.IsInfinity(length)) continue;
                pieces += length / total * (lengths.Count / 2) + 2;
                if (pieces > MaximumDashPieces) return true;
            }
        }
        return false;
    }
}

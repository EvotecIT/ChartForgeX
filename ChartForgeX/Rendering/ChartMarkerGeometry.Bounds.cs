using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartMarkerGeometry {
    /// <summary>Resolves physical extrema from the immutable numeric path; the radius may be an area-normalization multiplier.</summary>
    internal static ChartRect Bounds(ChartMarkerOptions options, double x, double y, double radius) {
        if (options.CustomPath == null) return new ChartRect(x - radius, y - radius, radius * 2, radius * 2);
        var unit = options.CustomPath.Bounds;
        return new ChartRect(x + unit.X * radius, y + unit.Y * radius, unit.Width * radius, unit.Height * radius);
    }

    /// <summary>Conservative symmetric paint padding about the source point, derived from actual cubic extrema.</summary>
    internal static double Extent(ChartMarkerOptions options, double radius) {
        if (options.CustomPath == null) return radius;
        var bounds = options.CustomPath.Bounds;
        return Math.Max(Math.Max(Math.Abs(bounds.Left), Math.Abs(bounds.Right)), Math.Max(Math.Abs(bounds.Top), Math.Abs(bounds.Bottom))) * radius;
    }
}

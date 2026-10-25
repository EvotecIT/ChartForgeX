using System;

namespace ChartForgeX.Raster;

/// <summary>One exact device-grid check shared by explicit mask and layer-composition producers.</summary>
internal static class RasterEffectGrid {
    internal static int Coordinate(double value, int density, string parameter) {
        var device = value * density;
        if (density <= 0 || double.IsNaN(device) || double.IsInfinity(device) || device < int.MinValue || device > int.MaxValue
            || device != Math.Truncate(device))
            throw new ArgumentOutOfRangeException(parameter, "Explicit effects require finite positions aligned to their device grid.");
        return (int)device;
    }
    /// <summary>Outward device coverage for measured numeric ink/support; never rounds inward.</summary>
    internal static RasterEffectRegion Cover(ChartForgeX.Primitives.ChartRect bounds, int density) {
        if (density <= 0) throw new ArgumentOutOfRangeException(nameof(density));
        var x = Math.Floor(bounds.Left * density);
        var y = Math.Floor(bounds.Top * density);
        var right = Math.Ceiling(bounds.Right * density);
        var bottom = Math.Ceiling(bounds.Bottom * density);
        if (!Finite(x) || !Finite(y) || !Finite(right) || !Finite(bottom) ||
            x < int.MinValue || y < int.MinValue || right > int.MaxValue || bottom > int.MaxValue ||
            right <= x || bottom <= y) throw new ArgumentOutOfRangeException(nameof(bounds));
        return new RasterEffectRegion(checked((int)x), checked((int)y),
            checked((int)(right - x)), checked((int)(bottom - y)));
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

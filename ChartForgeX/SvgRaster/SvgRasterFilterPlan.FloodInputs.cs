using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    /// <summary>Shares the existing finite flood, style cascade and device-region admission between blend and alpha composition.</summary>
    private static bool TryFloodInputs(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out RasterEffectRegion region, out ChartColor color) {
        region = default;
        color = default;
        if (filter.Get("filterUnits") != "userSpaceOnUse" || filter.Get("primitiveUnits") != "userSpaceOnUse"
            || filter.Get("href") != null || filter.Get("transform") != null || filter.Get("mask") != null) return false;
        var filterStyle = SvgRasterStyle.ResolveWithAncestors(filter, definitions.StyleSheet, definitions.AncestorsFor(filter));
        if (!filterStyle.Displayed || filterStyle.Opacity != 1 || HasFilter(filterStyle.Filter) || filterStyle.ClipPath != null) return false;
        var flood = filter.Children[0];
        SvgRasterStyle? floodStyle = null;
        foreach (var primitive in filter.Children) {
            if (primitive.Children.Count != 0 || primitive.Get("x") != null || primitive.Get("y") != null
                || primitive.Get("width") != null || primitive.Get("height") != null || primitive.Get("href") != null
                || primitive.Get("transform") != null || primitive.Get("mask") != null) return false;
            var style = SvgRasterStyle.ResolveWithAncestors(primitive, definitions.StyleSheet, definitions.AncestorsFor(primitive));
            if (!style.Displayed || style.Opacity != 1 || HasFilter(style.Filter) || style.ClipPath != null
                || !string.Equals(style.ColorInterpolationFilters, "sRGB", StringComparison.OrdinalIgnoreCase)) return false;
            if (ReferenceEquals(primitive, flood)) floodStyle = style;
        }
        if (!TryFlood(flood, floodStyle!, definitions, out color)) return false;
        if (!Finite(matrix.A) || !Finite(matrix.D) || !Finite(matrix.E) || !Finite(matrix.F)
            || matrix.A <= 0 || matrix.D <= 0 || matrix.B != 0 || matrix.C != 0) return false;
        if (!Number(filter.Get("x"), out var x) || !Number(filter.Get("y"), out var y)
            || !Number(filter.Get("width"), out var width) || !Number(filter.Get("height"), out var height) || width <= 0 || height <= 0
            || !Pixel(x * matrix.A + matrix.E, out var left) || !Pixel(y * matrix.D + matrix.F, out var top)
            || !Pixel(width * matrix.A, out var pixelsWide) || !Pixel(height * matrix.D, out var pixelsHigh)) return false;
        region = new RasterEffectRegion(left, top, pixelsWide, pixelsHigh);
        return true;
    }
}

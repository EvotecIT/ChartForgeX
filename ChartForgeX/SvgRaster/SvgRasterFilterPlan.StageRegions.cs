using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    private static bool TryStageFrame(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out RasterEffectRegion frame) {
        frame = default;
        if (filter.Get("filterUnits") != "userSpaceOnUse" || filter.Get("primitiveUnits") != "userSpaceOnUse"
            || filter.Get("href") != null || filter.Get("transform") != null || filter.Get("mask") != null || filter.Get("filterRes") != null) return false;
        var style = SvgRasterStyle.ResolveWithAncestors(filter, definitions.StyleSheet, definitions.AncestorsFor(filter));
        if (!style.Displayed || style.Opacity != 1 || HasFilter(style.Filter) || style.ClipPath != null) return false;
        if (!Finite(matrix.A) || !Finite(matrix.D) || !Finite(matrix.E) || !Finite(matrix.F)
            || matrix.A <= 0 || matrix.D <= 0 || matrix.B != 0 || matrix.C != 0) return false;
        if (!Number(filter.Get("x"), out var x) || !Number(filter.Get("y"), out var y)
            || !Number(filter.Get("width"), out var width) || !Number(filter.Get("height"), out var height) || width <= 0 || height <= 0
            || !Pixel(x * matrix.A + matrix.E, out var left) || !Pixel(y * matrix.D + matrix.F, out var top)
            || !Pixel(width * matrix.A, out var pixelsWide) || !Pixel(height * matrix.D, out var pixelsHigh)) return false;
        try { frame = new RasterEffectRegion(left, top, pixelsWide, pixelsHigh); return true; }
        catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) { return false; }
    }

    private static bool TryStageClip(SvgRasterElement primitive, SvgRasterMatrix matrix, RasterEffectRegion frame,
        List<SvgRasterFilterStage> stages, int[] inputs, out SvgRasterFilterSubregion subregion, out RasterEffectRegion? clip) {
        // Defaults use the declared input subregions, not their clipped pixels. Two off-frame inputs may
        // have a union crossing the frame, and an arithmetic constant can generate coverage in that union.
        var defaults = default(SvgRasterFilterSubregion);
        if (inputs.Length == 0) defaults = new SvgRasterFilterSubregion(0, 0, frame.Width, frame.Height);
        foreach (var input in inputs) {
            if (input < 0) { defaults = new SvgRasterFilterSubregion(0, 0, frame.Width, frame.Height); break; }
            defaults = SvgRasterFilterSubregion.Union(defaults, stages[input].Subregion);
        }
        var x = defaults.X; var y = defaults.Y; var width = defaults.Width; var height = defaults.Height;
        subregion = default; clip = null;
        bool Position(string name, double scale, double translation, int origin, ref long output) {
            if (primitive.Get(name) == null) return true;
            if (!Number(primitive.Get(name), out var value) || !Pixel(value * scale + translation, out var device)) return false;
            output = (long)device - origin; return true;
        }
        bool Length(string name, double scale, ref long output) {
            if (primitive.Get(name) == null) return true;
            if (!Number(primitive.Get(name), out var value) || !Pixel(value * scale, out var device)) return false;
            output = device; return true;
        }
        if (!Position("x", matrix.A, matrix.E, frame.X, ref x) || !Position("y", matrix.D, matrix.F, frame.Y, ref y)
            || !Length("width", matrix.A, ref width) || !Length("height", matrix.D, ref height)) return false;
        subregion = new SvgRasterFilterSubregion(x, y, width, height);
        if (subregion.Empty) return true;
        var right = Math.Min((long)frame.Width, checked(x + width)); var bottom = Math.Min((long)frame.Height, checked(y + height));
        var left = Math.Max(0, x); var top = Math.Max(0, y);
        if (right > left && bottom > top) clip = new RasterEffectRegion(checked((int)left), checked((int)top),
            checked((int)(right - left)), checked((int)(bottom - top)));
        return true;
    }
}

/// <summary>Unclipped primitive rectangle metadata; pixel allocations still use only the admitted filter frame.</summary>
internal readonly struct SvgRasterFilterSubregion {
    internal SvgRasterFilterSubregion(long x, long y, long width, long height) { X = x; Y = y; Width = width; Height = height; }
    internal long X { get; }
    internal long Y { get; }
    internal long Width { get; }
    internal long Height { get; }
    internal bool Empty => Width <= 0 || Height <= 0;
    internal static SvgRasterFilterSubregion Union(SvgRasterFilterSubregion a, SvgRasterFilterSubregion b) {
        if (a.Empty) return b; if (b.Empty) return a;
        var x = Math.Min(a.X, b.X); var y = Math.Min(a.Y, b.Y);
        return new SvgRasterFilterSubregion(x, y, checked(Math.Max(checked(a.X + a.Width), checked(b.X + b.Width)) - x),
            checked(Math.Max(checked(a.Y + a.Height), checked(b.Y + b.Height)) - y));
    }
}

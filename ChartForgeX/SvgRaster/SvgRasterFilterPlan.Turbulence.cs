using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    internal bool IsProcedural => _effect?.TurbulenceData != null;
    internal IReadOnlyList<ChartPoint>? ProceduralClipContour { get; }
    private SvgRasterFilterPlan(RasterEffectRegion filter, VisualTurbulence turbulence, ChartRect tile, double scaleX, double scaleY, CancellationToken cancellation = default) {
        FilterRegion = CaptureRegion = filter;
        _effect = RasterEffectPlan.CreateTurbulence(turbulence, tile,
            new RasterEffectRegion(0, 0, filter.Width, filter.Height), 1, cancellation,
            sampleStepX: scaleX == 1 ? (double?)null : 1 / scaleX,
            sampleStepY: scaleY == 1 ? (double?)null : 1 / scaleY);
    }

    /// <summary>One source-independent primitive over explicit user-space output; no SourceGraphic is rendered or allocated.</summary>
    private static bool TryTurbulence(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason) {
        plan = null;
        reason = "Turbulence requires one explicit user-space primitive without a graph, external reference or transformed definition.";
        if (filter.Children.Count != 1 || filter.Children[0].Name != "feTurbulence" ||
            filter.Get("filterUnits") != "userSpaceOnUse" || filter.Get("primitiveUnits") != "userSpaceOnUse" ||
            filter.Get("href") != null || filter.Get("transform") != null || filter.Get("mask") != null) return false;
        var primitive = filter.Children[0];
        if (primitive.Children.Count != 0 || primitive.Get("in") != null || primitive.Get("in2") != null ||
            primitive.Get("x") != null || primitive.Get("y") != null || primitive.Get("width") != null || primitive.Get("height") != null ||
            primitive.Get("href") != null || primitive.Get("transform") != null || primitive.Get("mask") != null) return false;
        var filterStyle = SvgRasterStyle.ResolveWithAncestors(filter, definitions.StyleSheet, definitions.AncestorsFor(filter));
        var style = SvgRasterStyle.ResolveWithAncestors(primitive, definitions.StyleSheet, definitions.AncestorsFor(primitive));
        if (!filterStyle.Displayed || !style.Displayed || filterStyle.Opacity != 1 || style.Opacity != 1 ||
            HasFilter(filterStyle.Filter) || HasFilter(style.Filter) || filterStyle.ClipPath != null || style.ClipPath != null) return false;
        // Resolve the definition ancestry, not the referencing element. Filter interpolation defaults to linearRGB.
        var space = style.ColorInterpolationFilters;
        VisualFilterColorSpace colorSpace;
        if (space == null || string.Equals(space, "linearRGB", StringComparison.OrdinalIgnoreCase)) colorSpace = VisualFilterColorSpace.LinearRgb;
        else if (string.Equals(space, "sRGB", StringComparison.OrdinalIgnoreCase)) colorSpace = VisualFilterColorSpace.Srgb;
        else { reason = "The computed turbulence filter color space must be sRGB or linearRGB."; return false; }
        reason = "The procedural region and its finite invertible affine transform must fit native device and lattice limits.";
        if (!FiniteMatrix(matrix)) return false;
        if (!Number(filter.Get("x"), out var x) || !Number(filter.Get("y"), out var y) ||
            !Number(filter.Get("width"), out var width) || !Number(filter.Get("height"), out var height) || width <= 0 || height <= 0) return false;
        var legacyAxis = matrix.A > 0 && matrix.D > 0 && matrix.B == 0 && matrix.C == 0 &&
            Pixel(x * matrix.A + matrix.E, out _) && Pixel(y * matrix.D + matrix.F, out _) &&
            Pixel(width * matrix.A, out _) && Pixel(height * matrix.D, out _);
        var inverse = SvgRasterMatrix.Identity;
        if (!legacyAxis && (!matrix.TryInvert(out inverse) || !FiniteMatrix(inverse))) return false;
        reason = "Turbulence type, stitch mode, frequencies, octave count and seed must be finite supported scalar values.";
        var typeText = primitive.Get("type") ?? "turbulence";
        if (typeText != "turbulence" && typeText != "fractalNoise") return false;
        var stitchText = primitive.Get("stitchTiles") ?? "noStitch";
        if (stitchText != "stitch" && stitchText != "noStitch") return false;
        var frequencies = (primitive.Get("baseFrequency") ?? "0").Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (frequencies.Length < 1 || frequencies.Length > 2 || !Number(frequencies[0], out var fx) ||
            !Number(frequencies.Length == 1 ? frequencies[0] : frequencies[1], out var fy) || fx < 0 || fy < 0) return false;
        if (!int.TryParse(primitive.Get("numOctaves") ?? "1", NumberStyles.Integer, CultureInfo.InvariantCulture, out var octaves) || octaves < 0 ||
            !Number(primitive.Get("seed") ?? "0", out var seed)) return false;
        try {
            var effect = new VisualTurbulence(fx, fy, octaves, seed,
                typeText == "fractalNoise" ? VisualTurbulenceType.FractalNoise : VisualTurbulenceType.Turbulence,
                stitchText == "stitch", colorSpace);
            var tile = new ChartRect(x, y, width, height);
            if (legacyAxis) {
                // Preserve the complete incumbent axis/default arithmetic, including null density division.
                Pixel(x * matrix.A + matrix.E, out var left); Pixel(y * matrix.D + matrix.F, out var top);
                Pixel(width * matrix.A, out var pixelWidth); Pixel(height * matrix.D, out var pixelHeight);
                plan = new SvgRasterFilterPlan(new RasterEffectRegion(left, top, pixelWidth, pixelHeight), effect, tile, matrix.A, matrix.D, definitions.Filters?.Cancellation ?? default);
            } else plan = CreateAffineTurbulence(effect, tile, matrix, inverse, definitions.Filters?.Cancellation ?? default);
            reason = string.Empty;
            return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) {
            reason = "Turbulence output, sampling coordinates or setup exceed the existing native raster safety limits.";
            return false;
        }
    }
}

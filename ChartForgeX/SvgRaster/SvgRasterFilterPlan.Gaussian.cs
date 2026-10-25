using System;
using System.Globalization;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

/// <summary>Finite opt-in source filters lowered to the existing native raster plan.</summary>
internal sealed partial class SvgRasterFilterPlan {
    internal RasterEffectRegion FilterRegion { get; }
    internal RasterEffectRegion CaptureRegion { get; }
    internal RasterEffectPlan Effect { get; }
    internal bool IsZero { get; }
    /// <summary>Nonzero single Gaussian output retains the incumbent image-composition conversion; typed finite operations do not.</summary>
    internal bool UsesGaussianImageComposition { get; }
    private SvgRasterFilterPlan(RasterEffectRegion filter, int xQ8, int yQ8) {
        FilterRegion = filter;
        var support = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), xQ8, yQ8).OutputRegion;
        int haloX = -support.X, haloY = -support.Y;
        CaptureRegion = new RasterEffectRegion(checked(filter.X - haloX), checked(filter.Y - haloY),
            checked(filter.Width + haloX * 2), checked(filter.Height + haloY * 2));
        Effect = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, CaptureRegion.Width, CaptureRegion.Height), xQ8, yQ8);
        IsZero = xQ8 == 0 && yQ8 == 0;
        UsesGaussianImageComposition = !IsZero;
    }

    internal static bool TryCreate(string reference, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason) {
        plan = null; reason = "Only one local filter reference is supported.";
        if (!TryLocalReference(reference, out var id) || !definitions.TryGetElement(id, out var filter) || filter.Name != "filter") return false;
        // Existing single-Gaussian grammar retains its own admission and arithmetic unchanged.
        if (filter.Children.Count == 1 && filter.Children[0].Name == "feGaussianBlur")
            return TryGaussian(reference, matrix, definitions, out plan, out reason);
        if (filter.Children.Count == 2 && filter.Children[0].Name == "feFlood" && filter.Children[1].Name == "feBlend")
            return TryFloodBlend(filter, matrix, definitions, out plan, out reason);
        if (filter.Children.Count == 2 && filter.Children[0].Name == "feFlood" && filter.Children[1].Name == "feComposite")
            return TryFloodComposite(filter, matrix, definitions, out plan, out reason);
        if (filter.Children.Count == 6 && filter.Children[5].Name == "feComposite")
            return TryShadow(filter, matrix, definitions, out plan, out reason, innerShadow: true);
        return TryShadow(filter, matrix, definitions, out plan, out reason);
    }

    private static bool TryGaussian(string reference, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason) {
        plan = null; reason = "Only one local filter reference is supported.";
        if (!TryLocalReference(reference, out var id) || !definitions.TryGetElement(id, out var filter) || filter.Name != "filter") return false;
        reason = "A single explicit user-space SourceGraphic Gaussian primitive is required.";
        if (filter.Children.Count != 1 || filter.Children[0].Name != "feGaussianBlur" || filter.Get("href") != null || filter.Get("transform") != null
            || filter.Get("filterUnits") != "userSpaceOnUse" || filter.Get("primitiveUnits") != "userSpaceOnUse") return false;
        var primitive = filter.Children[0];
        if (primitive.Children.Count != 0 || primitive.Get("in") != "SourceGraphic" || primitive.Get("edgeMode") != "none"
            || primitive.Get("x") != null || primitive.Get("y") != null || primitive.Get("width") != null || primitive.Get("height") != null
            || primitive.Get("result") != null || primitive.Get("transform") != null) return false;
        var filterStyle = SvgRasterStyle.ResolveWithAncestors(filter, definitions.StyleSheet, definitions.AncestorsFor(filter));
        var primitiveAncestors = new List<SvgRasterElement>(definitions.AncestorsFor(filter)) { filter };
        var primitiveStyle = SvgRasterStyle.Resolve(filterStyle, primitive, definitions.StyleSheet, primitiveAncestors);
        reason = "The primitive's computed color-interpolation-filters must be sRGB; linearRGB and implicit defaults remain unsupported.";
        if (!string.Equals(primitiveStyle.ColorInterpolationFilters, "sRGB", StringComparison.OrdinalIgnoreCase)) return false;
        reason = "Filter definition effects and hidden primitives are outside this subset.";
        if (!filterStyle.Displayed || !primitiveStyle.Displayed || filterStyle.Opacity != 1 || primitiveStyle.Opacity != 1
            || HasFilter(filterStyle.Filter) || HasFilter(primitiveStyle.Filter)) return false;
        reason = "Only positive axis-aligned scale and translation are supported on the filtered element.";
        if (!Finite(matrix.A) || !Finite(matrix.D) || !Finite(matrix.E) || !Finite(matrix.F)
            || matrix.A <= 0 || matrix.D <= 0 || matrix.B != 0 || matrix.C != 0) return false;
        reason = "The explicit filter region must contain finite numeric user-space lengths and align to device pixels.";
        if (!Number(filter.Get("x"), out var x) || !Number(filter.Get("y"), out var y)
            || !Number(filter.Get("width"), out var width) || !Number(filter.Get("height"), out var height) || width <= 0 || height <= 0
            || !Pixel(x * matrix.A + matrix.E, out var left) || !Pixel(y * matrix.D + matrix.F, out var top)
            || !Pixel(width * matrix.A, out var pixelWidth) || !Pixel(height * matrix.D, out var pixelHeight)) return false;
        reason = "stdDeviation must be one or two finite nonnegative numeric values, at most 128 device pixels per axis.";
        var values = (primitive.Get("stdDeviation") ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length < 1 || values.Length > 2 || !Number(values[0], out var sigmaX)
            || !Number(values.Length == 1 ? values[0] : values[1], out var sigmaY)
            || !Sigma(sigmaX * matrix.A, out var xQ8) || !Sigma(sigmaY * matrix.D, out var yQ8)) return false;
        try {
            plan = new SvgRasterFilterPlan(new RasterEffectRegion(left, top, pixelWidth, pixelHeight), xQ8, yQ8);
            reason = string.Empty; return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) {
            reason = "Filter capture and propagated support exceed the existing native raster dimension or pixel limits.";
            return false;
        }
    }

    internal static bool HasFilter(string? value) => !string.IsNullOrWhiteSpace(value) && !string.Equals(value!.Trim(), "none", StringComparison.OrdinalIgnoreCase);
    private static bool TryLocalReference(string value, out string? id) {
        id = null; var text = value.Trim();
        if (!text.StartsWith("url(", StringComparison.OrdinalIgnoreCase) || !text.EndsWith(")", StringComparison.Ordinal) || text.IndexOf(')') != text.Length - 1) return false;
        var body = text.Substring(4, text.Length - 5).Trim();
        if (body.Length >= 2 && ((body[0] == '\'' && body[body.Length - 1] == '\'') || (body[0] == '"' && body[body.Length - 1] == '"'))) body = body.Substring(1, body.Length - 2);
        if (body.Length < 2 || body[0] != '#' || body.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\'', '"', '(', ')' }) >= 0) return false;
        id = body.Substring(1); return true;
    }
    private static bool Number(string? value, out double result) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && Finite(result);
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static bool Pixel(double value, out int result) {
        result = 0; if (!Finite(value) || value < int.MinValue || value > int.MaxValue || value != Math.Truncate(value)) return false;
        result = (int)value; return true;
    }
    private static bool Sigma(double value, out int q8) {
        q8 = 0; if (!Finite(value) || value < 0 || value > 128) return false;
        q8 = (int)Math.Floor(value * 256 + .5); return true;
    }
}

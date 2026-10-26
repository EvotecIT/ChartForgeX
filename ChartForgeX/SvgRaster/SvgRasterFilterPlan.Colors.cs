using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    private SvgRasterFilterPlan(RasterEffectRegion region, RasterColorTransform transform) {
        FilterRegion = CaptureRegion = region;
        _effect = RasterEffectPlan.TransformColor(new RasterEffectRegion(0, 0, region.Width, region.Height), transform);
        IsZero = false;
    }

    private static bool TryColorTransform(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason) {
        plan = null;
        reason = "Color primitives require one explicit SourceGraphic input followed only by ordered immediately preceding results, in an aligned user-space region.";
        if (filter.Children.Count == 0 || filter.Get("filterUnits") != "userSpaceOnUse" || filter.Get("primitiveUnits") != "userSpaceOnUse"
            || filter.Get("href") != null || filter.Get("transform") != null || filter.Get("mask") != null || filter.Get("filterRes") != null) return false;
        var filterStyle = SvgRasterStyle.ResolveWithAncestors(filter, definitions.StyleSheet, definitions.AncestorsFor(filter));
        if (!filterStyle.Displayed || filterStyle.Opacity != 1 || HasFilter(filterStyle.Filter) || filterStyle.ClipPath != null) return false;
        if (!Finite(matrix.A) || !Finite(matrix.D) || !Finite(matrix.E) || !Finite(matrix.F)
            || matrix.A <= 0 || matrix.D <= 0 || matrix.B != 0 || matrix.C != 0) return false;
        if (!Number(filter.Get("x"), out var x) || !Number(filter.Get("y"), out var y)
            || !Number(filter.Get("width"), out var width) || !Number(filter.Get("height"), out var height) || width <= 0 || height <= 0
            || !Pixel(x * matrix.A + matrix.E, out var left) || !Pixel(y * matrix.D + matrix.F, out var top)
            || !Pixel(width * matrix.A, out var pixelsWide) || !Pixel(height * matrix.D, out var pixelsHigh)) return false;
        try {
            definitions.Filters!.Cancellation.ThrowIfCancellationRequested();
            var region = new RasterEffectRegion(left, top, pixelsWide, pixelsHigh);
            var stages = new RasterColorTransform[filter.Children.Count];
            var names = new HashSet<string>(StringComparer.Ordinal);
            string input = "SourceGraphic";
            for (var index = 0; index < stages.Length; index++) {
                definitions.Filters!.Cancellation.ThrowIfCancellationRequested();
                var primitive = filter.Children[index];
                reason = "Each color stage must consume only the immediately preceding unique result; the final stage has no result name.";
                if ((primitive.Name != "feColorMatrix" && primitive.Name != "feComponentTransfer")
                    || primitive.Get("in") != input || primitive.Get("in2") != null
                    || primitive.Get("x") != null || primitive.Get("y") != null || primitive.Get("width") != null || primitive.Get("height") != null
                    || primitive.Get("transform") != null || primitive.Get("href") != null || primitive.Get("mask") != null) return false;
                var result = primitive.Get("result");
                if (index + 1 == stages.Length) { if (result != null) return false; }
                else {
                    if (!ResultName(result) || !names.Add(result!)) return false;
                    input = result!;
                }
                if (!TryColorStage(primitive, definitions, out stages[index], out reason)) return false;
            }
            // Flat stages have no per-stage pixel surfaces. Existing work admission scales with the actual stage count.
            var transform = stages.Length == 1 ? stages[0] : new RasterColorTransform(stages);
            plan = new SvgRasterFilterPlan(region, transform);
            reason = string.Empty; return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) {
            reason = "Color sequence region or parameters exceed finite native admission."; return false;
        }
    }

    private static bool TryColorStage(SvgRasterElement primitive, SvgRasterDefinitions definitions,
        out RasterColorTransform transform, out string reason) {
        transform = null!;
        reason = "Each color stage requires finite explicit parameters and computed sRGB or linearRGB without nested effects.";
        var style = SvgRasterStyle.ResolveWithAncestors(primitive, definitions.StyleSheet, definitions.AncestorsFor(primitive));
        if (!style.Displayed || style.Opacity != 1 || HasFilter(style.Filter) || style.ClipPath != null) return false;
        var space = style.ColorInterpolationFilters;
        bool linearRgb;
        if (space == null || string.Equals(space, "linearRGB", StringComparison.OrdinalIgnoreCase)) linearRgb = true;
        else if (string.Equals(space, "sRGB", StringComparison.OrdinalIgnoreCase)) linearRgb = false;
        else return false;
        if (primitive.Name == "feColorMatrix") {
            if (primitive.Children.Count != 0 || !TryColorMatrix(primitive, out var values)) return false;
            transform = new RasterColorTransform(values, linearRgb);
        } else {
            var functions = new RasterComponentTransfer[4];
            foreach (var child in primitive.Children) {
                definitions.Filters!.Cancellation.ThrowIfCancellationRequested();
                int channel;
                switch (child.Name) { case "feFuncR": channel = 0; break; case "feFuncG": channel = 1; break;
                    case "feFuncB": channel = 2; break; case "feFuncA": channel = 3; break; default: return false; }
                if (child.Children.Count != 0 || child.Get("href") != null || child.Get("transform") != null
                    || !TryComponentFunction(child, out functions[channel])) return false;
            }
            transform = new RasterColorTransform(functions, linearRgb);
        }
        reason = string.Empty; return true;
    }

    private static bool TryComponentFunction(SvgRasterElement node, out RasterComponentTransfer function) {
        function = default;
        switch (node.Get("type") ?? "identity") {
            case "identity": return true;
            case "table": case "discrete":
                if (!ColorNumbers(node.Get("tableValues"), 256, out var table)) return false;
                function = new RasterComponentTransfer(node.Get("type") == "table" ? RasterTransferKind.Table : RasterTransferKind.Discrete, table); return true;
            case "linear":
                if (!ColorNumber(node, "slope", 1, out var slope) || !ColorNumber(node, "intercept", 0, out var intercept)
                    || !Finite(Math.Abs(slope) + Math.Abs(intercept))) return false;
                function = new RasterComponentTransfer(RasterTransferKind.Linear, first: slope, second: intercept); return true;
            case "gamma":
                if (!ColorNumber(node, "amplitude", 1, out var amplitude) || !ColorNumber(node, "exponent", 1, out var exponent)
                    || !ColorNumber(node, "offset", 0, out var offset)) return false;
                function = new RasterComponentTransfer(RasterTransferKind.Gamma, first: amplitude, second: exponent, third: offset); return true;
            default: return false;
        }
    }
    private static bool ColorNumber(SvgRasterElement node, string name, double fallback, out double value) {
        value = fallback; return node.Get(name) == null || Number(node.Get(name), out value);
    }
    private static bool ColorNumbers(string? text, int maximum, out double[] values) {
        var tokens = Tokens(text); values = Array.Empty<double>();
        if (tokens.Length > maximum) return false;
        values = new double[tokens.Length];
        for (var index = 0; index < tokens.Length; index++) if (!Number(tokens[index], out values[index])) return false;
        return true;
    }
}

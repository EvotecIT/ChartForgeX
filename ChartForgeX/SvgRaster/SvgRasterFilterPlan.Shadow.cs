using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    private SvgRasterFilterPlan(RasterEffectRegion filter, int xQ8, int yQ8, int offsetX, int offsetY, int spread, ChartColor color, bool innerShadow = false) {
        FilterRegion = filter;
        var support = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), xQ8, yQ8).OutputRegion;
        var haloX = checked(-support.X + Math.Abs(spread)); var haloY = checked(-support.Y + Math.Abs(spread));
        // Union the original-source request with the inverse propagated shadow input region.
        var left = Math.Min(filter.X, checked(filter.X - offsetX - haloX));
        var top = Math.Min(filter.Y, checked(filter.Y - offsetY - haloY));
        var right = Math.Max(checked(filter.X + filter.Width), checked(filter.X + filter.Width - offsetX + haloX));
        var bottom = Math.Max(checked(filter.Y + filter.Height), checked(filter.Y + filter.Height - offsetY + haloY));
        CaptureRegion = new RasterEffectRegion(left, top, checked(right - left), checked(bottom - top));
        var source = new RasterEffectRegion(0, 0, CaptureRegion.Width, CaptureRegion.Height);
        Effect = innerShadow
            ? RasterEffectPlan.InnerShadow(source, xQ8, yQ8, offsetX, offsetY, color, -spread)
            : RasterEffectPlan.LayerShadow(source, xQ8, yQ8, offsetX, offsetY, color, spread);
        IsZero = color.A == 0;
    }

    private static bool TryShadow(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason, bool innerShadow = false) {
        plan = null;
        reason = innerShadow
            ? "Only the explicit SourceAlpha morphology/Gaussian/offset/flood/out/atop inner-shadow chain is supported."
            : "Only the explicit SourceAlpha morphology/Gaussian/offset/flood/in/merge shadow chain is supported beside single SourceGraphic Gaussian.";
        var names = new[] { "feMorphology", "feGaussianBlur", "feOffset", "feFlood", "feComposite", innerShadow ? "feComposite" : "feMerge" };
        if (filter.Children.Count != names.Length || filter.Get("href") != null || filter.Get("transform") != null
            || filter.Get("filterUnits") != "userSpaceOnUse" || filter.Get("primitiveUnits") != "userSpaceOnUse") return false;
        var filterStyle = SvgRasterStyle.ResolveWithAncestors(filter, definitions.StyleSheet, definitions.AncestorsFor(filter));
        if (!filterStyle.Displayed || filterStyle.Opacity != 1 || HasFilter(filterStyle.Filter) || filterStyle.ClipPath != null || filter.Get("mask") != null) return false;
        var resultNames = new HashSet<string>(StringComparer.Ordinal);
        var styles = new SvgRasterStyle[names.Length];
        for (var index = 0; index < names.Length; index++) {
            var node = filter.Children[index];
            if (node.Name != names[index] || node.Get("x") != null || node.Get("y") != null || node.Get("width") != null
                || node.Get("height") != null || node.Get("transform") != null || node.Get("href") != null || node.Get("mask") != null) return false;
            var ancestors = new List<SvgRasterElement>(definitions.AncestorsFor(node));
            var style = styles[index] = SvgRasterStyle.ResolveWithAncestors(node, definitions.StyleSheet, ancestors);
            if (!style.Displayed || style.Opacity != 1 || HasFilter(style.Filter) || style.ClipPath != null) return false;
            if (!string.Equals(style.ColorInterpolationFilters, "sRGB", StringComparison.OrdinalIgnoreCase)) {
                reason = "Every finite shadow primitive requires computed sRGB filter color space."; return false;
            }
            if (index < names.Length - 1) {
                var result = node.Get("result");
                if (node.Children.Count != 0 || !ResultName(result) || !resultNames.Add(result!)) {
                    reason = "Finite shadow results must be explicit unique non-reserved tokens."; return false;
                }
            }
        }
        var morphology = filter.Children[0]; var blur = filter.Children[1]; var offset = filter.Children[2];
        var flood = filter.Children[3]; var composite = filter.Children[4]; var merge = filter.Children[5];
        if (morphology.Get("in") != "SourceAlpha" || morphology.Get("in2") != null
            || blur.Get("in") != morphology.Get("result") || blur.Get("in2") != null || blur.Get("edgeMode") != "none"
            || offset.Get("in") != blur.Get("result") || offset.Get("in2") != null
            || flood.Get("in") != null || flood.Get("in2") != null
            || composite.Get("in") != flood.Get("result") || composite.Get("in2") != offset.Get("result")
            || composite.Get("operator") != (innerShadow ? "out" : "in")) return false;
        if (innerShadow) {
            if (merge.Get("in") != composite.Get("result") || merge.Get("in2") != "SourceGraphic"
                || merge.Get("operator") != "atop" || merge.Get("result") != null || merge.Children.Count != 0) return false;
        } else if (merge.Get("in") != null || merge.Get("in2") != null || merge.Get("result") != null || merge.Children.Count != 2
            || merge.Children[0].Name != "feMergeNode" || merge.Children[1].Name != "feMergeNode"
            || merge.Children[0].Get("in") != composite.Get("result") || merge.Children[1].Get("in") != "SourceGraphic") return false;
        foreach (var child in merge.Children) {
            if (child.Children.Count != 0 || child.Get("transform") != null || child.Get("x") != null || child.Get("y") != null
                || child.Get("width") != null || child.Get("height") != null || child.Get("result") != null || child.Get("mask") != null) return false;
            var style = SvgRasterStyle.ResolveWithAncestors(child, definitions.StyleSheet, definitions.AncestorsFor(child));
            if (!style.Displayed || style.Opacity != 1 || HasFilter(style.Filter) || style.ClipPath != null) return false;
        }
        reason = "Only positive axis-aligned scale and translation with device-aligned filter/offset/morphology lengths are supported.";
        if (!Finite(matrix.A) || !Finite(matrix.D) || !Finite(matrix.E) || !Finite(matrix.F) || matrix.A <= 0 || matrix.D <= 0 || matrix.B != 0 || matrix.C != 0) return false;
        if (!Number(filter.Get("x"), out var x) || !Number(filter.Get("y"), out var y) || !Number(filter.Get("width"), out var width)
            || !Number(filter.Get("height"), out var height) || width <= 0 || height <= 0
            || !Pixel(x * matrix.A + matrix.E, out var left) || !Pixel(y * matrix.D + matrix.F, out var top)
            || !Pixel(width * matrix.A, out var pixelWidth) || !Pixel(height * matrix.D, out var pixelHeight)
            || !Number(offset.Get("dx"), out var dx) || !Number(offset.Get("dy"), out var dy)
            || !Pixel(dx * matrix.A, out var offsetX) || !Pixel(dy * matrix.D, out var offsetY)) return false;
        var radii = Tokens(morphology.Get("radius"));
        if (radii.Length < 1 || radii.Length > 2 || !Number(radii[0], out var rx) || !Number(radii.Length == 1 ? radii[0] : radii[1], out var ry)
            || rx < 0 || ry < 0 || !Pixel(rx * matrix.A, out var radiusX) || !Pixel(ry * matrix.D, out var radiusY) || radiusX != radiusY) {
            reason = "Finite shadow morphology requires one equal integer device radius per axis."; return false;
        }
        var operation = morphology.Get("operator");
        if (operation != "dilate" && operation != "erode") return false;
        var spread = operation == "dilate" ? radiusX : -radiusX;
        var values = Tokens(blur.Get("stdDeviation"));
        if (values.Length < 1 || values.Length > 2 || !Number(values[0], out var sx) || !Number(values.Length == 1 ? values[0] : values[1], out var sy)
            || !Sigma(sx * matrix.A, out var xQ8) || !Sigma(sy * matrix.D, out var yQ8)) return false;
        if (!TryFlood(flood, styles[3], definitions, out var color)) {
            reason = "The flood requires a resolved solid color/currentColor and finite numeric opacity through the existing cascade."; return false;
        }
        try {
            plan = new SvgRasterFilterPlan(new RasterEffectRegion(left, top, pixelWidth, pixelHeight), xQ8, yQ8, offsetX, offsetY, spread, color, innerShadow);
            reason = string.Empty; return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) {
            reason = "Finite shadow capture, union and support exceed existing native raster region limits."; return false;
        }
    }

    private static bool TryFlood(SvgRasterElement flood, SvgRasterStyle style, SvgRasterDefinitions definitions, out ChartColor color) {
        SvgRasterPaintProperties.Resolve(flood, definitions.StyleSheet, definitions.AncestorsFor(flood), style,
            "flood-color", "flood-opacity", out var paintText, out var opacityText);
        var paint = SvgRasterPaint.Parse(paintText ?? string.Empty, style.Color,
            paintText == null ? SvgRasterPaint.Solid(ChartColor.Black) : SvgRasterPaint.None);
        var opacity = opacityText == null ? 1 : SvgRasterGradientValues.ParseCoordinate(opacityText, double.NaN);
        color = default;
        if (!paint.Color.HasValue || paint.IsReference || !Finite(opacity)) return false;
        color = SvgRasterPaintProperties.WithOpacity(paint.Color.Value, Math.Max(0, Math.Min(1, opacity))); return true;
    }
    private static bool ResultName(string? value) => !string.IsNullOrWhiteSpace(value) && value != "SourceGraphic" && value != "SourceAlpha"
        && value != "BackgroundImage" && value != "BackgroundAlpha" && value != "FillPaint" && value != "StrokePaint"
        && value!.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) < 0;
    private static string[] Tokens(string? value) => (value ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
}

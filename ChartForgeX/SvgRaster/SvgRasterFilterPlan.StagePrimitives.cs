using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    private static bool TryStagePrimitive(SvgRasterElement node, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        int index, Dictionary<string, int> names, out SvgRasterFilterStage stage, out string reason) {
        stage = new SvgRasterFilterStage();
        reason = "The primitive needs represented finite inputs, parameters and computed sRGB or linearRGB without nested effects.";
        if (node.Get("href") != null || node.Get("transform") != null || node.Get("mask") != null || node.Get("filterRes") != null) return false;
        var style = SvgRasterStyle.ResolveWithAncestors(node, definitions.StyleSheet, definitions.AncestorsFor(node));
        if (!style.Displayed || style.Opacity != 1 || HasFilter(style.Filter) || style.ClipPath != null) return false;
        if (style.ColorInterpolationFilters == null || string.Equals(style.ColorInterpolationFilters, "linearRGB", StringComparison.OrdinalIgnoreCase)) stage.LinearRgb = true;
        else if (!string.Equals(style.ColorInterpolationFilters, "sRGB", StringComparison.OrdinalIgnoreCase)) return false;
        var previous = index == 0 ? SourceGraphicInput : index - 1;
        if (node.Name == "feFlood") {
            if (node.Children.Count != 0 || node.Get("in") != null || node.Get("in2") != null || !TryStageFlood(node, style, definitions, stage.LinearRgb, out stage.Flood)) return false;
            stage.Operation = SvgRasterFilterOperation.Flood; return true;
        }
        if (node.Name == "feMerge") {
            if (node.Get("in") != null || node.Get("in2") != null) return false;
            var inputs = new List<int>();
            foreach (var child in node.Children) {
                definitions.Filters!.Cancellation.ThrowIfCancellationRequested();
                if (child.Name != "feMergeNode" || child.Children.Count != 0 || child.Get("in2") != null || child.Get("result") != null
                    || child.Get("href") != null || child.Get("transform") != null || child.Get("mask") != null
                    || child.Get("x") != null || child.Get("y") != null || child.Get("width") != null || child.Get("height") != null) return false;
                var childStyle = SvgRasterStyle.ResolveWithAncestors(child, definitions.StyleSheet, definitions.AncestorsFor(child));
                if (!childStyle.Displayed || childStyle.Opacity != 1 || HasFilter(childStyle.Filter) || childStyle.ClipPath != null
                    || !TryStageInput(child.Get("in"), previous, names, out var input)) return false;
                inputs.Add(input);
            }
            stage.Operation = SvgRasterFilterOperation.Merge; stage.Inputs = inputs.ToArray(); return true;
        }
        if (!TryStageInput(node.Get("in"), previous, names, out var first)) {
            reason = "Backdrop and paint built-ins are not captured by this ordered input owner."; return false;
        }
        stage.Inputs = new[] { first };
        if (node.Name == "feColorMatrix" || node.Name == "feComponentTransfer") {
            if (node.Get("in2") != null || !TryColorStage(node, definitions, out var transform, out reason)) return false;
            stage.Operation = SvgRasterFilterOperation.Color; stage.Color = transform; return true;
        }
        if (node.Children.Count != 0) return false;
        switch (node.Name) {
            case "feGaussianBlur":
                if (node.Get("in2") != null || (node.Get("edgeMode") ?? "none") != "none"
                    || !StagePair(node.Get("stdDeviation") ?? "0", out var sx, out var sy)
                    || !Sigma(sx * matrix.A, out stage.X) || !Sigma(sy * matrix.D, out stage.Y)) return false;
                stage.Operation = SvgRasterFilterOperation.Blur; return true;
            case "feOffset":
                if (node.Get("in2") != null || !ColorNumber(node, "dx", 0, out var dx) || !ColorNumber(node, "dy", 0, out var dy)
                    || !Pixel(dx * matrix.A, out stage.X) || !Pixel(dy * matrix.D, out stage.Y)) return false;
                stage.Operation = SvgRasterFilterOperation.Offset; return true;
            case "feMorphology":
                if (node.Get("in2") != null || !StagePair(node.Get("radius") ?? "0", out var rx, out var ry) || rx < 0 || ry < 0
                    || !Pixel(rx * matrix.A, out stage.X) || !Pixel(ry * matrix.D, out stage.Y)) return false;
                var operation = node.Get("operator") ?? "erode";
                if (operation != "dilate" && operation != "erode") return false;
                stage.Maximize = operation == "dilate"; stage.Operation = SvgRasterFilterOperation.Morphology; return true;
            case "feBlend":
            case "feComposite":
                if (!TryStageInput(node.Get("in2"), previous, names, out var second)) {
                    reason = "Backdrop and paint built-ins are not captured by this ordered input owner."; return false;
                }
                stage.Inputs = new[] { first, second };
                if (node.Name == "feBlend") {
                    if (node.Get("no-composite") != null || !TryBlendMode(node.Get("mode") ?? "normal", out stage.Blend)) return false;
                    stage.Operation = SvgRasterFilterOperation.Blend; return true;
                }
                var composite = node.Get("operator") ?? "over";
                if (composite == "arithmetic") {
                    if (!ColorNumber(node, "k1", 0, out stage.K1) || !ColorNumber(node, "k2", 0, out stage.K2)
                        || !ColorNumber(node, "k3", 0, out stage.K3) || !ColorNumber(node, "k4", 0, out stage.K4)) return false;
                    stage.Operation = SvgRasterFilterOperation.Arithmetic; return true;
                }
                if (node.Get("k1") != null || node.Get("k2") != null || node.Get("k3") != null || node.Get("k4") != null
                    || !TryCompositeOperator(composite, out stage.Composite)) return false;
                stage.Operation = SvgRasterFilterOperation.Composite; return true;
            default: reason = "This filter primitive has no represented native operation in the ordered owner."; return false;
        }
    }

    /// <summary>Reuses the definition paint cascade and precise literal color owner; opacity reaches RGBA16 without an RGBA8 boundary.</summary>
    private static bool TryStageFlood(SvgRasterElement node, SvgRasterStyle style, SvgRasterDefinitions definitions,
        bool linearRgb, out RasterEffectPixel16 color) {
        color = default;
        SvgRasterPaintProperties.Resolve(node, definitions.StyleSheet, definitions.AncestorsFor(node), style,
            "flood-color", "flood-opacity", out var text, out var opacityText);
        ChartColorMath.PreciseColor precise;
        if (text == null) precise = ChartColorMath.PreciseColor.FromRgb(0, 0, 0);
        else if (string.Equals(text.Trim(), "currentColor", StringComparison.OrdinalIgnoreCase)) precise = ChartColorMath.PreciseColor.FromColor(style.Color);
        else if (!SvgRasterColor.TryParsePrecise(text, out precise)) return false;
        var opacity = opacityText == null ? 1 : SvgRasterGradientValues.ParseCoordinate(opacityText, double.NaN);
        if (!Finite(opacity)) return false;
        var r = precise.R; var g = precise.G; var b = precise.B;
        if (linearRgb) { r = RasterColorTransfer.Linear(r); g = RasterColorTransfer.Linear(g); b = RasterColorTransfer.Linear(b); }
        color = RasterEffectPixels.Straight16(r, g, b, precise.A * Math.Max(0, Math.Min(1, opacity))); return true;
    }

    private static bool StagePair(string text, out double x, out double y) {
        x = y = 0; var tokens = Tokens(text);
        return tokens.Length >= 1 && tokens.Length <= 2 && Number(tokens[0], out x)
            && Number(tokens.Length == 1 ? tokens[0] : tokens[1], out y);
    }
}

using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    private SvgRasterFilterPlan(RasterEffectRegion filter, ChartColor flood, VisualBlendMode mode) {
        FilterRegion = CaptureRegion = filter;
        _effect = RasterEffectPlan.FloodComposite(new RasterEffectRegion(0, 0, filter.Width, filter.Height), flood, mode);
        IsZero = false; // A transparent SourceGraphic still exposes the flood backdrop.
    }
    private static bool TryFloodBlend(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason) {
        plan = null; reason = "Finite blending requires explicit user-space flood then SourceGraphic over that flood using a standard blend mode, in computed sRGB.";
        var flood = filter.Children[0]; var blend = filter.Children[1];
        if (!ResultName(flood.Get("result")) || flood.Get("in") != null || flood.Get("in2") != null
            || blend.Get("in") != "SourceGraphic" || blend.Get("in2") != flood.Get("result") || blend.Get("result") != null
            || blend.Get("no-composite") != null) return false;
        if (!TryBlendMode(blend.Get("mode") ?? "normal", out var selected)) return false;
        try {
            if (!TryFloodInputs(filter, matrix, definitions, out var region, out var color)) return false;
            plan = new SvgRasterFilterPlan(region, color, selected);
            reason = string.Empty; return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) {
            reason = "Finite blend capture exceeds the existing native region limits."; return false;
        }
    }
    private static bool TryBlendMode(string mode, out VisualBlendMode selected) {
        selected = default;
        switch (mode) {
            case "normal": selected = VisualBlendMode.Normal; break;
            case "multiply": selected = VisualBlendMode.Multiply; break;
            case "screen": selected = VisualBlendMode.Screen; break;
            case "overlay": selected = VisualBlendMode.Overlay; break;
            case "darken": selected = VisualBlendMode.Darken; break;
            case "lighten": selected = VisualBlendMode.Lighten; break;
            case "color-dodge": selected = VisualBlendMode.ColorDodge; break;
            case "color-burn": selected = VisualBlendMode.ColorBurn; break;
            case "hard-light": selected = VisualBlendMode.HardLight; break;
            case "soft-light": selected = VisualBlendMode.SoftLight; break;
            case "difference": selected = VisualBlendMode.Difference; break;
            case "exclusion": selected = VisualBlendMode.Exclusion; break;
            case "hue": selected = VisualBlendMode.Hue; break;
            case "saturation": selected = VisualBlendMode.Saturation; break;
            case "color": selected = VisualBlendMode.Color; break;
            case "luminosity": selected = VisualBlendMode.Luminosity; break;
            default: return false;
        }
        return true;
    }

}

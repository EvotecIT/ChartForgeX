using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    private SvgRasterFilterPlan(RasterEffectRegion filter, ChartColor flood, VisualCompositeOperator operation) {
        FilterRegion = CaptureRegion = filter;
        Effect = RasterEffectPlan.FloodComposite(new RasterEffectRegion(0, 0, filter.Width, filter.Height), flood, operation);
        IsZero = false;
    }

    private static bool TryFloodComposite(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason) {
        plan = null;
        reason = "Finite compositing requires explicit user-space flood then SourceGraphic over/in/out/atop/xor against that flood, in computed sRGB.";
        var flood = filter.Children[0];
        var composite = filter.Children[1];
        if (!ResultName(flood.Get("result")) || flood.Get("in") != null || flood.Get("in2") != null ||
            composite.Get("in") != "SourceGraphic" || composite.Get("in2") != flood.Get("result") ||
            composite.Get("result") != null || composite.Get("no-composite") != null ||
            composite.Get("k1") != null || composite.Get("k2") != null || composite.Get("k3") != null || composite.Get("k4") != null)
            return false;
        VisualCompositeOperator operation;
        switch (composite.Get("operator") ?? "over") {
            case "over": operation = VisualCompositeOperator.SourceOver; break;
            case "in": operation = VisualCompositeOperator.SourceIn; break;
            case "out": operation = VisualCompositeOperator.SourceOut; break;
            case "atop": operation = VisualCompositeOperator.SourceAtop; break;
            case "xor": operation = VisualCompositeOperator.Xor; break;
            default: return false;
        }
        try {
            if (!TryFloodInputs(filter, matrix, definitions, out var region, out var color)) return false;
            plan = new SvgRasterFilterPlan(region, color, operation);
            reason = string.Empty;
            return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) {
            reason = "Finite composite capture exceeds the existing native region limits.";
            return false;
        }
    }
}

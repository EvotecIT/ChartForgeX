using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static SvgRasterFilterPlan? ResolveNativeFilter(SvgRasterElement element, SvgRasterStyle style,
        SvgRasterMatrix matrix, SvgRasterDefinitions definitions, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        if (definitions.Filters == null || !style.Displayed || style.Opacity <= 0 || !SvgRasterFilterPlan.HasFilter(style.Filter)) return null;
        if (element.Name == "svg") {
            ReportNativeFilterUnsupported(element, definitions, "Filters on root or nested viewport elements are outside the opt-in subset."); return null;
        }
        if (!SvgRasterFilterPlan.TryCreate(style.Filter!, matrix, definitions, out var plan, out var reason)) {
            ReportNativeFilterUnsupported(element, definitions, reason); return null;
        }
        if (!plan!.IsProcedural && (plan.Stages == null || plan.RequiresStageSource) && !CanCaptureNativeFilterSource(element, style, definitions, ancestors, viewport, new HashSet<SvgRasterElement>(), 0, true, out reason)) {
            ReportNativeFilterUnsupported(element, definitions, reason); return null;
        }
        // Bounding-box masks/clips and arbitrary mask graphs retain their old path until their source-bound semantics qualify.
        if (definitions.TryGetClipPath(ParseReference(style.ClipPath), out var clip) && !clip.UserSpaceOnUse) {
            ReportNativeFilterUnsupported(element, definitions, "The filtered element's clip must use user-space units."); return null;
        }
        if (definitions.TryGetMask(ReferenceId(element, "mask"), out var mask)
            && (!mask.UserSpaceOnUse || !mask.ContentUserSpaceOnUse || mask.Element.Get("transform") != null
                || SvgRasterFilterPlan.HasFilter(mask.RootStyle.Filter) || mask.RootStyle.Opacity != 1
                || mask.RootStyle.ClipPath != null || ReferenceId(mask.Element, "mask") != null
                || !CanCaptureNativeFilterSource(mask.Element, mask.RootStyle, definitions, new List<SvgRasterElement>(mask.Ancestors), viewport,
                    new HashSet<SvgRasterElement>(), 0, true, out reason))) {
            ReportNativeFilterUnsupported(element, definitions, "The filtered element's mask must be a simple user-space source without nested effects."); return null;
        }
        return plan;
    }

    private static RgbaCanvas RenderFilteredElement(SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix,
        SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors,
        SvgRasterViewport viewport, SvgRasterFilterPlan plan) {
        if (plan.Stages != null) return RenderFilterStages(element, style, matrix, definitions, width, height, referenceDepth, ancestors, viewport, plan);
        var context = definitions.Filters!; context.Cancellation.ThrowIfCancellationRequested();
        var capture = plan.CaptureRegion;
        if (plan.IsProcedural) {
            // Reserve the actual coverage canvas and row visits BEFORE the one source-free permit.
            // The permit's additional debit therefore includes it exactly once with all prior document buffers.
            if (plan.ProceduralClipContour != null) {
                context.ReserveCanvas(capture.Width, capture.Height);
                context.ReserveWorkSamples(checked((long)capture.Width * capture.Height * 16));
            }
            var generated = context.Reserve(plan.Effect, 0, 0, width, height).ExecuteSourceFree();
            if (plan.ProceduralClipContour != null) ClipProceduralRegion(generated, plan, context);
            return CopyFilterOutput(width, height, plan, capture, generated, context);
        }
        // Reserve capture, kernel and one output exactly once. The ordinary compositor charges its actual post-operation arrays.
        var permit = context.Reserve(plan.Effect, capture.Width, capture.Height, width, height);
        var source = new RgbaCanvas(capture.Width, capture.Height, 1);
        var contentStyle = style.Inherit(); contentStyle.Opacity = 1;
        var shifted = SvgRasterMatrix.Translate(-(double)capture.X, -(double)capture.Y).Multiply(matrix);
        RenderElementCore(source, element, contentStyle, shifted, definitions, capture.Width, capture.Height, referenceDepth, ancestors, viewport);
        context.Cancellation.ThrowIfCancellationRequested();
        if (SourceOutsideFilterRegion(source, capture, plan.FilterRegion, context)) {
            // No guessed source-edge behavior or silent ignored effect: strict callers reject this diagnostic.
            ReportNativeFilterUnsupported(element, definitions, "Visible SourceGraphic outside the explicit filter region remains unqualified.");
            var ordinary = new RgbaCanvas(width, height, 1);
            RenderElementCore(ordinary, element, contentStyle, matrix, definitions, width, height, referenceDepth, ancestors, viewport);
            return ordinary;
        }
        RasterEffectResult effect;
        if (plan.IsZero) effect = new RasterEffectResult(source.BorrowDeviceSurfaceForEffects(), new RasterEffectRegion(0, 0, capture.Width, capture.Height));
        else effect = permit.Execute(source.BorrowDeviceSurfaceForEffects());
        return CopyFilterOutput(width, height, plan, capture, effect, context);
    }

    /// <summary>One existing device crop/copy for source-bearing and generated finite filters.</summary>
    private static RgbaCanvas CopyFilterOutput(int width, int height, SvgRasterFilterPlan plan,
        RasterEffectRegion capture, RasterEffectResult effect, SvgRasterFilterContext context) {
        var output = new RgbaCanvas(width, height, 1);
        int left = Math.Max(0, plan.FilterRegion.X), top = Math.Max(0, plan.FilterRegion.Y);
        int right = Math.Min(width, checked(plan.FilterRegion.X + plan.FilterRegion.Width));
        int bottom = Math.Min(height, checked(plan.FilterRegion.Y + plan.FilterRegion.Height));
        int originX = checked(capture.X + effect.Region.X), originY = checked(capture.Y + effect.Region.Y);
        // This is a crop/copy of the existing kernel output, not another painter or quantizer.
        for (int y = top; y < bottom; y++) {
            context.Cancellation.ThrowIfCancellationRequested();
            if (right <= left) continue;
            Buffer.BlockCopy(effect.Image.Pixels, checked(((y - originY) * effect.Image.Width + left - originX) * 4),
                output.Pixels, checked((y * width + left) * 4), checked((right - left) * 4));
        }
        context.Cancellation.ThrowIfCancellationRequested();
        // Preserve the single Gaussian's established final image conversion. Rounded-zero sources and
        // typed finite effects remain completed samples and keep their existing lossless transfer path.
        output.HasCapturedEffectPixels = !plan.UsesGaussianImageComposition;
        return output;
    }

    private static bool SourceOutsideFilterRegion(RgbaCanvas source, RasterEffectRegion capture, RasterEffectRegion filter, SvgRasterFilterContext context) {
        for (int y = 0; y < capture.Height; y++) {
            context.Cancellation.ThrowIfCancellationRequested();
            for (int x = 0; x < capture.Width; x++) {
                long deviceX = (long)capture.X + x, deviceY = (long)capture.Y + y;
                if (deviceX >= filter.X && deviceX < (long)filter.X + filter.Width && deviceY >= filter.Y && deviceY < (long)filter.Y + filter.Height) continue;
                if (source.Pixels[(y * capture.Width + x) * 4 + 3] != 0) return true;
            }
        }
        return false;
    }

    private static bool CanCaptureNativeFilterSource(SvgRasterElement element, SvgRasterStyle style, SvgRasterDefinitions definitions,
        List<SvgRasterElement> ancestors, SvgRasterViewport viewport, HashSet<SvgRasterElement> references, int depth, bool root, out string reason) {
        reason = "Only solid native shape/text SourceGraphic, including its existing isolated opacity, without nested filters, transforms, viewports, images or paint servers is supported.";
        if (!style.Displayed) return true;
        if (!root && (SvgRasterFilterPlan.HasFilter(style.Filter) || element.Get("transform") != null
            || style.ClipPath != null || ReferenceId(element, "mask") != null)) return false;
        if (style.Fill.IsReference || style.Stroke.IsReference || HasMarkerPaint(element)) return false;
        switch (element.Name) {
            case "mask": if (!root) return false; break;
            case "g": case "a": case "path": case "rect": case "circle": case "ellipse": case "line":
            case "polyline": case "polygon": case "text": case "tspan": break;
            case "use":
                if (depth >= 8 || !definitions.TryGetElement(HrefReferenceId(element), out var referenced) || IsDefinitionElement(referenced.Name)
                    || referenced.Get("transform") != null || !references.Add(referenced)) return false;
                var useStyle = ResolveUseStyle(style, referenced, definitions);
                var valid = CanCaptureNativeFilterSource(referenced, useStyle, definitions, new List<SvgRasterElement>(definitions.AncestorsFor(referenced)),
                    viewport, references, depth + 1, false, out reason);
                references.Remove(referenced); return valid;
            default: return false;
        }
        ancestors.Add(element);
        foreach (var child in element.Children) {
            if (IsDefinitionElement(child.Name)) continue;
            var childStyle = SvgRasterStyle.Resolve(style, child, definitions.StyleSheet, ancestors);
            if (!CanCaptureNativeFilterSource(child, childStyle, definitions, ancestors, viewport, references, depth, false, out reason)) {
                ancestors.RemoveAt(ancestors.Count - 1); return false;
            }
        }
        ancestors.RemoveAt(ancestors.Count - 1);
        return true;
    }

    private static void ReportNativeFilterUnsupported(SvgRasterElement element, SvgRasterDefinitions definitions, string reason) =>
        definitions.Diagnostics?.Report("SFR002", "The opt-in native filter was not applied. " + reason, element);
}

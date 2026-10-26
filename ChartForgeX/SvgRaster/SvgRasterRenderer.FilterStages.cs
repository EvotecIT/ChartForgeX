using System;
using System.Collections.Generic;
using ChartForgeX.Effects;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>Evaluates the already-bound finite source order in the incumbent renderer; only the final result is published.</summary>
    private static RgbaCanvas RenderFilterStages(SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix,
        SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors,
        SvgRasterViewport viewport, SvgRasterFilterPlan plan) {
        var context = definitions.Filters!; var stages = plan.Stages!; var needed = plan.NeededStages!;
        var reads = (int[])plan.StageReadCounts!.Clone(); var slots = new RasterEffectSurface?[stages.Length + 2];
        var capture = plan.CaptureRegion; var frame = new RasterEffectRegion(0, 0, capture.Width, capture.Height);
        context.BeginStages(plan.RequiresStageSource ? frame.Width : 0, plan.RequiresStageSource ? frame.Height : 0, width, height);
        if (plan.RequiresStageSource) {
            var source = new RgbaCanvas(frame.Width, frame.Height, 1);
            var contentStyle = style.Inherit(); contentStyle.Opacity = 1;
            var shifted = SvgRasterMatrix.Translate(-(double)capture.X, -(double)capture.Y).Multiply(matrix);
            RenderElementCore(source, element, contentStyle, shifted, definitions, frame.Width, frame.Height, referenceDepth, ancestors, viewport);
            context.Cancellation.ThrowIfCancellationRequested();
            // The filter frame clips standard input. This path does not request or reject pixels outside that frame.
            var image = source.BorrowDeviceSurfaceForEffects();
            slots[1] = context.ReserveStageCapture(RasterEffectPlan.SurfaceCapture(frame), image).ExecuteSurfaceCapture(image);
        }

        RasterEffectSurface Read(int input) {
            if (input == SvgRasterFilterPlan.SourceAlphaInput && !slots[0].HasValue) {
                var graphic = slots[1] ?? throw new InvalidOperationException("SourceAlpha requires the one captured SourceGraphic.");
                slots[0] = context.ReserveSurface(RasterEffectPlan.SurfaceAlpha(frame, graphic.LinearRgb), graphic).ExecuteSurface();
                Release(SvgRasterFilterPlan.SourceGraphicInput);
            }
            return slots[input + 2] ?? throw new InvalidOperationException("An earlier filter result was released before its final read.");
        }
        void Release(int input) {
            var slot = input + 2;
            if (--reads[slot] == 0) slots[slot] = null;
        }
        RasterEffectSurface InSpace(RasterEffectSurface surface, bool linearRgb) {
            context.Cancellation.ThrowIfCancellationRequested();
            if (surface.LinearRgb == linearRgb) return surface;
            if (surface.AlphaOnly) return surface.RetagAlpha(linearRgb);
            return context.ReserveSurface(RasterEffectPlan.SurfaceConvert(frame, linearRgb), surface).ExecuteSurface();
        }

        for (var index = 0; index < stages.Length; index++) {
            context.Cancellation.ThrowIfCancellationRequested();
            if (!needed[index]) continue;
            var stage = stages[index]; RasterEffectSurface result;
            if (stage.Operation == SvgRasterFilterOperation.Merge) {
                if (stage.Inputs.Length == 0) result = context.ReserveSurface(
                    RasterEffectPlan.SurfaceFlood(frame, default, stage.LinearRgb), null).ExecuteSurface();
                else {
                    result = InSpace(Read(stage.Inputs[0]), stage.LinearRgb);
                    for (var item = 1; item < stage.Inputs.Length; item++) {
                        context.Cancellation.ThrowIfCancellationRequested();
                        var top = InSpace(Read(stage.Inputs[item]), stage.LinearRgb);
                        result = context.ReserveSurface(RasterEffectPlan.SurfaceBlend(frame, top, VisualBlendMode.Normal, stage.LinearRgb), result).ExecuteSurface();
                    }
                    // A one-input merge may alias a frozen input; obtain one admitted writable output before applying its own clip.
                    if (stage.Inputs.Length == 1) result = context.ReserveSurface(
                        RasterEffectPlan.SurfaceOffset(frame, 0, 0, stage.LinearRgb), result).ExecuteSurface();
                }
            } else if (stage.Operation == SvgRasterFilterOperation.Flood) {
                result = context.ReserveSurface(RasterEffectPlan.SurfaceFlood(frame, stage.Flood, stage.LinearRgb), null).ExecuteSurface();
            } else {
                var first = InSpace(Read(stage.Inputs[0]), stage.LinearRgb);
                RasterEffectPlan operation; RasterEffectSurface source = first;
                switch (stage.Operation) {
                    case SvgRasterFilterOperation.Color: operation = RasterEffectPlan.SurfaceColor(frame, stage.Color!); break;
                    case SvgRasterFilterOperation.Blur: operation = RasterEffectPlan.SurfaceBlur(frame, stage.X, stage.Y, stage.LinearRgb); break;
                    case SvgRasterFilterOperation.Morphology:
                        operation = RasterEffectPlan.SurfaceMorphology(frame, stage.X, stage.Y, stage.Maximize, stage.LinearRgb); break;
                    case SvgRasterFilterOperation.Offset: operation = RasterEffectPlan.SurfaceOffset(frame, stage.X, stage.Y, stage.LinearRgb); break;
                    default:
                        source = InSpace(Read(stage.Inputs[1]), stage.LinearRgb); // in2 is the destination; in is the source.
                        if (stage.Operation == SvgRasterFilterOperation.Blend)
                            operation = RasterEffectPlan.SurfaceBlend(frame, first, stage.Blend, stage.LinearRgb);
                        else if (stage.Operation == SvgRasterFilterOperation.Composite)
                            operation = RasterEffectPlan.SurfaceComposite(frame, first, stage.Composite, stage.LinearRgb);
                        else operation = RasterEffectPlan.SurfaceArithmetic(frame, first, stage.K1, stage.K2, stage.K3, stage.K4, stage.LinearRgb);
                        break;
                }
                result = context.ReserveSurface(operation, source).ExecuteSurface();
            }
            ClipFilterStage(result, stage.Clip, context);
            slots[index + 2] = result;
            foreach (var input in stage.Inputs) Release(input);
        }
        var final = Read(stages.Length - 1);
        var output = PublishFilterSurface(final, plan.FilterRegion, width, height, context);
        Release(stages.Length - 1); context.Cancellation.ThrowIfCancellationRequested(); return output;
    }

    /// <summary>Clips a newly created unpublished result in place; no shared input is mutated.</summary>
    private static void ClipFilterStage(RasterEffectSurface surface, RasterEffectRegion? clip, SvgRasterFilterContext context) {
        var frame = surface.Region; context.ReserveWorkSamples(checked((long)frame.Width * frame.Height * 4));
        for (var y = 0; y < frame.Height; y++) {
            context.Cancellation.ThrowIfCancellationRequested();
            if (!clip.HasValue || y < clip.Value.Y || y >= (long)clip.Value.Y + clip.Value.Height) {
                Array.Clear(surface.Pixels, checked(y * frame.Width * 4), checked(frame.Width * 4)); continue;
            }
            var region = clip.Value;
            if (region.X > 0) Array.Clear(surface.Pixels, checked(y * frame.Width * 4), checked(region.X * 4));
            var right = checked(region.X + region.Width);
            if (right < frame.Width) Array.Clear(surface.Pixels, checked((y * frame.Width + right) * 4), checked((frame.Width - right) * 4));
        }
    }

    private static RgbaCanvas PublishFilterSurface(RasterEffectSurface surface, RasterEffectRegion filter, int width, int height, SvgRasterFilterContext context) {
        context.ReserveWorkSamples(checked((long)surface.Region.Width * surface.Region.Height * 64));
        var output = new RgbaCanvas(width, height, 1);
        var left = Math.Max(0, filter.X); var top = Math.Max(0, filter.Y);
        var right = Math.Min(width, checked(filter.X + filter.Width)); var bottom = Math.Min(height, checked(filter.Y + filter.Height));
        for (var y = top; y < bottom; y++) {
            context.Cancellation.ThrowIfCancellationRequested();
            for (var x = left; x < right; x++) {
                var at = checked(((y - filter.Y) * surface.Region.Width + x - filter.X) * 4);
                var pixel = RasterEffectPixels.Read16(surface.Pixels, at);
                if (surface.LinearRgb) pixel = RasterEffectPixels.Convert16(pixel, false);
                RasterEffectPixels.Write(output.Pixels, checked((y * width + x) * 4), pixel.R, pixel.G, pixel.B, pixel.A, x, y, false);
            }
        }
        context.Cancellation.ThrowIfCancellationRequested(); output.HasCapturedEffectPixels = true; return output;
    }
}

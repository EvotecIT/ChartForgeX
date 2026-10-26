using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneRasterRenderer {
    internal const int MaximumIsolatedDepth = 64;
    /// <summary>Additional private pixel buffers/visits reserved by the incumbent capture preflight.</summary>
    internal static (long PixelBytes, long WorkSamples) OpacityCaptureExtras(VisualScene scene, RasterAllocation allocation, VisualRenderOptions? effectOptions = null) {
        var accounting = CaptureEffectAccounting(scene, allocation, effectOptions);
        return (accounting.PixelBytes, accounting.WorkSamples);
    }

    internal static VisualNativeEffectAccounting CaptureEffectAccounting(VisualScene scene,
        RasterAllocation allocation, VisualRenderOptions? effectOptions = null) {
        var plan = PlanOpacity(scene, allocation, effectOptions);
        return plan == null ? default : new VisualNativeEffectAccounting(
            plan.PixelBytes, plan.WorkSamples, plan.PeakConcurrentLayers, plan.RootMask != null);
    }

    /// <summary>Counts existing borrowed native scene images once; fonts/geometry/encoder allocations retain the incumbent exclusion policy.</summary>
    internal static long BorrowedNativeImageBytes(VisualScene scene, byte[]? except = null) {
        var borrowed = new HashSet<byte[]>();
        CollectBorrowedNativeImages(scene, borrowed);
        long bytes = 0;
        foreach (var pixels in borrowed) if (!ReferenceEquals(pixels, except)) bytes = checked(bytes + pixels.LongLength);
        return bytes;
    }

    internal static void CollectBorrowedNativeImages(VisualScene scene, ISet<byte[]> borrowed) {
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneImage image) borrowed.Add(image.Image.Pixels);
            if (node is VisualSceneGroup { RetainedEffect: not null } group && group.RetainedEffect!.Effect.Mask is { } mask)
                borrowed.Add(mask.Image.Pixels);
        }
    }

    private static double NodeOpacity(VisualSceneNode node) => node is VisualSceneMark mark ? mark.Opacity
        : node is VisualSceneGroup group ? group.Opacity : node is VisualSceneGradient gradient ? gradient.Shape.Opacity : 1;

    private static OpacityPlan? PrepareOpacity(VisualScene scene, RasterAllocation allocation, VisualRenderOptions? effectOptions = null,
        int density = 1, int coverage = 2, long pixelBudget = 64000000, TextHinting? textHinting = null) {
        var plan = PlanOpacity(scene, allocation, effectOptions, density, coverage, pixelBudget, textHinting);
        if (plan == null || plan.RootMask != null ||
            plan.IsolatedLayers == 0 && plan.Shadows == null && plan.Masks == null && plan.Procedural == null) return plan;
        // One existing safety owner validates source/result, private isolation and borrowed-image arrays before any canvas allocation.
        // Strip only antialias coverage. Dividing by CombinedScale would also strip
        // output density and undercount the real returned RGBA array when Scale > 1.
        var outputCoverage = effectOptions?.Supersampling ?? coverage;
        var sourceBytes = checked((long)(allocation.PixelWidth / outputCoverage) *
            (allocation.PixelHeight / outputCoverage) * 4);
        RasterEffectPreflight.ValidateCapture(null, sourceBytes,
            checked(allocation.ByteCount + plan.PixelBytes + BorrowedNativeImageBytes(scene)),
            checked((long)allocation.PixelWidth * allocation.PixelHeight * 4 + sourceBytes * 2 + plan.WorkSamples), out _, out _);
        return plan;
    }

    private static OpacityPlan? PlanOpacity(VisualScene scene, RasterAllocation allocation, VisualRenderOptions? effectOptions = null,
        int density = 1, int coverage = 2, long pixelBudget = 64000000, TextHinting? textHinting = null) {
        var found = false;
        for (var index = 0; index < scene.Nodes.Count; index++) {
            var node = scene.Nodes[index];
            if (node is VisualSceneProceduralSource || NodeOpacity(node) != 1 || node is VisualSceneGroup group &&
                (group.SurfaceShadow != null || group.RetainedEffect != null)) {
                found = true;
                break;
            }
        }
        if (!found) return null; // No added plan, image, layer or accounting allocation on the default path.
        var ends = VisualSceneScopes.Ends(scene);
        var active = new Stack<bool>(); var depth = 0; var peak = 0; long count = 0;
        Dictionary<int, VisualProceduralSourcePlan>? procedural = null;
        Dictionary<int, VisualSurfaceShadowPlan>? shadows = null;
        Dictionary<int, VisualRetainedEffectPlan>? masks = null;
        var sourceOptions = effectOptions;
        long effectBytes = 0, effectWork = 0;
        for (var index = 0; index < scene.Nodes.Count; index++) {
            var node = scene.Nodes[index]; var opacity = NodeOpacity(node);
            if (node is VisualSceneProceduralSource generated) {
                var densityForCells = checked((effectOptions?.Scale ?? density) * (effectOptions?.Supersampling ?? coverage));
                var sourcePlan = new VisualProceduralSourcePlan(generated, densityForCells);
                procedural ??= new Dictionary<int, VisualProceduralSourcePlan>();
                procedural.Add(index, sourcePlan);
                effectBytes = checked(effectBytes + sourcePlan.Plan.AllocatedBytes);
                effectWork = checked(effectWork + sourcePlan.Plan.WorkSamples);
                RasterEffectPreflight.ValidateCapture(null, 0, effectBytes, effectWork, out _, out _);
            }
            if (node is VisualSceneGroup) {
                if (opacity == 0) { index = ends[index]; continue; }
                if (node is VisualSceneGroup { RetainedEffect: not null } masked) {
                    sourceOptions ??= new VisualRenderOptions(density, coverage, pixelBudget, textHinting);
                    var mask = new VisualRetainedEffectPlan(scene, index + 1, ends[index], masked.RetainedEffect!, sourceOptions);
                    masks ??= new Dictionary<int, VisualRetainedEffectPlan>();
                    masks.Add(index, mask);
                    effectBytes = checked(effectBytes + mask.PixelBytes);
                    effectWork = checked(effectWork + mask.WorkSamples);
                    peak = Math.Max(peak, checked(depth + mask.PeakDepth));
                    if (peak > MaximumIsolatedDepth) throw new NotSupportedException("Native opacity/effects support at most64 simultaneously isolated captures.");
                    RasterEffectPreflight.ValidateCapture(null, 0, effectBytes, effectWork, out _, out _);
                    // The local completed-object capture owns its descendants' allocations.
                    // Final object opacity is applied once while drawing that result.
                    index = ends[index];
                    continue;
                }
                if (node is VisualSceneGroup { SurfaceShadow: not null } surface) {
                    sourceOptions ??= new VisualRenderOptions(density, coverage, pixelBudget, textHinting);
                    var shadow = new VisualSurfaceShadowPlan(scene, index + 1, ends[index], surface.SurfaceShadow!, sourceOptions);
                    shadows ??= new Dictionary<int, VisualSurfaceShadowPlan>();
                    shadows.Add(index, shadow);
                    effectBytes = checked(effectBytes + shadow.PixelBytes);
                    effectWork = checked(effectWork + shadow.WorkSamples);
                    peak = Math.Max(peak, checked(depth + shadow.PeakDepth));
                    RasterEffectPreflight.ValidateCapture(null, 0, effectBytes, effectWork, out _, out _);
                }
                var isolated = opacity != 1; active.Push(isolated);
                if (isolated) { depth++; count++; peak = Math.Max(peak, depth); }
            } else if (node is VisualSceneEndGroup) { if (active.Pop()) depth--; }
            else if (opacity > 0 && opacity < 1) { count++; peak = Math.Max(peak, depth + 1); }
            if (peak > MaximumIsolatedDepth) throw new NotSupportedException("Native opacity supports at most64 simultaneously isolated groups/elements.");
        }
        // Match the existing conservative capture boundary: reserve every declared private allocation,
        // including sequential layers, without assuming collection or pooling. Peak nesting is checked separately.
        var bytes = checked(count * allocation.ByteCount + effectBytes);
        var work = checked(count * allocation.PixelWidth * allocation.PixelHeight * 20 + effectWork);
        VisualRetainedEffectPlan? rootMask = null;
        if (masks != null && masks.TryGetValue(0, out var candidate) && ends[0] == scene.Nodes.Count - 1 &&
            scene.Nodes[0] is VisualSceneGroup root && root.Opacity == 1 && root.Clip == null &&
            root.PathClip == null && root.Rotation == null && root.Translation == null &&
            candidate.Scope.SourceSize.Width == scene.Size.Width && candidate.Scope.SourceSize.Height == scene.Size.Height) {
            rootMask = candidate;
        }
        return new OpacityPlan(ends, count, peak, bytes, work, shadows, masks, rootMask, procedural);
    }

    private static RgbaImage RenderOpacity(VisualScene scene, RasterAllocation allocation, OpacityPlan plan,
        int scale, int supersampling, TextHinting? textHinting, CancellationToken cancellation = default) {
        var canvas = NewCanvas();
        PaintRange(canvas, 0, scene.Nodes.Count, VisualSceneTransform.Identity);
        return canvas.ToImage();

        RgbaCanvas NewCanvas() => new(scene.Size.Width, scene.Size.Height, supersampling, null, scale, useDefaultOutlineFont: false);
        void Composite(RgbaCanvas parent, RgbaCanvas layer, double opacity) {
            var unit = 1d / parent.PixelsPerUnit;
            parent.DrawCapturedImageTransformed(allocation.PixelWidth, allocation.PixelHeight, layer.Pixels, unit, 0, 0, unit, 0, 0, opacity);
        }
        void PaintShadow(RgbaCanvas target, int index, VisualSceneTransform transform) {
            if (plan.Shadows == null || !plan.Shadows.TryGetValue(index, out var shadow) || !shadow.Visible) return;
            cancellation.ThrowIfCancellationRequested();
            var image = shadow.Capture(cancellation);
            var origin = transform.Apply(new ChartPoint(shadow.Bounds.X, shadow.Bounds.Y));
            var unit = 1d / shadow.Options.Scale;
            target.DrawCapturedImageTransformed(image.Width, image.Height, image.Pixels,
                transform.A * unit, transform.B * unit, transform.C * unit, transform.D * unit, origin.X, origin.Y);
        }
        void PaintMask(RgbaCanvas target, VisualRetainedEffectPlan masked, VisualSceneTransform transform, double opacity) {
            cancellation.ThrowIfCancellationRequested();
            var image = masked.Capture(cancellation);
            var origin = transform.Apply(new ChartPoint(0, 0));
            var unit = 1d / masked.Options.Scale;
            target.DrawCapturedImageTransformed(image.Width, image.Height, image.Pixels,
                transform.A * unit, transform.B * unit, transform.C * unit, transform.D * unit,
                origin.X, origin.Y, opacity);
        }
        void PaintRange(RgbaCanvas target, int first, int last, VisualSceneTransform inherited) {
            var clips = new Stack<IDisposable?>(); var transforms = new Stack<VisualSceneTransform>(); var transform = inherited;
            try {
                for (var index = first; index < last; index++) {
                    cancellation.ThrowIfCancellationRequested();
                    var node = scene.Nodes[index]; var opacity = NodeOpacity(node);
                    if (node is VisualSceneGroup group) {
                        if (opacity == 0) { index = plan.Ends[index]; continue; }
                        var next = transform;
                        if (group.Rotation.HasValue) next = next.Rotate(group.Rotation.Value);
                        if (group.Translation.HasValue) next = next.Translate(group.Translation.Value);
                        if (plan.Masks != null && plan.Masks.TryGetValue(index, out var masked)) {
                            using (GroupClip(target, group, next)) PaintMask(target, masked, next, opacity);
                            index = plan.Ends[index];
                            continue;
                        }
                        if (opacity != 1) {
                            var layer = NewCanvas();
                            // Ancestor clips apply once at final composite. This group's own clip applies once inside its isolated source.
                            using (GroupClip(layer, group, next)) {
                                PaintShadow(layer, index, next);
                                PaintRange(layer, index + 1, plan.Ends[index], next);
                            }
                            Composite(target, layer, opacity); index = plan.Ends[index];
                        } else {
                            transforms.Push(transform); transform = next;
                            clips.Push(GroupClip(target, group, transform));
                            PaintShadow(target, index, transform);
                        }
                    } else if (node is VisualSceneEndGroup) { clips.Pop()?.Dispose(); transform = transforms.Pop(); }
                    else if (node is VisualSceneProceduralSource && plan.Procedural != null && plan.Procedural.TryGetValue(index, out var generated)) {
                        PaintProcedural(target, generated, transform, cancellation);
                    }
                    else if (opacity > 0) {
                        if (opacity != 1) {
                            var layer = NewCanvas(); PaintNativeNode(scene, layer, node, index, transform, textHinting);
                            Composite(target, layer, opacity);
                        } else PaintNativeNode(scene, target, node, index, transform, textHinting);
                    }
                }
            } finally { while (clips.Count > 0) clips.Pop()?.Dispose(); }
        }
    }

    private sealed class OpacityPlan {
        internal OpacityPlan(int[] ends, long layers, int peak, long bytes, long work, Dictionary<int, VisualSurfaceShadowPlan>? shadows, Dictionary<int, VisualRetainedEffectPlan>? masks, VisualRetainedEffectPlan? rootMask, Dictionary<int, VisualProceduralSourcePlan>? procedural) {
            Procedural = procedural; RootMask = rootMask; Masks = masks; Shadows = shadows; Ends = ends; IsolatedLayers = layers; PeakConcurrentLayers = peak; PixelBytes = bytes; WorkSamples = work; }
        internal Dictionary<int, VisualProceduralSourcePlan>? Procedural { get; }
        internal Dictionary<int, VisualSurfaceShadowPlan>? Shadows { get; }
        internal Dictionary<int, VisualRetainedEffectPlan>? Masks { get; }
        internal VisualRetainedEffectPlan? RootMask { get; }
        internal int[] Ends { get; }
        internal long IsolatedLayers { get; }
        internal int PeakConcurrentLayers { get; }
        internal long PixelBytes { get; }
        internal long WorkSamples { get; }
    }
}

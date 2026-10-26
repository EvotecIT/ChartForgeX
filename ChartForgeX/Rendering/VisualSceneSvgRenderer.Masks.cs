using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private sealed class EffectExportPlan {
        internal readonly Dictionary<int, VisualProceduralSourcePlan> Procedural = new();
        internal readonly Dictionary<int, VisualSurfaceShadowPlan> Shadows = new();
        internal readonly Dictionary<int, VisualRetainedEffectPlan> BakedMasks = new();
        internal readonly Dictionary<int, VisualSceneRetainedEffect> NativeMasks = new();
        internal readonly Dictionary<int, VisualRetainedNativeEffectPlan> NativeAppearance = new();
        internal readonly Dictionary<VisualMask, VisualMaskSvgCoverage> Coverage = new();
        internal readonly HashSet<byte[]> Borrowed = new();
        internal long PixelBytes, WorkSamples;

        internal void Validate() {
            long borrowed = 0;
            foreach (var pixels in Borrowed) borrowed = checked(borrowed + pixels.LongLength);
            RasterEffectPreflight.ValidateCapture(null, 0, checked(PixelBytes + borrowed), WorkSamples, out _, out _);
        }
    }

    private static EffectExportPlan? EffectPlans(VisualScene scene, VisualSvgOptions? options,
        CancellationToken cancellation) {
        if (!scene.HasSurfaceEffects && !scene.HasRetainedEffects && !scene.HasProceduralSources) return null;
        var native = options?.EffectProfile == VisualSvgEffectProfile.NativeFilters;
        var raster = native ? new VisualRenderOptions(1, 1) : options?.EffectRasterOptions ?? new VisualRenderOptions();
        var ends = VisualSceneScopes.Ends(scene);
        var result = new EffectExportPlan();
        for (var index = 0; index < scene.Nodes.Count; index++) {
            cancellation.ThrowIfCancellationRequested();
            if (scene.Nodes[index] is VisualSceneProceduralSource generated) {
                var sourcePlan = new VisualProceduralSourcePlan(generated, native ? 1 : checked(raster.Scale * raster.Supersampling));
                result.Procedural.Add(index, sourcePlan);
                if (!native) {
                    result.PixelBytes = checked(result.PixelBytes + sourcePlan.Plan.AllocatedBytes);
                    result.WorkSamples = checked(result.WorkSamples + sourcePlan.Plan.WorkSamples);
                    result.Validate();
                }
            }
            if (!(scene.Nodes[index] is VisualSceneGroup group)) continue;
            if (group.Opacity == 0 || group.ImageResource != null) {
                index = ends[index];
                continue;
            }
            if (group.RetainedEffect is { } mask) {
                if (native && mask.Effect.Mask is { } inputMask) {
                    result.NativeMasks.Add(index, mask);
                    if (!result.Coverage.ContainsKey(inputMask)) {
                        var coverage = new VisualMaskSvgCoverage(inputMask);
                        result.Coverage.Add(inputMask, coverage);
                        result.PixelBytes = checked(result.PixelBytes + coverage.PixelBytes);
                        result.WorkSamples = checked(result.WorkSamples + coverage.WorkSamples);
                        result.Borrowed.Add(inputMask.Image.Pixels);
                    }
                    result.Validate();
                } else if (native) {
                    result.NativeAppearance.Add(index, new VisualRetainedNativeEffectPlan(mask));
                } else {
                    var capture = new VisualRetainedEffectPlan(scene, index + 1, ends[index], mask, raster);
                    result.BakedMasks.Add(index, capture);
                    result.PixelBytes = checked(result.PixelBytes + capture.PixelBytes);
                    result.WorkSamples = checked(result.WorkSamples + capture.WorkSamples);
                    VisualSceneRasterRenderer.CollectBorrowedNativeImages(capture.Source, result.Borrowed);
                    if (mask.Effect.Mask is { } operand) result.Borrowed.Add(operand.Image.Pixels);
                    result.Validate();
                    index = ends[index];
                }
                continue;
            }
            if (group.SurfaceShadow == null) continue;
            var shadow = new VisualSurfaceShadowPlan(scene, index + 1, ends[index], group.SurfaceShadow, raster, !native);
            result.Shadows.Add(index, shadow);
            if (!native) {
                result.PixelBytes = checked(result.PixelBytes + shadow.PixelBytes);
                result.WorkSamples = checked(result.WorkSamples + shadow.WorkSamples);
                result.Validate();
            }
        }
        return result;
    }

    private static string RetainedEffectId(string prefix, int index) =>
        prefix + "-retained-mask-" + index.ToString(CultureInfo.InvariantCulture);

    private static void WriteRetainedEffects(SvgMarkupWriter writer, EffectExportPlan? effects,
        string prefix, CancellationToken cancellation) {
        if (effects == null) return;
        WriteNativeAppearanceFilters(writer, effects.NativeAppearance, prefix, cancellation);
        if (effects.NativeMasks.Count == 0) return;
        writer.StartElement("defs").EndStartElement();
        foreach (var entry in effects.NativeMasks) {
            cancellation.ThrowIfCancellationRequested();
            var scope = entry.Value;
            var input = effects.Coverage[scope.Effect.Mask!].Image(cancellation);
            var encoded = PngWriter.WriteRgba(input);
            cancellation.ThrowIfCancellationRequested();
            var bounds = scope.Effect.Mask!.Bounds;
            writer.StartElement("mask").Attribute("id", RetainedEffectId(prefix, entry.Key))
                .Attribute("maskUnits", "userSpaceOnUse").Attribute("maskContentUnits", "userSpaceOnUse")
                .Attribute("x", 0).Attribute("y", 0)
                .Attribute("width", scope.SourceSize.Width).Attribute("height", scope.SourceSize.Height)
                .Attribute("mask-type", "alpha").Attribute("style", "mask-type:alpha")
                .Attribute("color-interpolation", "sRGB").EndStartElement();
            writer.StartElement("image").Attribute("x", bounds.X).Attribute("y", bounds.Y)
                .Attribute("width", bounds.Width).Attribute("height", bounds.Height)
                .Attribute("preserveAspectRatio", "none")
                .Attribute("href", "data:image/png;base64," + Convert.ToBase64String(encoded)).EndEmptyElement();
            writer.EndElement();
        }
        writer.EndElement();
    }

    private static void WriteBakedRetainedEffect(SvgMarkupWriter writer, VisualRetainedEffectPlan plan,
        CancellationToken cancellation) {
        var image = plan.Capture(cancellation);
        cancellation.ThrowIfCancellationRequested();
        var encoded = PngWriter.WriteRgba(image);
        cancellation.ThrowIfCancellationRequested();
        writer.StartElement("image").Attribute("x", 0).Attribute("y", 0)
            .Attribute("width", plan.Scope.SourceSize.Width).Attribute("height", plan.Scope.SourceSize.Height)
            .Attribute("preserveAspectRatio", "none")
            .Attribute("href", "data:image/png;base64," + Convert.ToBase64String(encoded))
            .Attribute("data-cfx-role", plan.Scope.Effect.Mask != null ? "retained-masked-presentation"
                : "retained-" + AppearanceName(plan.Scope.Effect) + "-presentation").EndEmptyElement();
    }

    private static void MaskIdentity(BinaryWriter writer, VisualSceneRetainedEffect? scope) {
        if (scope == null) return;
        var mask = scope.Effect.Mask;
        if (mask == null) { RetainedAppearanceIdentity(writer, scope); return; }
        writer.Write("retained-mask");
        writer.Write(scope.SourceSize.Width); writer.Write(scope.SourceSize.Height);
        writer.Write(mask.Bounds.X); writer.Write(mask.Bounds.Y);
        writer.Write(mask.PixelsPerUnit); writer.Write((int)mask.Mode); writer.Write(mask.OrderedDither);
        writer.Write(mask.Image.Width); writer.Write(mask.Image.Height); writer.Write(mask.Image.Pixels);
    }
}

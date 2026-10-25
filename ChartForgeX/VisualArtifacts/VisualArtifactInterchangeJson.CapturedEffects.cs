using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void CapturedEffects(VisualArtifactInterchangeJsonWriter writer,
        IReadOnlyList<VisualArtifactInterchangeCapturedEffect> effects) {
        if (effects.Count == 0) return;
        writer.Property("capturedEffects");
        writer.StartArray();
        foreach (var effect in effects) {
            writer.StartObject();
            if (effect.Blur is { } blur) {
                writer.Property("blur"); writer.StartObject();
                Number(writer, "sigmaX", blur.SigmaX); Number(writer, "sigmaY", blur.SigmaY);
                Boolean(writer, "orderedDither", blur.OrderedDither); writer.EndObject();
            }
            DropShadow(writer, "dropShadow", effect.DropShadow);
            InnerShadow(writer, effect.InnerShadow);
            if (effect.Outline is { } outline) {
                writer.Property("outline"); writer.StartObject();
                String(writer, "color", outline.Color.ToCss()); Number(writer, "radius", outline.Radius);
                Boolean(writer, "orderedDither", outline.OrderedDither); writer.EndObject();
            }
            if (effect.Bloom is { } bloom) {
                writer.Property("bloom"); writer.StartObject();
                Number(writer, "sigmaX", bloom.SigmaX); Number(writer, "sigmaY", bloom.SigmaY);
                Number(writer, "threshold", bloom.Threshold); Number(writer, "strength", bloom.Strength);
                Boolean(writer, "orderedDither", bloom.OrderedDither); writer.EndObject();
            }
            if (effect.Mask is { } mask) { writer.Property("mask"); CapturedMask(writer, mask); }
            if (effect.CaptureFrame is { } frame) CaptureFrame(writer, frame);
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void CaptureFrame(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeCaptureFrame frame) {
        writer.Property("captureFrame"); writer.StartObject();
        Number(writer, "x", frame.Bounds.X); Number(writer, "y", frame.Bounds.Y);
        Number(writer, "width", frame.Bounds.Width); Number(writer, "height", frame.Bounds.Height);
        var options = frame.RasterOptions;
        Number(writer, "pixelsPerUnit", options.Scale); Number(writer, "supersampling", options.Supersampling);
        Number(writer, "pixelBudget", (double)options.PixelBudget);
        if (options.TextHinting.HasValue) String(writer, "textHinting", options.TextHinting.Value.ToString());
        writer.EndObject();
    }

    private static void ReadCapturedEffects(Dictionary<string, GeoJsonValue> root,
        List<VisualArtifactInterchangeCapturedEffect> effects) {
        // The unpublished mask-only schema is intentionally replaced, never silently treated as empty.
        if (root.ContainsKey("capturedMasks"))
            throw new NotSupportedException("The unpublished capturedMasks field is replaced by ordered capturedEffects mask steps.");
        long decodedBytes = 0;
        int? maskDensity = null;
        foreach (var entry in OptionalArray(root, "capturedEffects")) {
            if (effects.Count >= VisualSceneRasterRenderer.MaximumIsolatedDepth)
                throw new NotSupportedException("Captured effects exceed the existing native capture depth.");
            var item = entry.AsObject("captured effect");
            RequireEffectMembers(item, new[] { "blur", "dropShadow", "outline", "bloom", "mask", "innerShadow", "captureFrame" }, "captured effect");
            var effect = new VisualArtifactInterchangeCapturedEffect {
                DropShadow = ReadDropShadow(item, "dropShadow"), InnerShadow = ReadInnerShadow(item)
            };
            if (OptionalObject(item, "blur") is { } blur) {
                RequireEffectMembers(blur, new[] { "sigmaX", "sigmaY", "orderedDither" }, "captured blur");
                effect.Blur = new VisualLayerBlur(RequiredNumber(blur, "sigmaX"), RequiredNumber(blur, "sigmaY"), RequiredEffectDither(blur));
            }
            if (OptionalObject(item, "outline") is { } outline) {
                RequireEffectMembers(outline, new[] { "color", "radius", "orderedDither" }, "captured outline");
                if (!ChartColor.TryParse(RequiredString(outline, "color"), out var color))
                    throw new ArgumentException("An outline color must be a supported native literal.");
                effect.Outline = new VisualOutline(color, RequiredNumber(outline, "radius"), RequiredEffectDither(outline));
            }
            if (OptionalObject(item, "bloom") is { } bloom) {
                RequireEffectMembers(bloom, new[] { "sigmaX", "sigmaY", "threshold", "strength", "orderedDither" }, "captured bloom");
                effect.Bloom = new VisualBloom(RequiredNumber(bloom, "sigmaX"), RequiredNumber(bloom, "threshold"),
                    RequiredNumber(bloom, "strength"), RequiredNumber(bloom, "sigmaY"), RequiredEffectDither(bloom));
            }
            if (OptionalObject(item, "mask") is { } mask)
                effect.Mask = ReadCapturedMask(mask, ref decodedBytes, ref maskDensity);
            if (OptionalObject(item, "captureFrame") is { } frame) {
                RequireEffectMembers(frame, new[] { "x", "y", "width", "height", "pixelsPerUnit", "supersampling", "pixelBudget", "textHinting" }, "capture frame");
                effect.CaptureFrame = new VisualArtifactInterchangeCaptureFrame {
                    Bounds = new ChartRect(RequiredNumber(frame, "x"), RequiredNumber(frame, "y"),
                        RequiredNumber(frame, "width"), RequiredNumber(frame, "height")),
                    RasterOptions = new VisualRenderOptions(RequiredInt(frame, "pixelsPerUnit"), RequiredInt(frame, "supersampling"),
                        RequiredInt(frame, "pixelBudget"), OptionalEnum<TextHinting>(frame, "textHinting"))
                };
            }
            effects.Add(effect);
        }
    }

    private static bool RequiredEffectDither(Dictionary<string, GeoJsonValue> item) =>
        OptionalBool(item, "orderedDither") ?? throw new ArgumentException("Missing captured effect orderedDither boolean.");
}

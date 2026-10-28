using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static string AppearanceName(VisualCapturedEffect effect) => effect.InnerShadow != null ? "inner-shadow"
        : effect.Outline != null ? "outline"
        : effect.Bloom != null ? "bloom" : "shadow";
    private static string RetainedAppearanceId(string prefix, int index, VisualCapturedEffect effect) =>
        prefix + "-retained-" + AppearanceName(effect) + "-" + index.ToString(CultureInfo.InvariantCulture);

    private static void WriteNativeAppearanceFilters(SvgMarkupWriter writer,
        Dictionary<int, VisualRetainedNativeEffectPlan> plans, string prefix, CancellationToken cancellation) {
        if (plans.Count == 0) return;
        writer.StartElement("defs").EndStartElement();
        foreach (var entry in plans) {
            cancellation.ThrowIfCancellationRequested();
            var native = entry.Value;
            if (native.Effect == null) continue;
            var scope = native.Scope;
            writer.StartElement("filter").Attribute("id", RetainedAppearanceId(prefix, entry.Key, scope.Effect))
                .Attribute("filterUnits", "userSpaceOnUse").Attribute("primitiveUnits", "userSpaceOnUse")
                .Attribute("x", 0).Attribute("y", 0).Attribute("width", scope.SourceSize.Width).Attribute("height", scope.SourceSize.Height)
                .Attribute("color-interpolation-filters", "sRGB").EndStartElement();
            if (scope.Effect.InnerShadow != null) WriteInnerShadowFilter(writer, native);
            else if (scope.Effect.Outline != null) WriteOutlineFilter(writer, native);
            else if (scope.Effect.Bloom != null) WriteBloomFilter(writer, native);
            else WriteRetainedShadowFilter(writer, native);
            writer.EndElement();
        }
        writer.EndElement();
    }

    private static void WriteOutlineFilter(SvgMarkupWriter writer, VisualRetainedNativeEffectPlan native) {
        var effect = native.Scope.Effect.Outline!;
        writer.StartElement("feMorphology").Attribute("in", "SourceAlpha").Attribute("operator", "dilate")
            .Attribute("radius", native.Effect!.SpreadRadius).Attribute("result", "outlineDilated").EndEmptyElement();
        writer.StartElement("feComposite").Attribute("in", "outlineDilated").Attribute("in2", "SourceAlpha")
            .Attribute("operator", "arithmetic").Attribute("k1", 0).Attribute("k2", 1).Attribute("k3", -1).Attribute("k4", 0)
            .Attribute("result", "outlineBand").EndEmptyElement();
        writer.StartElement("feFlood").Attribute("flood-color", effect.Color.WithAlpha(255).ToCss())
            .Attribute("flood-opacity", EffectNumber(effect.Color.A / 255d)).Attribute("result", "outlineTint").EndEmptyElement();
        writer.StartElement("feComposite").Attribute("in", "outlineTint").Attribute("in2", "outlineBand")
            .Attribute("operator", "in").Attribute("result", "outlineColor").EndEmptyElement();
        writer.StartElement("feComposite").Attribute("in", "SourceGraphic").Attribute("in2", "outlineColor")
            .Attribute("operator", "arithmetic").Attribute("k1", 0).Attribute("k2", 1).Attribute("k3", 1).Attribute("k4", 0).EndEmptyElement();
    }

    private static void WriteBloomFilter(SvgMarkupWriter writer, VisualRetainedNativeEffectPlan native) {
        var effect = native.Scope.Effect.Bloom!;
        var threshold = native.Effect!.BloomThreshold16 / 65535d;
        var divisor = 1 - threshold;
        writer.StartElement("feColorMatrix").Attribute("in", "SourceGraphic").Attribute("type", "matrix")
            .Attribute("values", "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 " + EffectNumber(.2126 / divisor) + " " +
                EffectNumber(.7152 / divisor) + " " + EffectNumber(.0722 / divisor) + " 0 " + EffectNumber(-threshold / divisor))
            .Attribute("result", "bloomRamp").EndEmptyElement();
        writer.StartElement("feComposite").Attribute("in", "SourceGraphic").Attribute("in2", "bloomRamp")
            .Attribute("operator", "in").Attribute("result", "bloomBright").EndEmptyElement();
        writer.StartElement("feGaussianBlur").Attribute("in", "bloomBright").Attribute("edgeMode", "none")
            .Attribute("stdDeviation", EffectNumber(effect.HorizontalQ8(1) / 256d) + " " + EffectNumber(effect.VerticalQ8(1) / 256d))
            .Attribute("result", "bloomSoft").EndEmptyElement();
        writer.StartElement("feComponentTransfer").Attribute("in", "bloomSoft").Attribute("result", "bloomStrength").EndStartElement();
        writer.StartElement("feFuncA").Attribute("type", "linear").Attribute("slope", EffectNumber(native.Effect.BloomStrength16 / 65535d))
            .Attribute("intercept", 0).EndEmptyElement();
        writer.EndElement();
        writer.StartElement("feBlend").Attribute("in", "bloomStrength").Attribute("in2", "SourceGraphic").Attribute("mode", "screen").EndEmptyElement();
    }

    private static void WriteRetainedShadowFilter(SvgMarkupWriter writer, VisualRetainedNativeEffectPlan native) {
        var effect = native.Scope.Effect.DropShadow!;
        var plan = native.Effect!;
        writer.StartElement("feMorphology").Attribute("in", "SourceAlpha").Attribute("operator", effect.Spread < 0 ? "erode" : "dilate")
            .Attribute("radius", Math.Abs((long)effect.Spread)).Attribute("result", "spread").EndEmptyElement();
        writer.StartElement("feGaussianBlur").Attribute("in", "spread").Attribute("edgeMode", "none")
            .Attribute("stdDeviation", EffectNumber(effect.HorizontalQ8(1) / 256d) + " " + EffectNumber(effect.VerticalQ8(1) / 256d))
            .Attribute("result", "blur").EndEmptyElement();
        writer.StartElement("feOffset").Attribute("in", "blur").Attribute("dx", effect.OffsetX).Attribute("dy", effect.OffsetY)
            .Attribute("result", "offset").EndEmptyElement();
        writer.StartElement("feFlood").Attribute("flood-color", effect.Color.WithAlpha(255).ToCss())
            .Attribute("flood-opacity", EffectNumber(effect.Color.A / 255d * plan.ShadowOpacity))
            .Attribute("result", "tint").EndEmptyElement();
        writer.StartElement("feComposite").Attribute("in", "tint").Attribute("in2", "offset").Attribute("operator", "in")
            .Attribute("result", "shadow");
        if (plan.ShadowVisibleRegion is { } clip)
            writer.Attribute("x", clip.X).Attribute("y", clip.Y).Attribute("width", clip.Width).Attribute("height", clip.Height);
        writer.EndEmptyElement();
        writer.StartElement("feMerge").EndStartElement();
        writer.StartElement("feMergeNode").Attribute("in", "shadow").EndEmptyElement();
        writer.StartElement("feMergeNode").Attribute("in", "SourceGraphic").EndEmptyElement();
        writer.EndElement();
    }

    private static void RetainedAppearanceIdentity(BinaryWriter writer, VisualSceneRetainedEffect scope) {
        writer.Write("retained-" + AppearanceName(scope.Effect));
        writer.Write(scope.SourceSize.Width); writer.Write(scope.SourceSize.Height);
        if (scope.Effect.InnerShadow is { } inner) {
            writer.Write(inner.Color.R); writer.Write(inner.Color.G); writer.Write(inner.Color.B); writer.Write(inner.Color.A);
            writer.Write(inner.OffsetX); writer.Write(inner.OffsetY); writer.Write(inner.SigmaX); writer.Write(inner.SigmaY);
            writer.Write(inner.Spread); writer.Write(inner.Opacity); writer.Write(inner.OrderedDither);
        } else if (scope.Effect.Outline is { } outline) {
            writer.Write(outline.Color.R); writer.Write(outline.Color.G); writer.Write(outline.Color.B); writer.Write(outline.Color.A);
            writer.Write(outline.Radius); writer.Write(outline.OrderedDither);
        } else if (scope.Effect.Bloom is { } bloom) {
            writer.Write(bloom.SigmaX); writer.Write(bloom.SigmaY); writer.Write(bloom.Threshold); writer.Write(bloom.Strength);
            writer.Write(bloom.OrderedDither);
        } else {
            var size = scope.SourceSize;
            var bounds = new ChartForgeX.Primitives.ChartRect(0, 0, size.Width, size.Height);
            SurfaceIdentity(writer, new VisualSceneSurfaceShadow(scope.Effect.DropShadow!, bounds, bounds));
        }
    }

    private static string EffectNumber(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}

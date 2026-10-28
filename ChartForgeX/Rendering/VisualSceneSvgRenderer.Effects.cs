using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static string SurfaceFilterId(string prefix, int index) =>
        prefix + "-surface-shadow-" + index.ToString(CultureInfo.InvariantCulture);

    private static void WriteSurfaceFilters(SvgMarkupWriter writer,
        Dictionary<int, VisualSurfaceShadowPlan>? plans, string prefix, VisualSvgOptions? options) {
        if (plans == null || options?.EffectProfile != VisualSvgEffectProfile.NativeFilters || plans.Count == 0) return;
        writer.StartElement("defs").EndStartElement();
        foreach (var entry in plans) {
            var plan = entry.Value;
            var effect = plan.Scope.Effect;
            var source = plan.Scope.InkBounds;
            var shadow = plan.Bounds;
            var x = Math.Min(source.Left, shadow.Left);
            var y = Math.Min(source.Top, shadow.Top);
            var right = Math.Max(source.Right, shadow.Right);
            var bottom = Math.Max(source.Bottom, shadow.Bottom);
            writer.StartElement("filter").Attribute("id", SurfaceFilterId(prefix, entry.Key))
                .Attribute("filterUnits", "userSpaceOnUse").Attribute("primitiveUnits", "userSpaceOnUse")
                .Attribute("x", x).Attribute("y", y).Attribute("width", right - x).Attribute("height", bottom - y)
                .Attribute("color-interpolation-filters", "sRGB").EndStartElement();
            writer.StartElement("feMorphology").Attribute("in", "SourceAlpha")
                .Attribute("operator", effect.Spread < 0 ? "erode" : "dilate")
                .Attribute("radius", Math.Abs((long)effect.Spread)).Attribute("result", "spread").EndEmptyElement();
            writer.StartElement("feGaussianBlur").Attribute("in", "spread").Attribute("edgeMode", "none")
                .Attribute("stdDeviation", Number(effect.SigmaX) + " " + Number(effect.SigmaY))
                .Attribute("result", "blur").EndEmptyElement();
            writer.StartElement("feOffset").Attribute("in", "blur").Attribute("dx", effect.OffsetX)
                .Attribute("dy", effect.OffsetY).Attribute("result", "offset").EndEmptyElement();
            writer.StartElement("feFlood").Attribute("flood-color", effect.Color.WithAlpha(255).ToCss())
                .Attribute("flood-opacity", Number(effect.Color.A / 255d * effect.Opacity))
                .Attribute("result", "tint").EndEmptyElement();
            writer.StartElement("feComposite").Attribute("in", "tint").Attribute("in2", "offset")
                .Attribute("operator", "in").Attribute("result", "shadow");
            if (plan.LogicalClip is ChartRect clip) {
                writer.Attribute("x", clip.X).Attribute("y", clip.Y)
                    .Attribute("width", clip.Width).Attribute("height", clip.Height);
            }
            writer.EndEmptyElement();
            writer.StartElement("feMerge").EndStartElement();
            writer.StartElement("feMergeNode").Attribute("in", "shadow").EndEmptyElement();
            writer.StartElement("feMergeNode").Attribute("in", "SourceGraphic").EndEmptyElement();
            writer.EndElement();
            writer.EndElement();
        }
        writer.EndElement();

        string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static void WriteBakedSurfaceShadow(SvgMarkupWriter writer, VisualSurfaceShadowPlan plan,
        CancellationToken cancellation) {
        if (!plan.Visible) return;
        var image = plan.Capture(cancellation);
        cancellation.ThrowIfCancellationRequested();
        var encoded = PngWriter.WriteRgba(image);
        cancellation.ThrowIfCancellationRequested();
        var bounds = plan.Bounds;
        writer.StartElement("image").Attribute("x", bounds.X).Attribute("y", bounds.Y)
            .Attribute("width", bounds.Width).Attribute("height", bounds.Height)
            .Attribute("preserveAspectRatio", "none")
            .Attribute("href", "data:image/png;base64," + Convert.ToBase64String(encoded))
            .Attribute("aria-hidden", "true").Attribute("pointer-events", "none").EndEmptyElement();
    }

    private static void SurfaceIdentity(BinaryWriter writer, VisualSceneSurfaceShadow? surface) {
        if (surface == null) return;
        writer.Write("surface-shadow");
        var effect = surface.Effect;
        writer.Write(effect.Color.ToRgba());
        writer.Write(effect.OffsetX); writer.Write(effect.OffsetY);
        writer.Write(effect.SigmaX); writer.Write(effect.SigmaY);
        writer.Write(effect.Spread); writer.Write(effect.OrderedDither); writer.Write(effect.Opacity);
        Bounds(surface.ObjectBounds); Bounds(surface.InkBounds);
        writer.Write(effect.RelativeRegion.HasValue);
        if (effect.RelativeRegion is ChartRect region) Bounds(region);
        void Bounds(ChartRect bounds) {
            writer.Write(bounds.X); writer.Write(bounds.Y); writer.Write(bounds.Width); writer.Write(bounds.Height);
        }
    }
}

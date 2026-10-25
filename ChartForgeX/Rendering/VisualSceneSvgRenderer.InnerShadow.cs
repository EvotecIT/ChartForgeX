using System;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    /// <summary>One finite alpha chain retains one SourceGraphic; browser Gaussian quantization is profile-specific.</summary>
    private static void WriteInnerShadowFilter(SvgMarkupWriter writer, VisualRetainedNativeEffectPlan native) {
        var effect = native.Scope.Effect.InnerShadow!;
        writer.StartElement("feMorphology").Attribute("in", "SourceAlpha")
            .Attribute("operator", effect.Spread > 0 ? "erode" : "dilate")
            .Attribute("radius", Math.Abs((long)effect.Spread)).Attribute("result", "innerSpread").EndEmptyElement();
        writer.StartElement("feGaussianBlur").Attribute("in", "innerSpread").Attribute("edgeMode", "none")
            .Attribute("stdDeviation", EffectNumber(effect.HorizontalQ8(1) / 256d) + " " + EffectNumber(effect.VerticalQ8(1) / 256d))
            .Attribute("result", "innerSoft").EndEmptyElement();
        writer.StartElement("feOffset").Attribute("in", "innerSoft").Attribute("dx", effect.OffsetX).Attribute("dy", effect.OffsetY)
            .Attribute("result", "innerShifted").EndEmptyElement();
        writer.StartElement("feFlood").Attribute("flood-color", effect.Color.WithAlpha(255).ToCss())
            .Attribute("flood-opacity", EffectNumber(effect.Color.A / 255d * effect.Opacity))
            .Attribute("result", "innerTint").EndEmptyElement();
        writer.StartElement("feComposite").Attribute("in", "innerTint").Attribute("in2", "innerShifted")
            .Attribute("operator", "out").Attribute("result", "innerColor").EndEmptyElement();
        writer.StartElement("feComposite").Attribute("in", "innerColor").Attribute("in2", "SourceGraphic")
            .Attribute("operator", "atop").EndEmptyElement();
    }
}

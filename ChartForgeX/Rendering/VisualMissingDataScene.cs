using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>The same outlined, hatched key for an absent observation in every native output.</summary>
internal static class VisualMissingDataScene {
    internal static void Swatch(VisualSceneBuilder builder, ChartRect box, ChartColorBlend blend, ChartColor ink, string prefix, bool hatch = true) {
        builder.Rect(box, blend.Color, ink, .75, radius: 1, role: prefix + "-missing-swatch",
            paint: new VisualScenePaintBinding(fill: blend.Paint, stroke: SvgPaint.Of(ink, SvgColorRole.Text)));
        if (hatch) ChartMissingData.Hatch(builder, ChartPathBuilder.RoundedRectangle(box, 1), ink, prefix + "-missing");
    }
}

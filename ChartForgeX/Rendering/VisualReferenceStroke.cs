using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Resolves reference-line alpha and paint provenance identically for SVG and raster output.</summary>
internal static class VisualReferenceStroke {
    internal static VisualScenePaintBinding Paint(ChartStrokeStyle? style, ChartColor fallback, SvgColorRole role, out ChartColor color) {
        var source = style?.Color ?? fallback;
        var opacity = style?.Opacity ?? 1;
        color = ChartColorMath.WithOpacity(source, opacity);
        var paint = style?.Color != null ? SvgPaint.Literal(source) : SvgPaint.Of(source, role);
        return VisualChartPaint.Stroke(paint.WithOpacity(color, opacity));
    }
}

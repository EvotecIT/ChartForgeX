using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualColorScaleLegendScene {
    /// <summary>Draws one exact shared gradient; constant domains retain their single mapped color.</summary>
    internal static void Gradient(VisualSceneBuilder builder, ChartColorScale scale, ChartRect bounds,
        double min, double max, string role, bool vertical = false) {
        if (scale.EffectiveMinimum(min) == scale.EffectiveMaximum(max)) {
            var blend = scale.BlendFor(scale.EffectiveMinimum(min), min, max);
            builder.Rect(bounds, blend.Color, role: role, paint: VisualChartPaint.Fill(blend.Paint));
            return;
        }
        var start = new ChartPoint(bounds.Left, vertical ? bounds.Bottom : bounds.Top);
        var end = new ChartPoint(vertical ? bounds.Left : bounds.Right, bounds.Top);
        builder.RectGradient(bounds, start, end, ChartColorScaleLegend.GradientStops(scale, min, max), role: role);
    }
}

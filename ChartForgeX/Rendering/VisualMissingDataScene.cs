using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>The same outlined, hatched key for an absent observation in every native output.</summary>
internal static class VisualMissingDataScene {
    internal static void Swatch(VisualSceneBuilder builder, ChartRect box, ChartColorBlend blend, ChartColor ink, string prefix) {
        builder.Rect(box, blend.Color, ink, .75, radius: 1, role: prefix + "-missing-swatch", paint: VisualChartPaint.Fill(blend.Paint));
        using (builder.PushClip(box)) {
            var hatch = ChartColorMath.WithOpacity(ink, .55); const double spacing = 5;
            var centre = (box.Width - box.Height) / 2;
            for (var step = spacing / 2; centre - step > -box.Height || centre + step < box.Width; step += spacing)
                foreach (var offset in new[] { centre - step, centre + step })
                    builder.Line(box.Left + offset, box.Bottom, box.Left + offset + box.Height, box.Top, hatch, .75, role: prefix + "-missing-hatch");
        }
    }
}

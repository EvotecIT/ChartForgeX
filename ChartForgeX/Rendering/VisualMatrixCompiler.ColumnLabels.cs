using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    /// <summary>Draws measured wrapped captions while keeping the original category in descriptive exports.</summary>
    private static void MatrixColumnText(VisualSceneBuilder builder, string caption, string displayed, ChartRect bounds,
        TextStyle style, string id) {
        var measured = builder.MeasureText(displayed, style);
        if (measured.Width > bounds.Width + .000001 || measured.Height > bounds.Height + .000001) {
            // Explicit horizontal captions retain the ordinary measured shortening policy.
            VisualStateSceneTools.Text(builder, caption, bounds, style, "heatmap-column-label", id, TextAlignment.Center);
            return;
        }
        builder.AddRegion(new VisualSemanticRegion(id, "heatmap-column-label", bounds, caption));
        style = style.Clone(); style.Alignment = TextAlignment.Center;
        var baseline = bounds.Top + (bounds.Height - measured.Height) / 2 + builder.TextAscent(style);
        using (builder.PushClip(bounds)) builder.Text(displayed, bounds.Left + bounds.Width / 2, baseline, style,
            "heatmap-column-label", id, VisualChartPaint.Text(style));
    }
}

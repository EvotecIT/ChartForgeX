using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Builds one bounded card silhouette and its soft shadow using shared native geometry.</summary>
internal static class VisualFrameSurface {
    internal static void Paint(VisualSceneBuilder builder, VisualRenderContext context, VisualThemeColors colors, VisualFramePaints? paints = null) {
        var size = context.Layout.Size;
        var stroke = Math.Min(context.Theme.AxisStrokeWidth, Math.Min(size.Width, size.Height));
        var inset = stroke / 2;
        var shadow = context.Theme.CardShadowColor;
        var opacity = context.Theme.CardShadowOpacity * shadow.A / 255d;
        if (opacity > 0) {
            var padding = context.Layout.PaddingEdges;
            var paddingRoom = Math.Min(Math.Min(padding.Left, padding.Right), Math.Min(padding.Top, padding.Bottom)) - inset;
            var room = Math.Max(0, Math.Min(20, Math.Min(paddingRoom, (Math.Min(size.Width, size.Height) - stroke) / 8)));
            if (room > 0) {
                inset += room;
                // Equal alpha increments composite to the requested total opacity rather than multiplying it
                // by the layer count. Wider silhouettes fade outwards and stay inside the authored canvas.
                const int layers = 12;
                var alpha = (byte)Math.Round(255 * (1 - Math.Pow(1 - opacity, 1d / layers)));
                for (var layer = layers; layer >= 1; layer--) {
                    var spread = room * .6 * layer / layers;
                    var offset = room * .3;
                    builder.Rect(new ChartRect(inset - spread, inset - spread + offset,
                            size.Width - 2 * inset + 2 * spread, size.Height - 2 * inset + 2 * spread),
                        shadow.WithAlpha(alpha), radius: context.Theme.CardRadius + spread, role: "frame-card-shadow");
                }
            } else builder.AddDiagnostic(new VisualDiagnostic("frame.card-shadow-no-room",
                "The authored padding leaves no room for a card shadow; content coordinates are retained."));
        }
        builder.Rect(new ChartRect(inset, inset, size.Width - 2 * inset, size.Height - 2 * inset),
            colors.ElevatedSurface, colors.Border, stroke, radius: context.Theme.CardRadius, role: "frame-card",
            paint: new VisualScenePaintBinding(fill: paints?.ElevatedSurface ?? SvgPaint.Of(colors.ElevatedSurface, SvgColorRole.Surface),
                stroke: paints?.Border ?? SvgPaint.Of(colors.Border, SvgColorRole.Grid)));
    }
}

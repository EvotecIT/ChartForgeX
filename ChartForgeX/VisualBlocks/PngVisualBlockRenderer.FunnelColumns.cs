using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawSegmentedMetricFunnelColumns(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);
        y += 10;

        var hasAction = card.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.14)) : 0;
        var layout = VisualBlockRendering.SegmentedFunnelLayout(card, content, y, footerHeight);

        foreach (var stage in layout.Stages) {
            var item = stage.Item;
            var color = VisualBlockRendering.SegmentedItemColor(theme, item, stage.Index);
            var value = VisualBlockRendering.SegmentedItemDisplayValue(card, item);
            canvas.FillRoundedRect(stage.X, y + 2, 7, 24, 3.5, color);
            DrawAlignedText(canvas, item.Label, stage.X + 11, y + 1, Math.Max(48, stage.Width + layout.GroupGap * 0.5), VisualTextAlignment.Left, theme.MutedText, Math.Max(10, theme.SubtitleFontSize), false);
            DrawAlignedText(canvas, value, stage.X + 11, y + 20, Math.Max(48, stage.Width + layout.GroupGap * 0.5), VisualTextAlignment.Left, theme.Text, Math.Max(13, theme.SubtitleFontSize + 2), true);
            for (var i = 0; i < stage.SegmentCount; i++) {
                var x = stage.X + i * (layout.BarWidth + layout.BarGap);
                var radius = Math.Min(4, layout.BarWidth * 0.48);
                canvas.FillRoundedRectVerticalGradient(x, layout.BarY, layout.BarWidth, layout.BarHeight, radius, ChartSurfacePolish.GradientTop(color), ChartSurfacePolish.GradientBottom(color));
                canvas.FillRoundedRect(x + Math.Max(0.6, layout.BarWidth * 0.18), layout.BarY + 2, Math.Max(0.8, layout.BarWidth * 0.28), Math.Max(1, layout.BarHeight - 4), Math.Min(3, layout.BarWidth * 0.24), ChartColor.White.WithAlpha(48));
            }
        }

        if (hasAction) DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - footerHeight, footerHeight, content.X, content.Width, theme);
    }
}

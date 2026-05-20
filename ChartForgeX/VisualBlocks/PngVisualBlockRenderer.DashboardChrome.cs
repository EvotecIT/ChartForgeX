using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawSegmentedMetricHeading(RgbaCanvas canvas, SegmentedMetricBlock card, ref double y, double x, double width) {
        if (card.HeaderSymbol.Length > 0 || card.ShowMenu) DrawSegmentedMetricHeader(canvas, card, ref y, x, width);
        else DrawHeading(canvas, card, ref y, x, width);
    }

    private static void DrawSegmentedMetricHeader(RgbaCanvas canvas, SegmentedMetricBlock card, ref double y, double x, double width) {
        var theme = card.Options.Theme;
        var layout = VisualBlockRendering.SegmentedHeaderLayout(card, x, y, width);
        if (layout.BadgeSize > 0) {
            canvas.FillRoundedRectVerticalGradient(x, y, layout.BadgeSize, layout.BadgeSize, 14, ChartSurfacePolish.GradientTop(ChartColor.White), ChartSurfacePolish.GradientBottom(ChartColor.White));
            canvas.StrokeRoundedRect(x, y, layout.BadgeSize, layout.BadgeSize, 14, theme.CardBorder, 1);
            DrawAlignedText(canvas, card.HeaderSymbol, x, y + 12, layout.BadgeSize, VisualTextAlignment.Center, theme.Text, 18, true);
        }

        if (card.ShowMenu) {
            for (var i = 0; i < 3; i++) canvas.DrawCircle(layout.MenuDotStartX + i * 7, layout.MenuDotY, 2.1, theme.MutedText);
        }

        if (card.Title.Length > 0) DrawAlignedText(canvas, card.Title, layout.TextX, y, layout.TextWidth, VisualTextAlignment.Left, theme.Text, theme.TitleFontSize, true);
        if (card.Subtitle.Length > 0) DrawAlignedText(canvas, card.Subtitle, layout.TextX, y + theme.TitleFontSize + 8, layout.TextWidth, VisualTextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, false);
        canvas.DrawLine(x, layout.DividerY, x + width, layout.DividerY, theme.PlotBorder, 1);
        y = layout.NextY;
    }
}

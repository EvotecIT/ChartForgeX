using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

using static ChartForgeX.VisualBlocks.PngVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngFactBlockRenderer {
    private static void DrawSegmentedMetricHeading(RgbaCanvas canvas, SegmentedMetricBlock card, ref double y, double x, double width) {
        if (card.HeaderSymbol.Length > 0) DrawSegmentedMetricHeader(canvas, card, ref y, x, width);
        else DrawHeading(canvas, card, ref y, x, width);
    }

    private static void DrawSegmentedMetricHeader(RgbaCanvas canvas, SegmentedMetricBlock card, ref double y, double x, double width) {
        var theme = card.Options.Theme;
        var layout = VisualFactBlockRendering.SegmentedHeaderLayout(card, x, y, width);
        if (layout.BadgeSize > 0) {
            canvas.FillRoundedRectVerticalGradient(x, y, layout.BadgeSize, layout.BadgeSize, 14, ChartSurfacePolish.GradientTop(ChartColor.White), ChartSurfacePolish.GradientBottom(ChartColor.White));
            canvas.StrokeRoundedRect(x, y, layout.BadgeSize, layout.BadgeSize, 14, theme.CardBorder, 1);
            DrawAlignedText(canvas, card.HeaderSymbol, x, y + 12, layout.BadgeSize, TextAlignment.Center, theme.Text, 18, true);
        }


        if (card.Title.Length > 0) DrawAlignedText(canvas, card.Title, layout.TextX, layout.TitleTop, layout.TextWidth, TextAlignment.Left, theme.Text, theme.TitleFontSize, true);
        if (card.Subtitle.Length > 0) DrawAlignedText(canvas, card.Subtitle, layout.TextX, layout.SubtitleTop, layout.TextWidth, TextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, false);
        canvas.DrawLine(x, layout.DividerY, x + width, layout.DividerY, theme.PlotBorder, 1, RasterLineCap.Butt);
        y = layout.NextY;
    }
}

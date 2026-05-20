using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawSegmentedMetricProgressRows(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);
        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.SegmentedProgressRowsLayout(card, y, hasAction);
        for (var rowIndex = 0; rowIndex < card.Items.Count && y + 38 <= layout.Bottom; rowIndex++) {
            var row = card.Items[rowIndex];
            var accent = VisualBlockRendering.SegmentedItemColor(theme, row, rowIndex);
            var rowLayout = VisualBlockRendering.SegmentedProgressRowLayout(card, row, content, y, layout.RowHeight, accent);
            DrawAlignedText(canvas, row.Label, content.X, y, rowLayout.LabelWidth, VisualTextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, true);
            if (row.Delta.Length > 0) {
                canvas.FillRoundedRect(rowLayout.DeltaX, y - 2, rowLayout.DeltaWidth, 22, 11, rowLayout.DeltaColor.WithAlpha(34));
                DrawAlignedText(canvas, row.Delta, rowLayout.DeltaX + 6, y + 3, rowLayout.DeltaWidth - 12, VisualTextAlignment.Center, rowLayout.DeltaColor, theme.SubtitleFontSize, true);
            }

            DrawAlignedText(canvas, rowLayout.ValueText, rowLayout.ValueX, y, rowLayout.ValueWidth, VisualTextAlignment.Right, theme.Text, rowLayout.ValueFontSize, true);
            DrawSegmentedStrip(canvas, row, content.X, rowLayout.StripY, content.Width, rowLayout.StripHeight, accent, theme);
            y += layout.RowHeight;
        }

        if (hasAction) {
            if (card.ActionBackground.HasValue) DrawSegmentedFooterAction(canvas, card, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width);
            else DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
        }
    }

    private static void DrawSegmentedFooterAction(RgbaCanvas canvas, SegmentedMetricBlock card, double footerY, double footerHeight, double x, double width) {
        var theme = card.Options.Theme;
        var fill = card.ActionBackground ?? theme.PlotBackground;
        var foreground = card.ActionForeground ?? theme.Text;
        var inset = Math.Min(8, Math.Max(1, footerHeight * 0.16));
        canvas.FillRoundedRect(x, footerY + 1, width, Math.Max(1, footerHeight - inset - 1), Math.Min(10, Math.Max(2, footerHeight * 0.18)), fill);
        var fontSize = Math.Max(10, theme.SubtitleFontSize);
        var y = footerY + (footerHeight - fontSize) * 0.52;
        DrawAlignedText(canvas, card.ActionLabel, x, y, Math.Max(1, width - 38), VisualTextAlignment.Left, foreground, fontSize, false);
        DrawActionSymbol(canvas, card.ActionSymbol, x + width - 18, footerY + footerHeight * 0.5, 12, foreground, fontSize);
    }

    private static void DrawSegmentedStrip(RgbaCanvas canvas, SegmentedMetricItem row, double x, double y, double width, double height, ChartColor accent, ChartForgeX.Themes.ChartTheme theme) {
        var empty = theme.CardBackground.A > 0 ? theme.CardBackground : ChartColor.White;
        var emptyStroke = theme.PlotBorder.WithAlpha(120);
        foreach (var segment in VisualBlockRendering.SegmentedProgressStripSegments(row, x, y, width, height)) {
            var color = segment.Filled ? accent : empty;
            canvas.FillRoundedRect(segment.X + 0.6, segment.Y + 1.2, segment.Width, segment.Height, segment.Radius, theme.MutedText.WithAlpha(segment.Filled ? (byte)28 : (byte)18));
            if (segment.Filled) {
                canvas.FillRoundedRectVerticalGradient(segment.X, segment.Y, segment.Width, segment.Height, segment.Radius, ChartSurfacePolish.GradientTop(color), ChartSurfacePolish.GradientBottom(color));
                canvas.StrokeRoundedRect(segment.X, segment.Y, segment.Width, segment.Height, segment.Radius, accent.WithAlpha(120), 0.8);
                canvas.FillRoundedRect(segment.X + 1, segment.Y + 1, Math.Max(1, segment.Width - 2), Math.Max(1, segment.Height * 0.32), Math.Min(3, segment.Width * 0.28), ChartColor.White.WithAlpha(48));
            } else {
                canvas.FillRoundedRectVerticalGradient(segment.X, segment.Y, segment.Width, segment.Height, segment.Radius, ChartSurfacePolish.GradientTop(empty), ChartSurfacePolish.GradientBottom(empty));
                canvas.StrokeRoundedRect(segment.X, segment.Y, segment.Width, segment.Height, segment.Radius, emptyStroke, 0.8);
                canvas.FillRoundedRect(segment.X + 1, segment.Y + 1, Math.Max(1, segment.Width - 2), Math.Max(1, segment.Height * 0.32), Math.Min(3, segment.Width * 0.28), ChartColor.White.WithAlpha(92));
            }
        }
    }
}

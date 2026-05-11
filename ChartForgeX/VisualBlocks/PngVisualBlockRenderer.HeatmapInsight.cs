using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawHeatmapInsightCard(RgbaCanvas canvas, HeatmapInsightCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, card, ref y, content.X, content.Width);
        DrawHeatmapControls(canvas, card, content.X, y, content.Width);
        y += card.LeftControl.Length > 0 || card.SelectedControl.Length > 0 || card.PeriodLabel.Length > 0 ? 34 : 2;
        var railGap = card.Insights.Count == 0 ? 0 : Math.Min(24, Math.Max(8, content.Width * 0.06));
        var minimumMatrixWidth = Math.Min(content.Width, 92 + card.Columns.Count * 12);
        var desiredRailWidth = card.Insights.Count == 0 ? 0 : Math.Min(190, Math.Max(90, content.Width * 0.28));
        var railWidth = card.Insights.Count == 0 ? 0 : Math.Min(desiredRailWidth, Math.Max(0, content.Width - railGap - minimumMatrixWidth));
        if (railWidth < 72) { railWidth = 0; railGap = 0; }
        var matrixWidth = Math.Max(1, content.Width - railWidth - railGap);
        var labelWidth = Math.Min(54, Math.Max(44, matrixWidth * 0.13));
        var cellGap = 6.0;
        var cellWidth = Math.Max(10, (matrixWidth - labelWidth - cellGap * Math.Max(0, card.Columns.Count - 1)) / card.Columns.Count);
        var cellHeight = Math.Max(16, Math.Min(24, (options.Size.Height - options.Padding.Bottom - y - 22) / Math.Max(1, card.Rows.Count + 1)));
        var matrixX = content.X;
        var headerY = y;
        var gridY = headerY + cellHeight + 8;

        for (var column = 0; column < card.Columns.Count; column++) {
            DrawAlignedText(canvas, card.Columns[column], matrixX + labelWidth + column * (cellWidth + cellGap), headerY + cellHeight * 0.18, cellWidth, VisualTextAlignment.Center, theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 2), true);
        }

        for (var row = 0; row < card.Rows.Count; row++) {
            var item = card.Rows[row];
            var rowY = gridY + row * (cellHeight + 5);
            DrawAlignedText(canvas, item.Label, matrixX, rowY + cellHeight * 0.2, labelWidth - 6, VisualTextAlignment.Right, theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 2), true);
            for (var column = 0; column < card.Columns.Count; column++) {
                var value = item.Values[column];
                var color = HeatmapInsightColorForPng(card, value);
                var x = matrixX + labelWidth + column * (cellWidth + cellGap);
                canvas.FillRoundedRect(x, rowY, cellWidth, cellHeight, Math.Min(5, cellHeight * 0.32), color);
                DrawAlignedText(canvas, value.ToString("0", CultureInfo.InvariantCulture), x, rowY + cellHeight * 0.2, cellWidth, VisualTextAlignment.Center, ChartColorMath.TextOnBackground(color), Math.Max(9, theme.SubtitleFontSize - 2), true);
            }
        }

        if (railWidth > 0) DrawHeatmapInsightRail(canvas, card, content.X + matrixWidth + railGap, gridY, railWidth, Math.Max(1, options.Size.Height - options.Padding.Bottom - gridY));
    }

    private static void DrawHeatmapControls(RgbaCanvas canvas, HeatmapInsightCard card, double x, double y, double width) {
        var theme = card.Options.Theme;
        var buttonHeight = 24.0;
        var cursor = x;
        if (card.LeftControl.Length > 0) {
            var w = Math.Min(70, RgbaCanvas.MeasureTextEmphasizedWidth(card.LeftControl, theme.SubtitleFontSize, null) + 22);
            canvas.FillRoundedRect(cursor, y, w, buttonHeight, 8, theme.PlotBackground);
            DrawAlignedText(canvas, card.LeftControl, cursor, y + 5, w, VisualTextAlignment.Center, theme.MutedText, Math.Max(10, theme.SubtitleFontSize - 1), true);
            cursor += w - 2;
        }

        if (card.SelectedControl.Length > 0) {
            var w = Math.Min(80, RgbaCanvas.MeasureTextEmphasizedWidth(card.SelectedControl, theme.SubtitleFontSize, null) + 24);
            canvas.FillRoundedRect(cursor, y, w, buttonHeight, 8, ChartColor.White);
            canvas.StrokeRoundedRect(cursor, y, w, buttonHeight, 8, theme.CardBorder, 1);
            DrawAlignedText(canvas, card.SelectedControl, cursor, y + 5, w, VisualTextAlignment.Center, VisualBlockRendering.PaletteAt(theme, 0), Math.Max(10, theme.SubtitleFontSize - 1), true);
            cursor += w + 14;
        }

        if (card.PeriodLabel.Length > 0) {
            var remaining = Math.Max(0, width - (cursor - x));
            if (remaining < 24) return;
            var w = Math.Min(remaining, RgbaCanvas.MeasureTextEmphasizedWidth(card.PeriodLabel, theme.SubtitleFontSize, null) + 34);
            canvas.FillRoundedRect(cursor, y, w, buttonHeight, 8, ChartColor.White);
            canvas.StrokeRoundedRect(cursor, y, w, buttonHeight, 8, theme.CardBorder, 1);
            DrawAlignedText(canvas, card.PeriodLabel, cursor + 12, y + 5, w - 24, VisualTextAlignment.Left, theme.Text, Math.Max(10, theme.SubtitleFontSize - 1), true);
        }
    }

    private static void DrawHeatmapInsightRail(RgbaCanvas canvas, HeatmapInsightCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        DrawAlignedText(canvas, card.InsightTitle, x, y + 2, width, VisualTextAlignment.Left, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        var itemY = y + 42;
        foreach (var item in card.Insights) {
            DrawAlignedText(canvas, item.Label, x, itemY - 10, width, VisualTextAlignment.Left, theme.Text, Math.Max(10, theme.SubtitleFontSize - 1), true);
            DrawAlignedText(canvas, item.Detail, x, itemY + 8, width, VisualTextAlignment.Left, theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 2), false);
            itemY += 48;
        }

        var keyY = Math.Min(y + height - 46, Math.Max(itemY + 8, y + 150));
        DrawAlignedText(canvas, card.ColorKeyLabel, x, keyY - 12, width, VisualTextAlignment.Left, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        keyY += 12;
        DrawAlignedText(canvas, card.Minimum.ToString("0", CultureInfo.InvariantCulture), x, keyY - 4, 18, VisualTextAlignment.Left, theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 2), true);
        var keyX = x + 22;
        var keyWidth = Math.Max(30, width - 48);
        var steps = 16;
        var stepWidth = keyWidth / steps;
        for (var step = 0; step < steps; step++) {
            var value = card.Minimum + (card.Maximum - card.Minimum) * (step + 0.5) / steps;
            canvas.FillRoundedRect(keyX + step * stepWidth, keyY, stepWidth + 0.6, 7, step == 0 || step == steps - 1 ? 4 : 0, HeatmapInsightColorForPng(card, value));
        }

        DrawAlignedText(canvas, card.Maximum.ToString("0", CultureInfo.InvariantCulture), x + width - 20, keyY - 4, 20, VisualTextAlignment.Right, theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 2), true);
    }

    private static ChartColor HeatmapInsightColorForPng(HeatmapInsightCard card, double value) {
        var ratio = Math.Max(0, Math.Min(1, (value - card.Minimum) / (card.Maximum - card.Minimum)));
        return ChartColor.FromRgba((byte)Math.Round(card.LowColor.R + (card.HighColor.R - card.LowColor.R) * ratio), (byte)Math.Round(card.LowColor.G + (card.HighColor.G - card.LowColor.G) * ratio), (byte)Math.Round(card.LowColor.B + (card.HighColor.B - card.LowColor.B) * ratio), 255);
    }
}

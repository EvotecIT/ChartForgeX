using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawDistributionStripCard(RgbaCanvas canvas, DistributionStripCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, card, ref y, content.X, content.Width);

        var hasAction = card.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.13)) : 0;
        var bottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var metricSize = Math.Min(38, Math.Max(24, options.Size.Height * 0.12));
        var captionWidth = card.Caption.Length == 0 ? 0 : Math.Min(content.Width * 0.34, RgbaCanvas.MeasureTextWidth(card.Caption, theme.SubtitleFontSize, null) + 12);

        DrawAlignedText(canvas, card.Label, content.X, y, content.Width - captionWidth - 10, VisualTextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, true);
        if (card.Caption.Length > 0) DrawAlignedText(canvas, card.Caption, content.X + content.Width - captionWidth, y, captionWidth, VisualTextAlignment.Right, theme.MutedText, theme.SubtitleFontSize, true);
        DrawAlignedText(canvas, card.Value, content.X, y + theme.SubtitleFontSize + 16, content.Width, VisualTextAlignment.Left, theme.Text, metricSize, true);
        y += theme.SubtitleFontSize + metricSize + 30;

        var stripHeight = Math.Max(14, Math.Min(24, options.Size.Height * 0.065));
        DrawDistributionStack(canvas, card, content.X, y, content.Width, stripHeight);
        y += stripHeight + 12;

        var legendHeight = DrawDistributionLegend(canvas, card, content.X, y, content.Width);
        y += legendHeight + 10;

        var rowCount = Math.Max(1, card.Segments.Count);
        var rowHeight = Math.Max(27, Math.Min(36, (bottom - y) / rowCount));
        for (var i = 0; i < card.Segments.Count && y + rowHeight <= bottom + 1; i++) {
            DrawDistributionRow(canvas, card, card.Segments[i], i, content.X, y, content.Width, rowHeight);
            y += rowHeight;
        }

        if (hasAction) DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - footerHeight, footerHeight, content.X, content.Width, theme);
    }

    private static void DrawDistributionStack(RgbaCanvas canvas, DistributionStripCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var total = VisualBlockRendering.DistributionTotal(card);
        var gap = VisualBlockRendering.EffectiveStackGap(card.Segments.Count, width, 5);
        var segmentArea = Math.Max(0, width - gap * Math.Max(0, card.Segments.Count - 1));
        var cursor = x;
        for (var i = 0; i < card.Segments.Count; i++) {
            var segment = card.Segments[i];
            var segmentWidth = i == card.Segments.Count - 1 ? x + width - cursor : Math.Max(0, segmentArea * segment.Value / total);
            if (segmentWidth <= 0) continue;
            var color = segment.Color ?? (segment.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, segment.Status));
            canvas.FillRoundedRect(cursor, y, segmentWidth, height, Math.Min(6, height / 2), color);
            for (var lineX = cursor - height; lineX < cursor + segmentWidth; lineX += 12) {
                canvas.DrawLine(lineX, y + height, lineX + height, y, ChartColor.White.WithAlpha(38), 2);
            }

            cursor += segmentWidth + gap;
        }
    }

    private static double DrawDistributionLegend(RgbaCanvas canvas, DistributionStripCard card, double x, double y, double width) {
        var theme = card.Options.Theme;
        var rowHeight = 20.0;
        var cursorX = x;
        var cursorY = y;
        var total = VisualBlockRendering.DistributionTotal(card);
        for (var i = 0; i < card.Segments.Count; i++) {
            var segment = card.Segments[i];
            var color = segment.Color ?? (segment.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, segment.Status));
            var label = ShortDistributionLabelForPng(segment.Label) + " " + (segment.Value / total * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";
            var fontSize = Math.Max(9, theme.SubtitleFontSize - 2);
            var chipWidth = Math.Min(140, Math.Max(54, RgbaCanvas.MeasureTextEmphasizedWidth(label, fontSize, null) + 18));
            if (cursorX + chipWidth > x + width && cursorX > x) {
                cursorX = x;
                cursorY += rowHeight;
            }

            canvas.DrawCircle(cursorX + 4, cursorY + 8, 4, color);
            DrawAlignedText(canvas, label, cursorX + 13, cursorY + 2, chipWidth - 13, VisualTextAlignment.Left, theme.Text, fontSize, true);
            cursorX += chipWidth + 10;
        }

        return cursorY - y + rowHeight;
    }

    private static void DrawDistributionRow(RgbaCanvas canvas, DistributionStripCard card, DistributionStripSegment segment, int index, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var color = segment.Color ?? (segment.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, index) : VisualBlockRendering.StatusColor(theme, segment.Status));
        var share = segment.Value / VisualBlockRendering.DistributionTotal(card);
        var rowY = y + height * 0.5;
        var badgeSize = Math.Min(22, Math.Max(16, height - 9));
        var ringRadius = Math.Min(8.5, Math.Max(6, height * 0.24));
        var percentWidth = 58.0;
        var detailWidth = segment.Detail.Length == 0 ? 0 : Math.Min(96, RgbaCanvas.MeasureTextEmphasizedWidth(segment.Detail, theme.SubtitleFontSize, null) + 8);
        var labelWidth = Math.Max(1, width - badgeSize - 16 - percentWidth - detailWidth - 34);

        canvas.FillRoundedRect(x, rowY - badgeSize / 2, badgeSize, badgeSize, Math.Min(7, badgeSize * 0.32), color.WithAlpha(34));
        canvas.StrokeRoundedRect(x, rowY - badgeSize / 2, badgeSize, badgeSize, Math.Min(7, badgeSize * 0.32), color.WithAlpha(110), 1);
        DrawAlignedText(canvas, segment.Symbol.Length > 0 ? segment.Symbol : ShortDistributionLabelForPng(segment.Label), x + 2, rowY - badgeSize * 0.22, badgeSize - 4, VisualTextAlignment.Center, color, Math.Max(8, badgeSize * 0.42), true);
        DrawAlignedText(canvas, segment.Label, x + badgeSize + 12, rowY - theme.SubtitleFontSize * 0.36, labelWidth, VisualTextAlignment.Left, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        if (segment.Detail.Length > 0) DrawAlignedText(canvas, segment.Detail, x + width - detailWidth, rowY - theme.SubtitleFontSize * 0.36, detailWidth, VisualTextAlignment.Right, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        DrawAlignedText(canvas, (share * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%", x + width - detailWidth - percentWidth - 6, rowY - theme.SubtitleFontSize * 0.36, percentWidth, VisualTextAlignment.Right, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        DrawDistributionRing(canvas, x + width - detailWidth - percentWidth - 24, rowY, ringRadius, share, color, theme.PlotBorder);
    }

    private static void DrawDistributionRing(RgbaCanvas canvas, double cx, double cy, double radius, double ratio, ChartColor color, ChartColor trackColor) {
        canvas.DrawCircleOutline(cx, cy, radius, trackColor, 2);
        if (ratio <= 0) return;
        canvas.DrawArc(cx, cy, radius, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * Math.Max(0.002, Math.Min(1, ratio)), color, 2.4);
    }

    private static string ShortDistributionLabelForPng(string value) {
        var start = value.LastIndexOf('(');
        var end = value.LastIndexOf(')');
        if (start >= 0 && end > start + 1) return value.Substring(start + 1, end - start - 1);
        var trimmed = value.Trim();
        return trimmed.Length <= 4 ? trimmed : trimmed.Substring(0, Math.Min(3, trimmed.Length)).ToUpperInvariant();
    }
}

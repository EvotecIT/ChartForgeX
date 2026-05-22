using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawSegmentedMetricCapsuleSplit(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);
        y += 8;

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.CapsuleSplitLayout(options, content, y, hasAction);

        DrawCapsuleTrack(canvas, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight);
        DrawCapsuleSegments(canvas, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight);

        DrawCapsuleLegend(canvas, card, layout.LegendX, layout.LegendY, layout.LegendWidth, layout.LegendHeight);
        if (hasAction) DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void DrawCapsuleTrack(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        canvas.FillRoundedRect(x, y + 1.6, width, height, height / 2, theme.MutedText.WithAlpha(14));
        canvas.FillRoundedRect(x, y, width, height, height / 2, theme.CardBackground.WithAlpha(160));
    }

    private static void DrawCapsuleSegments(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var segments = VisualBlockRendering.SegmentedStackSegments(card, x, y, width, height, 3);
        var radius = height / 2;
        for (var i = 0; i < segments.Count; i++) {
            var segment = segments[i];
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment.Item, segment.Index);
            if (segment.Width <= 0) continue;
            var rounded = i == 0 || i == segments.Count - 1;
            canvas.FillRoundedRect(segment.X, segment.Y, segment.Width, segment.Height, rounded ? Math.Min(radius, segment.Width / 2) : 0, color);
            if (rounded && segment.Width > radius) {
                var squareX = i == 0 ? segment.X + radius : segment.X;
                canvas.FillRoundedRect(squareX, segment.Y, Math.Max(0, segment.Width - radius), segment.Height, 0, color);
            }

            if (segment.Share >= 0.075) DrawCapsuleSegmentLabel(canvas, card, segment, color);
        }
    }

    private static void DrawCapsuleSegmentLabel(RgbaCanvas canvas, SegmentedMetricBlock card, SegmentedStackSegment segment, ChartColor color) {
        var label = (segment.Share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        var fontSize = Math.Max(9, Math.Min(12, segment.Height * 0.48));
        var foreground = VisualBlockRendering.CapsuleSplitLabelForeground(color);
        var halo = VisualBlockRendering.CapsuleSplitLabelHalo(foreground);
        var labelX = segment.X + segment.Width / 2 - Math.Max(18, segment.Width * 0.32);
        var labelY = segment.Y + segment.Height / 2 - fontSize * 0.46;
        var labelWidth = Math.Max(36, segment.Width * 0.64);
        DrawAlignedText(canvas, label, labelX - 0.7, labelY, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX + 0.7, labelY, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX, labelY - 0.7, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX, labelY + 0.7, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX, labelY, labelWidth, VisualTextAlignment.Center, foreground, fontSize, true);
    }

    private static void DrawCapsuleLegend(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var rowHeight = Math.Max(26, Math.Min(34, height / Math.Max(1, card.Items.Count)));
        var total = VisualBlockRendering.SegmentedTotal(card);
        var bottom = y + height;
        for (var i = 0; i < card.Items.Count && y + rowHeight <= bottom + 1; i++) {
            var item = card.Items[i];
            var color = VisualBlockRendering.SegmentedItemColor(theme, item, i);
            var value = VisualBlockRendering.SegmentedCapsuleLegendValue(card, item, total);
            canvas.FillRoundedRect(x, y + rowHeight * 0.26, 12, 12, 3, color);
            DrawAlignedText(canvas, item.Label, x + 22, y + rowHeight * 0.24, width * 0.56, VisualTextAlignment.Left, theme.MutedText, Math.Max(11, theme.SubtitleFontSize), true);
            DrawAlignedText(canvas, value, x + width * 0.60, y + rowHeight * 0.24, width * 0.40, VisualTextAlignment.Right, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
            y += rowHeight;
        }
    }
}

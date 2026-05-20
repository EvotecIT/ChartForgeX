using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawDistributionRows(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.SegmentedDistributionLayout(card, content, y, hasAction);

        DrawAlignedText(canvas, card.Label, content.X, y, content.Width - layout.CaptionWidth - 10, VisualTextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, true);
        if (card.Caption.Length > 0) DrawAlignedText(canvas, card.Caption, content.X + content.Width - layout.CaptionWidth, y, layout.CaptionWidth, VisualTextAlignment.Right, theme.MutedText, theme.SubtitleFontSize, true);
        DrawAlignedText(canvas, card.Value, content.X, y + theme.SubtitleFontSize + 16, content.Width, VisualTextAlignment.Left, theme.Text, layout.MetricSize, true);
        DrawDistributionStack(canvas, card, content.X, layout.StripY, content.Width, layout.StripHeight);
        DrawDistributionLegend(canvas, card, layout);
        y = layout.RowsY;
        for (var i = 0; i < card.Items.Count && y + layout.RowHeight <= layout.Bottom + 1; i++) {
            DrawDistributionRow(canvas, card, card.Items[i], i, content.X, y, content.Width, layout.RowHeight);
            y += layout.RowHeight;
        }

        if (hasAction) DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void DrawDistributionStack(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        foreach (var segment in VisualBlockRendering.SegmentedStackSegments(card, x, y, width, height, 5)) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment.Item, segment.Index);
            canvas.FillRoundedRect(segment.X, segment.Y, segment.Width, segment.Height, Math.Min(6, height / 2), color);
            var radius = Math.Min(6, height / 2);
            foreach (var line in ChartPatternLineGeometry.Build(ChartFillPattern.DiagonalForward, segment.X, segment.Y, segment.Width, segment.Height, radius, 12)) {
                canvas.DrawLine(line.X1, line.Y1, line.X2, line.Y2, ChartColor.White.WithAlpha(38), 2);
            }
        }
    }

    private static void DrawDistributionLegend(RgbaCanvas canvas, SegmentedMetricBlock card, SegmentedDistributionLayout layout) {
        var theme = card.Options.Theme;
        foreach (var chip in layout.LegendChips) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, chip.Item, chip.Index);
            canvas.DrawCircle(chip.X + 4, chip.Y + 8, 4, color);
            DrawAlignedText(canvas, chip.Label, chip.X + 13, chip.Y + 2, chip.Width - 13, VisualTextAlignment.Left, theme.Text, chip.FontSize, true);
        }
    }

    private static void DrawDistributionRow(RgbaCanvas canvas, SegmentedMetricBlock card, SegmentedMetricItem segment, int index, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var color = VisualBlockRendering.SegmentedItemColor(theme, segment, index);
        var layout = VisualBlockRendering.SegmentedDistributionRowLayout(card, segment, x, y, width, height);

        canvas.FillRoundedRect(x, layout.Y - layout.BadgeSize / 2, layout.BadgeSize, layout.BadgeSize, Math.Min(7, layout.BadgeSize * 0.32), color.WithAlpha(34));
        canvas.StrokeRoundedRect(x, layout.Y - layout.BadgeSize / 2, layout.BadgeSize, layout.BadgeSize, Math.Min(7, layout.BadgeSize * 0.32), color.WithAlpha(110), 1);
        DrawAlignedText(canvas, layout.SymbolText, x + 2, layout.Y - layout.BadgeSize * 0.22, layout.BadgeSize - 4, VisualTextAlignment.Center, color, Math.Max(8, layout.BadgeSize * 0.42), true);
        DrawAlignedText(canvas, segment.Label, x + layout.BadgeSize + 12, layout.Y - theme.SubtitleFontSize * 0.36, layout.LabelWidth, VisualTextAlignment.Left, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        if (segment.DisplayValue.Length > 0) DrawAlignedText(canvas, segment.DisplayValue, x + width - layout.DisplayValueWidth, layout.Y - theme.SubtitleFontSize * 0.36, layout.DisplayValueWidth, VisualTextAlignment.Right, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        DrawAlignedText(canvas, layout.PercentText, x + width - layout.DisplayValueWidth - layout.PercentWidth - 6, layout.Y - theme.SubtitleFontSize * 0.36, layout.PercentWidth, VisualTextAlignment.Right, theme.Text, Math.Max(11, theme.SubtitleFontSize), true);
        DrawDistributionRing(canvas, x + width - layout.DisplayValueWidth - layout.PercentWidth - 24, layout.Y, layout.RingRadius, layout.Share, color, theme.PlotBorder);
    }

    private static void DrawDistributionRing(RgbaCanvas canvas, double cx, double cy, double radius, double ratio, ChartColor color, ChartColor trackColor) {
        canvas.DrawCircleOutline(cx, cy, radius, trackColor, 2);
        if (ratio <= 0) return;
        canvas.DrawArc(cx, cy, radius, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * Math.Max(0.002, Math.Min(1, ratio)), color, 2.4);
    }
}

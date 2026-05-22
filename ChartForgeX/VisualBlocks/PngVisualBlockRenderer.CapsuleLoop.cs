using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawSegmentedMetricCapsuleLoop(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);
        y += 8;

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.CapsuleLoopLayout(options, content, y, hasAction);

        DrawCapsuleTrack(canvas, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);
        DrawCapsuleSegments(canvas, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);

        DrawCapsuleLegend(canvas, card, layout.LegendX, layout.LegendY, layout.LegendWidth, layout.LegendHeight);
        if (hasAction) DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void DrawCapsuleTrack(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        DrawCapsuleLoopStroke(canvas, x, y + 1.4, width, height, stroke + 2, 0, 1, theme.MutedText.WithAlpha(14), RasterLineCap.Round);
        DrawCapsuleLoopStroke(canvas, x, y, width, height, stroke, 0, 1, theme.CardBackground.WithAlpha(170), RasterLineCap.Round);
    }

    private static void DrawCapsuleSegments(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        foreach (var slice in VisualBlockRendering.SegmentedSlices(card)) {
            if (slice.Share <= 0) continue;
            var color = VisualBlockRendering.SegmentedItemColor(theme, slice.Item, slice.Index);
            var start = Math.Max(0, slice.Start - 0.002);
            var end = Math.Min(1, slice.End + 0.002);
            DrawCapsuleLoopStroke(canvas, x, y, width, height, stroke, start, end, color, RasterLineCap.Butt);
            DrawCapsuleLoopStroke(canvas, x, y, width, height, VisualBlockRendering.CapsuleLoopSheenStroke(stroke), start, end, VisualBlockRendering.CapsuleLoopSheenColor(color), RasterLineCap.Butt);
            if (slice.Share >= 0.075) DrawCapsuleSegmentLabel(canvas, slice, color, x, y, width, height, stroke);
        }
    }

    private static void DrawCapsuleSegmentLabel(RgbaCanvas canvas, SegmentedMetricSlice slice, ChartColor color, double x, double y, double width, double height, double stroke) {
        var label = (slice.Share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        var fontSize = Math.Max(9, Math.Min(12, stroke * 0.42));
        var point = VisualBlockRendering.CapsuleLoopPoint(x, y, width, height, stroke, (slice.Start + slice.End) / 2);
        var foreground = VisualBlockRendering.CapsuleLoopLabelForeground(color);
        var halo = VisualBlockRendering.CapsuleLoopLabelHalo(foreground);
        var labelX = point.X - Math.Max(18, stroke * 0.95);
        var labelY = point.Y - fontSize * 0.46;
        var labelWidth = Math.Max(36, stroke * 1.9);
        DrawAlignedText(canvas, label, labelX - 0.7, labelY, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX + 0.7, labelY, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX, labelY - 0.7, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX, labelY + 0.7, labelWidth, VisualTextAlignment.Center, halo, fontSize, true);
        DrawAlignedText(canvas, label, labelX, labelY, labelWidth, VisualTextAlignment.Center, foreground, fontSize, true);
    }

    private static void DrawCapsuleLoopStroke(RgbaCanvas canvas, double x, double y, double width, double height, double stroke, double start, double end, ChartColor color, RasterLineCap lineCap) {
        if (color.A == 0) return;
        foreach (var part in VisualBlockRendering.CapsuleLoopParts(x, y, width, height, stroke, start, end)) {
            if (part.Kind == CapsuleLoopPartKind.Line) {
                canvas.DrawLine(part.StartPoint.X, part.StartPoint.Y, part.EndPoint.X, part.EndPoint.Y, color, stroke, lineCap);
            } else {
                canvas.DrawArc(part.CenterX, part.CenterY, part.Radius, part.StartAngle, part.EndAngle, color, stroke, lineCap);
            }
        }
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

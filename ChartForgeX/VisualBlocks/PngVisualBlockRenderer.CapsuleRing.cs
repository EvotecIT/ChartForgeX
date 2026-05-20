using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawSegmentedMetricCapsuleRing(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);
        y += 8;

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.CapsuleRingLayout(options, content, y, hasAction);

        DrawCapsuleTrack(canvas, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);
        DrawCapsuleSegments(canvas, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);

        DrawCapsuleLegend(canvas, card, layout.LegendX, layout.LegendY, layout.LegendWidth, layout.LegendHeight);
        if (hasAction) DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void DrawCapsuleTrack(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        canvas.DrawPolyline(VisualBlockRendering.CapsuleRingSegmentPoints(x, y + 1.1, width, height, 0, 1, VisualBlockRendering.CapsuleRingSamples), theme.MutedText.WithAlpha(16), stroke + 2);
        canvas.DrawPolyline(VisualBlockRendering.CapsuleRingSegmentPoints(x, y, width, height, 0, 1, VisualBlockRendering.CapsuleRingSamples), theme.PlotBackground, stroke);
    }

    private static void DrawCapsuleSegments(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var gap = VisualBlockRendering.CapsuleRingBoundaryGapRatio(width, height, stroke);
        foreach (var slice in VisualBlockRendering.SegmentedSlices(card)) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, slice.Item, slice.Index);
            var segmentGap = Math.Min(gap, Math.Max(0, slice.Share * 0.28));
            var start = Math.Min(slice.End, slice.Start + segmentGap);
            var end = Math.Max(start, slice.End - segmentGap);
            if (end - start <= 0.001) continue;
            var points = VisualBlockRendering.CapsuleRingSegmentPoints(x, y, width, height, start, end, VisualBlockRendering.CapsuleRingSamples);
            DrawCapsulePolyline(canvas, points, color, stroke);
            if (slice.Share >= 0.075) DrawCapsuleSegmentLabel(canvas, card, slice, color, x, y, width, height, stroke);
        }
    }

    private static void DrawCapsulePolyline(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, ChartColor color, double thickness) {
        canvas.DrawPolyline(points, color, thickness);
        var radius = Math.Max(0.5, thickness / 2);
        for (var i = 1; i < points.Count - 1; i++) canvas.DrawCircle(points[i].X, points[i].Y, radius, color);
    }

    private static void DrawCapsuleSegmentLabel(RgbaCanvas canvas, SegmentedMetricBlock card, SegmentedMetricSlice slice, ChartColor color, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var point = VisualBlockRendering.CapsuleRingPoint(x, y, width, height, (slice.Start + slice.End) / 2);
        var label = (slice.Share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        var fontSize = Math.Max(9, Math.Min(12, stroke * 0.42));
        var foreground = VisualBlockRendering.CapsuleRingLabelForeground(color);
        var halo = VisualBlockRendering.CapsuleRingLabelHalo(foreground);
        var labelX = point.X - Math.Max(20, stroke);
        var labelY = point.Y - fontSize * 0.46;
        var labelWidth = Math.Max(40, stroke * 2);
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

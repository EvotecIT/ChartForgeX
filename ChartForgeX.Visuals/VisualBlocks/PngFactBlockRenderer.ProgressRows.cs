using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

using static ChartForgeX.VisualBlocks.PngVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngFactBlockRenderer {
    private static void DrawSegmentedMetricProgressRows(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);
        var layout = VisualFactBlockRendering.SegmentedProgressRowsLayout(card, y);
        for (var rowIndex = 0; rowIndex < card.Items.Count; rowIndex++) {
            var row = card.Items[rowIndex];
            var accent = VisualFactBlockRendering.SegmentedItemColor(theme, row, rowIndex);
            var rowLayout = VisualFactBlockRendering.SegmentedProgressRowLayout(card, row, content, y, layout.RowHeight, accent);
            if (!VisualFactBlockRendering.CanRenderProgressRow(rowLayout, layout.Bottom)) break;
            DrawAlignedText(canvas, row.Label, content.X, y, rowLayout.LabelWidth, TextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, true);
            if (row.Delta.Length > 0) {
                canvas.FillRoundedRect(rowLayout.DeltaX, y - 2, rowLayout.DeltaWidth, 22, 11, rowLayout.DeltaColor.WithAlpha(34));
                DrawAlignedText(canvas, row.Delta, rowLayout.DeltaX + 6, y + 3, rowLayout.DeltaWidth - 12, TextAlignment.Center, rowLayout.DeltaColor, theme.SubtitleFontSize, true);
            }

            DrawAlignedText(canvas, rowLayout.ValueText, rowLayout.ValueX, y, rowLayout.ValueWidth, TextAlignment.Right, theme.Text, rowLayout.ValueFontSize, true);
            DrawSegmentedStrip(canvas, row, content.X, rowLayout.StripY, content.Width, rowLayout.StripHeight, accent, theme);
            y += layout.RowHeight;
        }

    }


    private static void DrawSegmentedStrip(RgbaCanvas canvas, SegmentedMetricItem row, double x, double y, double width, double height, ChartColor accent, ChartForgeX.Themes.ChartTheme theme) {
        var empty = theme.CardBackground.A > 0 ? theme.CardBackground : ChartColor.White;
        var emptyStroke = theme.PlotBorder.WithAlpha(120);
        foreach (var segment in VisualFactBlockRendering.SegmentedProgressStripSegments(row, x, y, width, height)) {
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

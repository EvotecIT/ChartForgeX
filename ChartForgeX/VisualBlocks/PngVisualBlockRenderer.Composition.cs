using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawSegmentedMetricComposition(RgbaCanvas canvas, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawSegmentedMetricHeading(canvas, card, ref y, content.X, content.Width);
        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.SegmentedCompositionLayout(card, content, y, hasAction);
        DrawAlignedText(canvas, card.Label, content.X, y, content.Width * 0.58, VisualTextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, true);
        DrawAlignedText(canvas, card.Value, content.X + content.Width * 0.58, y + layout.MetricSize * 0.08, content.Width * 0.42, VisualTextAlignment.Right, theme.Text, layout.MetricSize, true);
        y = layout.StripY;
        DrawCompositionStrip(canvas, card, content.X, y, content.Width, layout.StripHeight);
        y = layout.LegendY;
        DrawCompositionLegend(canvas, card, content, ref y, layout.RowHeight, layout.LegendBottom);
        if (hasAction) DrawFooterAction(canvas, card.ActionLabel, card.ActionSymbol, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void DrawCompositionLegend(RgbaCanvas canvas, SegmentedMetricBlock card, ChartRect content, ref double y, double rowHeight, double legendBottom) {
        var theme = card.Options.Theme;
        var total = VisualBlockRendering.SegmentedTotal(card);
        for (var i = 0; i < card.Items.Count && VisualBlockRendering.CanRenderLegendRow(y, rowHeight, legendBottom); i++) {
            var segment = card.Items[i];
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment, i);
            var swatchSize = Math.Max(8, Math.Min(14, rowHeight * 0.55));
            var rowFont = Math.Max(10, Math.Min(theme.SubtitleFontSize, rowHeight * 0.58));
            canvas.FillRoundedRect(content.X, y + (rowHeight - swatchSize) / 2, swatchSize, swatchSize, Math.Min(4, swatchSize * 0.32), color);
            DrawAlignedText(canvas, segment.Label, content.X + swatchSize + 10, y + (rowHeight - rowFont) * 0.44, content.Width * 0.58, VisualTextAlignment.Left, theme.Text, rowFont, true);
            DrawAlignedText(canvas, VisualBlockRendering.SegmentedCompositionValueText(card, segment, total), content.X + content.Width * 0.66, y + (rowHeight - rowFont) * 0.44, content.Width * 0.34, VisualTextAlignment.Right, theme.Text, rowFont, true);
            y += rowHeight;
        }
    }

    private static void DrawCompositionStrip(RgbaCanvas canvas, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        foreach (var segment in VisualBlockRendering.SegmentedStackSegments(card, x, y, width, height, 4)) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment.Item, segment.Index);
            canvas.FillRoundedRect(segment.X, segment.Y, segment.Width, segment.Height, Math.Min(7, height / 2), color);
            DrawCompositionPattern(canvas, segment, height);
        }
    }

    private static void DrawCompositionPattern(RgbaCanvas canvas, SegmentedStackSegment segment, double height) {
        if (segment.Item.Pattern == ChartFillPattern.None) return;
        var radius = Math.Min(7, height / 2);
        foreach (var line in ChartPatternLineGeometry.Build(segment.Item.Pattern, segment.X, segment.Y, segment.Width, segment.Height, radius, 10)) {
            canvas.DrawLine(line.X1, line.Y1, line.X2, line.Y2, ChartColor.White.WithAlpha(46), 2);
        }
    }
}

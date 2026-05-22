using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderSegmentedMetricProgressRows(SvgMarkupWriter writer, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderSegmentedMetricHeading(writer, card, ref y, content.X, content.Width);
        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.SegmentedProgressRowsLayout(card, y, hasAction);
        writer.StartElement("g")
            .Attribute("data-cfx-role", "segmented-metric-progress-rows")
            .Attribute("data-cfx-bottom", layout.Bottom)
            .Attribute("data-cfx-row-height", layout.RowHeight)
            .EndStartElement().Line();
        for (var rowIndex = 0; rowIndex < card.Items.Count; rowIndex++) {
            var row = card.Items[rowIndex];
            var accent = VisualBlockRendering.SegmentedItemColor(theme, row, rowIndex);
            var rowLayout = VisualBlockRendering.SegmentedProgressRowLayout(card, row, content, y, layout.RowHeight, accent);
            if (!VisualBlockRendering.CanRenderProgressRow(rowLayout, layout.Bottom)) break;
            WriteText(writer, row.Label, content.X, y + theme.SubtitleFontSize, rowLayout.LabelWidth, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "600");
            if (row.Delta.Length > 0) {
                writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-delta-pill").Attribute("x", rowLayout.DeltaX).Attribute("y", y).Attribute("width", rowLayout.DeltaWidth).Attribute("height", 22).Attribute("rx", 11).Attribute("fill", rowLayout.DeltaColor.WithAlpha(34).ToCss()).EndEmptyElement().Line();
                WriteText(writer, row.Delta, rowLayout.DeltaX + 6, y + 15.5, rowLayout.DeltaWidth - 12, VisualTextAlignment.Center, rowLayout.DeltaColor, theme.FontFamily, theme.SubtitleFontSize, "800");
            }

            WriteText(writer, rowLayout.ValueText, rowLayout.ValueX, y + theme.SubtitleFontSize, rowLayout.ValueWidth, VisualTextAlignment.Right, theme.Text, theme.FontFamily, rowLayout.ValueFontSize, "850");
            RenderSegmentedStrip(writer, row, content.X, rowLayout.StripY, content.Width, rowLayout.StripHeight, accent, theme);
            y += layout.RowHeight;
        }

        writer.EndElement().Line();
        if (hasAction) {
            if (card.ActionBackground.HasValue) RenderSegmentedFooterAction(writer, card, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width);
            else RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
        }
    }

    private static void RenderSegmentedFooterAction(SvgMarkupWriter writer, SegmentedMetricBlock card, double footerY, double footerHeight, double x, double width) {
        var theme = card.Options.Theme;
        var fill = card.ActionBackground ?? theme.PlotBackground;
        var foreground = card.ActionForeground ?? theme.Text;
        var inset = Math.Min(8, Math.Max(1, footerHeight * 0.16));
        writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-action-band").Attribute("x", x).Attribute("y", footerY + 1).Attribute("width", width).Attribute("height", Math.Max(1, footerHeight - inset - 1)).Attribute("rx", Math.Min(10, Math.Max(2, footerHeight * 0.18))).Attribute("fill", fill.ToCss()).EndEmptyElement().Line();
        var fontSize = Math.Max(10, theme.SubtitleFontSize);
        var baseline = footerY + footerHeight * 0.64;
        if (card.ActionUrl.Length > 0) writer.StartElement("a").Attribute("data-cfx-role", "visual-action-link").Attribute("href", card.ActionUrl).Attribute("target", "_top").EndStartElement().Line();
        WriteText(writer, card.ActionLabel, x, baseline, Math.Max(1, width - 38), VisualTextAlignment.Left, foreground, theme.FontFamily, fontSize, "500");
        RenderActionSymbol(writer, card.ActionSymbol, x + width - 18, footerY + footerHeight * 0.5, 12, foreground, theme, fontSize);
        if (card.ActionUrl.Length > 0) writer.EndElement().Line();
    }

    private static void RenderSegmentedStrip(SvgMarkupWriter writer, SegmentedMetricItem row, double x, double y, double width, double height, ChartColor accent, ChartForgeX.Themes.ChartTheme theme) {
        var filled = VisualBlockRendering.FilledSegments(row);
        var empty = theme.CardBackground.A > 0 ? theme.CardBackground : ChartColor.White;
        var emptyStroke = theme.PlotBorder.WithAlpha(120);
        writer.StartElement("g")
            .Attribute("data-cfx-role", "segmented-metric-progress-strip")
            .Attribute("data-cfx-segments", row.Segments)
            .Attribute("data-cfx-filled", filled)
            .Attribute("data-cfx-strip-y", y)
            .Attribute("data-cfx-strip-height", height)
            .EndStartElement().Line();
        foreach (var segment in VisualBlockRendering.SegmentedProgressStripSegments(row, x, y, width, height)) {
            var role = segment.Filled ? "segmented-metric-segment-filled" : "segmented-metric-segment-empty";
            var color = segment.Filled ? accent : empty;
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "segmented-metric-segment-shadow")
                .Attribute("data-cfx-index", segment.Index)
                .Attribute("x", segment.X + 0.6)
                .Attribute("y", segment.Y + 1.2)
                .Attribute("width", segment.Width)
                .Attribute("height", segment.Height)
                .Attribute("rx", segment.Radius)
                .Attribute("fill", theme.MutedText.WithAlpha(segment.Filled ? (byte)28 : (byte)18).ToCss())
                .EndEmptyElement().Line();
            writer.StartElement("rect")
                .Attribute("data-cfx-role", role)
                .Attribute("data-cfx-index", segment.Index)
                .Attribute("x", segment.X)
                .Attribute("y", segment.Y)
                .Attribute("width", segment.Width)
                .Attribute("height", segment.Height)
                .Attribute("rx", segment.Radius)
                .Attribute("fill", color.ToCss())
                .Attribute("stroke", segment.Filled ? accent.WithAlpha(120).ToCss() : emptyStroke.ToCss())
                .Attribute("stroke-width", 0.8)
                .EndEmptyElement().Line();
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "segmented-metric-segment-highlight")
                .Attribute("data-cfx-index", segment.Index)
                .Attribute("x", segment.X + 1)
                .Attribute("y", segment.Y + 1)
                .Attribute("width", Math.Max(1, segment.Width - 2))
                .Attribute("height", Math.Max(1, segment.Height * 0.32))
                .Attribute("rx", Math.Min(3, segment.Width * 0.28))
                .Attribute("fill", ChartColor.White.WithAlpha(segment.Filled ? (byte)48 : (byte)92).ToCss())
                .EndEmptyElement().Line();
        }

        writer.EndElement().Line();
    }
}

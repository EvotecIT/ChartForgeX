using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderDistributionStripCard(SvgMarkupWriter writer, DistributionStripCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, card, ref y, content.X, content.Width);

        var hasAction = card.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.13)) : 0;
        var bottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var metricSize = Math.Min(38, Math.Max(24, options.Size.Height * 0.12));
        var captionWidth = card.Caption.Length == 0 ? 0 : Math.Min(content.Width * 0.34, VisualBlockRendering.EstimateTextWidth(card.Caption, theme.SubtitleFontSize) + 12);

        writer.StartElement("g")
            .Attribute("data-cfx-role", "distribution-strip-card")
            .Attribute("data-cfx-total", VisualBlockRendering.DistributionTotal(card))
            .EndStartElement().Line();
        WriteText(writer, card.Label, content.X, y + theme.SubtitleFontSize, content.Width - captionWidth - 10, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "650");
        if (card.Caption.Length > 0) WriteText(writer, card.Caption, content.X + content.Width - captionWidth, y + theme.SubtitleFontSize, captionWidth, VisualTextAlignment.Right, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "600");
        WriteText(writer, card.Value, content.X, y + theme.SubtitleFontSize + metricSize + 12, content.Width, VisualTextAlignment.Left, theme.Text, theme.FontFamily, metricSize, "850");
        y += theme.SubtitleFontSize + metricSize + 30;

        var stripHeight = Math.Max(14, Math.Min(24, options.Size.Height * 0.065));
        RenderDistributionStack(writer, card, content.X, y, content.Width, stripHeight);
        y += stripHeight + 12;

        var legendHeight = RenderDistributionLegend(writer, card, content.X, y, content.Width);
        y += legendHeight + 10;

        var rowCount = Math.Max(1, card.Segments.Count);
        var rowHeight = Math.Max(27, Math.Min(36, (bottom - y) / rowCount));
        for (var i = 0; i < card.Segments.Count && y + rowHeight <= bottom + 1; i++) {
            RenderDistributionRow(writer, card, card.Segments[i], i, content.X, y, content.Width, rowHeight);
            y += rowHeight;
        }

        writer.EndElement().Line();
        if (hasAction) RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - footerHeight, footerHeight, content.X, content.Width, theme);
    }

    private static void RenderDistributionStack(SvgMarkupWriter writer, DistributionStripCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var total = VisualBlockRendering.DistributionTotal(card);
        var gap = VisualBlockRendering.EffectiveStackGap(card.Segments.Count, width, 5);
        var segmentArea = Math.Max(0, width - gap * Math.Max(0, card.Segments.Count - 1));
        var cursor = x;
        writer.StartElement("g").Attribute("data-cfx-role", "distribution-strip").Attribute("data-cfx-total", total).EndStartElement().Line();
        for (var i = 0; i < card.Segments.Count; i++) {
            var segment = card.Segments[i];
            var segmentWidth = i == card.Segments.Count - 1 ? x + width - cursor : Math.Max(0, segmentArea * segment.Value / total);
            if (segmentWidth <= 0) continue;
            var color = segment.Color ?? (segment.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, segment.Status));
            var share = segment.Value / total;
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "distribution-segment")
                .Attribute("data-cfx-label", segment.Label)
                .Attribute("data-cfx-value", segment.Value)
                .Attribute("data-cfx-share", share)
                .Attribute("x", cursor)
                .Attribute("y", y)
                .Attribute("width", segmentWidth)
                .Attribute("height", height)
                .Attribute("rx", Math.Min(6, height / 2))
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();

            for (var lineX = cursor - height; lineX < cursor + segmentWidth; lineX += 12) {
                writer.StartElement("line")
                    .Attribute("data-cfx-role", "distribution-segment-sheen")
                    .Attribute("x1", lineX)
                    .Attribute("y1", y + height)
                    .Attribute("x2", lineX + height)
                    .Attribute("y2", y)
                    .Attribute("stroke", "#fff")
                    .Attribute("stroke-opacity", 0.15)
                    .Attribute("stroke-width", 2)
                    .EndEmptyElement().Line();
            }

            cursor += segmentWidth + gap;
        }

        writer.EndElement().Line();
    }

    private static double RenderDistributionLegend(SvgMarkupWriter writer, DistributionStripCard card, double x, double y, double width) {
        var theme = card.Options.Theme;
        var rowHeight = 20.0;
        var cursorX = x;
        var cursorY = y;
        var total = VisualBlockRendering.DistributionTotal(card);
        writer.StartElement("g").Attribute("data-cfx-role", "distribution-legend").EndStartElement().Line();
        for (var i = 0; i < card.Segments.Count; i++) {
            var segment = card.Segments[i];
            var color = segment.Color ?? (segment.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, segment.Status));
            var label = ShortDistributionLabel(segment.Label) + " " + (segment.Value / total * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";
            var chipWidth = Math.Min(140, Math.Max(54, VisualBlockRendering.EstimateTextWidth(label, Math.Max(9, theme.SubtitleFontSize - 2)) + 18));
            if (cursorX + chipWidth > x + width && cursorX > x) {
                cursorX = x;
                cursorY += rowHeight;
            }

            writer.StartElement("g").Attribute("data-cfx-role", "distribution-legend-chip").Attribute("data-cfx-label", segment.Label).EndStartElement().Line();
            writer.StartElement("circle").Attribute("cx", cursorX + 4).Attribute("cy", cursorY + 8).Attribute("r", 4).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, label, cursorX + 13, cursorY + 12, chipWidth - 13, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "650");
            writer.EndElement().Line();
            cursorX += chipWidth + 10;
        }

        writer.EndElement().Line();
        return cursorY - y + rowHeight;
    }

    private static void RenderDistributionRow(SvgMarkupWriter writer, DistributionStripCard card, DistributionStripSegment segment, int index, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var color = segment.Color ?? (segment.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, index) : VisualBlockRendering.StatusColor(theme, segment.Status));
        var share = segment.Value / VisualBlockRendering.DistributionTotal(card);
        var rowY = y + height * 0.5;
        var badgeSize = Math.Min(22, Math.Max(16, height - 9));
        var ringRadius = Math.Min(8.5, Math.Max(6, height * 0.24));
        var percentWidth = 58.0;
        var detailWidth = segment.Detail.Length == 0 ? 0 : Math.Min(96, VisualBlockRendering.EstimateTextWidth(segment.Detail, theme.SubtitleFontSize) + 8);
        var labelWidth = Math.Max(1, width - badgeSize - 16 - percentWidth - detailWidth - 34);

        writer.StartElement("g")
            .Attribute("data-cfx-role", "distribution-row")
            .Attribute("data-cfx-label", segment.Label)
            .Attribute("data-cfx-value", segment.Value)
            .Attribute("data-cfx-share", share)
            .EndStartElement().Line();
        writer.StartElement("rect").Attribute("data-cfx-role", "distribution-symbol-badge").Attribute("x", x).Attribute("y", rowY - badgeSize / 2).Attribute("width", badgeSize).Attribute("height", badgeSize).Attribute("rx", Math.Min(7, badgeSize * 0.32)).Attribute("fill", color.WithAlpha(34).ToCss()).Attribute("stroke", color.WithAlpha(110).ToCss()).EndEmptyElement().Line();
        WriteText(writer, segment.Symbol.Length > 0 ? segment.Symbol : ShortDistributionLabel(segment.Label), x + 2, rowY + 4, badgeSize - 4, VisualTextAlignment.Center, color, theme.FontFamily, Math.Max(8, badgeSize * 0.42), "850");
        WriteText(writer, segment.Label, x + badgeSize + 12, rowY + 4, labelWidth, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "650");
        if (segment.Detail.Length > 0) WriteText(writer, segment.Detail, x + width - detailWidth, rowY + 4, detailWidth, VisualTextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "750");
        WriteText(writer, (share * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%", x + width - detailWidth - percentWidth - 6, rowY + 4, percentWidth, VisualTextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "800");
        RenderDistributionRing(writer, x + width - detailWidth - percentWidth - 24, rowY, ringRadius, share, color, theme.PlotBorder);
        writer.EndElement().Line();
    }

    private static void RenderDistributionRing(SvgMarkupWriter writer, double cx, double cy, double radius, double ratio, ChartColor color, ChartColor trackColor) {
        writer.StartElement("circle").Attribute("data-cfx-role", "distribution-ring-track").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", radius).Attribute("fill", "none").Attribute("stroke", trackColor.ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
        if (ratio <= 0) return;
        var start = -Math.PI / 2;
        var end = start + Math.PI * 2 * Math.Max(0.002, Math.Min(1, ratio));
        writer.StartElement("path")
            .Attribute("data-cfx-role", "distribution-ring")
            .Attribute("d", ArcPath(cx, cy, radius, start, end))
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", 2.4)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement().Line();
    }

    private static string ShortDistributionLabel(string value) {
        var start = value.LastIndexOf('(');
        var end = value.LastIndexOf(')');
        if (start >= 0 && end > start + 1) return value.Substring(start + 1, end - start - 1);
        var trimmed = value.Trim();
        return trimmed.Length <= 4 ? trimmed : trimmed.Substring(0, Math.Min(3, trimmed.Length)).ToUpperInvariant();
    }
}

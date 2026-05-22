using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderSegmentedMetricCapsuleSplit(SvgMarkupWriter writer, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderSegmentedMetricHeading(writer, card, ref y, content.X, content.Width);
        y += 8;

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.CapsuleSplitLayout(options, content, y, hasAction);

        writer.StartElement("g")
            .Attribute("data-cfx-role", "segmented-metric-capsule-split")
            .Attribute("data-cfx-total", VisualBlockRendering.SegmentedTotal(card))
            .Attribute("data-cfx-strip-height", layout.RingHeight)
            .EndStartElement().Line();
        RenderCapsuleTrack(writer, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight);
        RenderCapsuleSegments(writer, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight);
        writer.EndElement().Line();

        RenderCapsuleLegend(writer, card, layout.LegendX, layout.LegendY, layout.LegendWidth, layout.LegendHeight);
        if (hasAction) RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void RenderCapsuleTrack(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        writer.StartElement("rect")
            .Attribute("data-cfx-role", "segmented-metric-capsule-track-shadow")
            .Attribute("x", x)
            .Attribute("y", y + 1.6)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", height / 2)
            .Attribute("fill", theme.MutedText.WithAlpha(14).ToCss())
            .EndEmptyElement().Line();
        writer.StartElement("rect")
            .Attribute("data-cfx-role", "segmented-metric-capsule-track")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", height / 2)
            .Attribute("fill", theme.CardBackground.WithAlpha(160).ToCss())
            .EndEmptyElement().Line();
    }

    private static void RenderCapsuleSegments(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var segments = VisualBlockRendering.SegmentedStackSegments(card, x, y, width, height, 3);
        var radius = height / 2;
        for (var i = 0; i < segments.Count; i++) {
            var segment = segments[i];
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment.Item, segment.Index);
            if (segment.Width <= 0) continue;
            var rounded = i == 0 || i == segments.Count - 1;
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "segmented-metric-capsule-segment")
                .Attribute("data-cfx-label", segment.Item.Label)
                .Attribute("data-cfx-value", segment.Item.Value)
                .Attribute("data-cfx-share", segment.Share)
                .Attribute("data-cfx-x", segment.X)
                .Attribute("data-cfx-width", segment.Width)
                .Attribute("x", segment.X)
                .Attribute("y", segment.Y)
                .Attribute("width", segment.Width)
                .Attribute("height", segment.Height)
                .Attribute("rx", rounded ? Math.Min(radius, segment.Width / 2) : 0)
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();
            if (rounded && segment.Width > radius) {
                var squareX = i == 0 ? segment.X + radius : segment.X;
                writer.StartElement("rect")
                    .Attribute("data-cfx-role", "segmented-metric-capsule-segment-square")
                    .Attribute("x", squareX)
                    .Attribute("y", segment.Y)
                    .Attribute("width", Math.Max(0, segment.Width - radius))
                    .Attribute("height", segment.Height)
                    .Attribute("fill", color.ToCss())
                    .EndEmptyElement().Line();
            }

            if (segment.Share >= 0.075) RenderCapsuleSegmentLabel(writer, card, segment, color);
        }
    }

    private static void RenderCapsuleSegmentLabel(SvgMarkupWriter writer, SegmentedMetricBlock card, SegmentedStackSegment segment, ChartColor color) {
        var theme = card.Options.Theme;
        var label = (segment.Share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        var fontSize = Math.Max(9, Math.Min(12, segment.Height * 0.48));
        var maxWidth = Math.Max(26, segment.Width - 10);
        var foreground = VisualBlockRendering.CapsuleSplitLabelForeground(color);
        writer.StartElement("text")
            .Attribute("data-cfx-role", "segmented-metric-capsule-label")
            .Attribute("x", segment.X + segment.Width / 2)
            .Attribute("y", segment.Y + segment.Height / 2 + fontSize * 0.34)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", foreground.ToCss())
            .Attribute("stroke", VisualBlockRendering.CapsuleSplitLabelHalo(foreground).ToCss())
            .Attribute("stroke-width", 2.4)
            .Attribute("paint-order", "stroke")
            .Attribute("font-family", theme.FontFamily)
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "850")
            .Text(VisualBlockRendering.FitText(label, fontSize, maxWidth))
            .EndElement().Line();
    }

    private static void RenderCapsuleLegend(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var rowHeight = Math.Max(26, Math.Min(34, height / Math.Max(1, card.Items.Count)));
        var total = VisualBlockRendering.SegmentedTotal(card);
        var bottom = y + height;
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-capsule-legend").EndStartElement().Line();
        for (var i = 0; i < card.Items.Count && y + rowHeight <= bottom + 1; i++) {
            var item = card.Items[i];
            var color = VisualBlockRendering.SegmentedItemColor(theme, item, i);
            var value = VisualBlockRendering.SegmentedCapsuleLegendValue(card, item, total);
            writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-capsule-swatch").Attribute("x", x).Attribute("y", y + rowHeight * 0.26).Attribute("width", 12).Attribute("height", 12).Attribute("rx", 3).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, item.Label, x + 22, y + rowHeight * 0.68, width * 0.56, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "550");
            WriteText(writer, value, x + width * 0.60, y + rowHeight * 0.68, width * 0.40, VisualTextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "800");
            y += rowHeight;
        }

        writer.EndElement().Line();
    }
}

using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderDistributionRows(SvgMarkupWriter writer, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderSegmentedMetricHeading(writer, card, ref y, content.X, content.Width);

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.SegmentedDistributionLayout(card, content, y, hasAction);

        writer.StartElement("g")
            .Attribute("data-cfx-role", "segmented-metric-distribution")
            .Attribute("data-cfx-total", VisualBlockRendering.SegmentedTotal(card))
            .EndStartElement().Line();
        WriteText(writer, card.Label, content.X, y + theme.SubtitleFontSize, content.Width - layout.CaptionWidth - 10, TextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "650");
        if (card.Caption.Length > 0) WriteText(writer, card.Caption, content.X + content.Width - layout.CaptionWidth, y + theme.SubtitleFontSize, layout.CaptionWidth, TextAlignment.Right, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "600");
        WriteText(writer, card.Value, content.X, y + theme.SubtitleFontSize + layout.MetricSize + 12, content.Width, TextAlignment.Left, theme.Text, theme.FontFamily, layout.MetricSize, "850");
        RenderDistributionStack(writer, card, content.X, layout.StripY, content.Width, layout.StripHeight);
        RenderDistributionLegend(writer, card, layout);
        y = layout.RowsY;
        for (var i = 0; i < card.Items.Count && y + layout.RowHeight <= layout.Bottom + 1; i++) {
            RenderDistributionRow(writer, card, card.Items[i], i, content.X, y, content.Width, layout.RowHeight);
            y += layout.RowHeight;
        }

        writer.EndElement().Line();
        if (hasAction) RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void RenderDistributionStack(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var total = VisualBlockRendering.SegmentedTotal(card);
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-distribution-strip").Attribute("data-cfx-total", total).EndStartElement().Line();
        foreach (var segment in VisualBlockRendering.SegmentedStackSegments(card, x, y, width, height, 5)) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment.Item, segment.Index);
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "segmented-metric-distribution-segment")
                .Attribute("data-cfx-label", segment.Item.Label)
                .Attribute("data-cfx-value", segment.Item.Value)
                .Attribute("data-cfx-share", segment.Share)
                .Attribute("x", segment.X)
                .Attribute("y", segment.Y)
                .Attribute("width", segment.Width)
                .Attribute("height", segment.Height)
                .Attribute("rx", Math.Min(6, height / 2))
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();

            var radius = Math.Min(6, height / 2);
            foreach (var line in ChartPatternLineGeometry.Build(ChartFillPattern.DiagonalForward, segment.X, segment.Y, segment.Width, segment.Height, radius, 12)) {
                writer.StartElement("line")
                    .Attribute("data-cfx-role", "segmented-metric-distribution-sheen")
                    .Attribute("x1", line.X1)
                    .Attribute("y1", line.Y1)
                    .Attribute("x2", line.X2)
                    .Attribute("y2", line.Y2)
                    .Attribute("stroke", "#fff")
                    .Attribute("stroke-opacity", 0.15)
                    .Attribute("stroke-width", 2)
                    .Attribute("stroke-linecap", "round")
                    .EndEmptyElement().Line();
            }
        }

        writer.EndElement().Line();
    }

    private static void RenderDistributionLegend(SvgMarkupWriter writer, SegmentedMetricBlock card, SegmentedDistributionLayout layout) {
        var theme = card.Options.Theme;
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-distribution-legend").EndStartElement().Line();
        foreach (var chip in layout.LegendChips) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, chip.Item, chip.Index);
            writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-distribution-chip").Attribute("data-cfx-label", chip.Item.Label).EndStartElement().Line();
            writer.StartElement("circle").Attribute("cx", chip.X + 4).Attribute("cy", chip.Y + 8).Attribute("r", 4).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, chip.Label, chip.X + 13, chip.Y + 12, chip.Width - 13, TextAlignment.Left, theme.Text, theme.FontFamily, chip.FontSize, "650");
            writer.EndElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void RenderDistributionRow(SvgMarkupWriter writer, SegmentedMetricBlock card, SegmentedMetricItem segment, int index, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var color = VisualBlockRendering.SegmentedItemColor(theme, segment, index);
        var layout = VisualBlockRendering.SegmentedDistributionRowLayout(card, segment, x, y, width, height);

        writer.StartElement("g")
            .Attribute("data-cfx-role", "segmented-metric-distribution-row")
            .Attribute("data-cfx-label", segment.Label)
            .Attribute("data-cfx-value", segment.Value)
            .Attribute("data-cfx-share", layout.Share)
            .EndStartElement().Line();
        writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-distribution-symbol-badge").Attribute("x", x).Attribute("y", layout.Y - layout.BadgeSize / 2).Attribute("width", layout.BadgeSize).Attribute("height", layout.BadgeSize).Attribute("rx", Math.Min(7, layout.BadgeSize * 0.32)).Attribute("fill", color.WithAlpha(34).ToCss()).Attribute("stroke", color.WithAlpha(110).ToCss()).EndEmptyElement().Line();
        WriteText(writer, layout.SymbolText, x + 2, layout.Y + 4, layout.BadgeSize - 4, TextAlignment.Center, color, theme.FontFamily, Math.Max(8, layout.BadgeSize * 0.42), "850");
        WriteText(writer, segment.Label, x + layout.BadgeSize + 12, layout.Y + 4, layout.LabelWidth, TextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "650");
        if (segment.DisplayValue.Length > 0) WriteText(writer, segment.DisplayValue, x + width - layout.DisplayValueWidth, layout.Y + 4, layout.DisplayValueWidth, TextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "750");
        WriteText(writer, layout.PercentText, x + width - layout.DisplayValueWidth - layout.PercentWidth - 6, layout.Y + 4, layout.PercentWidth, TextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "800");
        RenderDistributionRing(writer, x + width - layout.DisplayValueWidth - layout.PercentWidth - 24, layout.Y, layout.RingRadius, layout.Share, color, theme.PlotBorder);
        writer.EndElement().Line();
    }

    private static void RenderDistributionRing(SvgMarkupWriter writer, double cx, double cy, double radius, double ratio, ChartColor color, ChartColor trackColor) {
        writer.StartElement("circle").Attribute("data-cfx-role", "segmented-metric-ring-track").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", radius).Attribute("fill", "none").Attribute("stroke", trackColor.ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
        if (ratio <= 0) return;
        var start = -Math.PI / 2;
        var end = start + Math.PI * 2 * Math.Max(0.002, Math.Min(1, ratio));
        writer.StartElement("path")
            .Attribute("data-cfx-role", "segmented-metric-ring")
            .Attribute("d", ArcPath(cx, cy, radius, start, end))
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", 2.4)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement().Line();
    }
}

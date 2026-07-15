using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderSegmentedMetricComposition(SvgMarkupWriter writer, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderSegmentedMetricHeading(writer, card, ref y, content.X, content.Width);
        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.SegmentedCompositionLayout(card, content, y, hasAction);
        WriteText(writer, card.Label, content.X, y + theme.SubtitleFontSize, content.Width * 0.58, TextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "600");
        WriteText(writer, card.Value, content.X + content.Width * 0.58, y + layout.MetricSize * 0.82, content.Width * 0.42, TextAlignment.Right, theme.Text, theme.FontFamily, layout.MetricSize, "850");
        y = layout.StripY;
        RenderCompositionStrip(writer, card, content.X, y, content.Width, layout.StripHeight);
        y = layout.LegendY;
        RenderCompositionLegend(writer, card, content, ref y, layout.RowHeight, layout.LegendBottom);
        if (hasAction) RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void RenderCompositionLegend(SvgMarkupWriter writer, SegmentedMetricBlock card, ChartRect content, ref double y, double rowHeight, double legendBottom) {
        var theme = card.Options.Theme;
        var total = VisualBlockRendering.SegmentedTotal(card);
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-composition").Attribute("data-cfx-legend-y", y).Attribute("data-cfx-row-height", rowHeight).Attribute("data-cfx-legend-bottom", legendBottom).EndStartElement().Line();
        for (var i = 0; i < card.Items.Count && VisualBlockRendering.CanRenderLegendRow(y, rowHeight, legendBottom); i++) {
            var segment = card.Items[i];
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment, i);
            var swatchSize = Math.Max(8, Math.Min(14, rowHeight * 0.55));
            var rowFont = Math.Max(10, Math.Min(theme.SubtitleFontSize, rowHeight * 0.58));
            writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-legend-swatch").Attribute("x", content.X).Attribute("y", y + (rowHeight - swatchSize) / 2).Attribute("width", swatchSize).Attribute("height", swatchSize).Attribute("rx", Math.Min(4, swatchSize * 0.32)).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, segment.Label, content.X + swatchSize + 10, y + rowHeight * 0.66, content.Width * 0.58, TextAlignment.Left, theme.Text, theme.FontFamily, rowFont, "500");
            WriteText(writer, VisualBlockRendering.SegmentedCompositionValueText(card, segment, total), content.X + content.Width * 0.66, y + rowHeight * 0.66, content.Width * 0.34, TextAlignment.Right, theme.Text, theme.FontFamily, rowFont, "750");
            y += rowHeight;
        }

        writer.EndElement().Line();
    }

    private static void RenderCompositionStrip(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var total = VisualBlockRendering.SegmentedTotal(card);
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-composition-strip").Attribute("data-cfx-total", total).EndStartElement().Line();
        foreach (var segment in VisualBlockRendering.SegmentedStackSegments(card, x, y, width, height, 4)) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, segment.Item, segment.Index);
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "segmented-metric-composition-segment")
                .Attribute("data-cfx-label", segment.Item.Label)
                .Attribute("data-cfx-value", segment.Item.Value)
                .Attribute("data-cfx-pattern", segment.Item.Pattern.ToString())
                .Attribute("data-cfx-share", segment.Share)
                .Attribute("x", segment.X)
                .Attribute("y", segment.Y)
                .Attribute("width", segment.Width)
                .Attribute("height", segment.Height)
                .Attribute("rx", Math.Min(7, height / 2))
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();
            RenderCompositionPattern(writer, segment, height);
        }

        writer.EndElement().Line();
    }

    private static void RenderCompositionPattern(SvgMarkupWriter writer, SegmentedStackSegment segment, double height) {
        if (segment.Item.Pattern == ChartFillPattern.None) return;
        var radius = Math.Min(7, height / 2);
        foreach (var line in ChartPatternLineGeometry.Build(segment.Item.Pattern, segment.X, segment.Y, segment.Width, segment.Height, radius, 10)) {
            writer.StartElement("line")
                .Attribute("data-cfx-role", "segmented-metric-composition-pattern")
                .Attribute("data-cfx-pattern", segment.Item.Pattern.ToString())
                .Attribute("x1", line.X1)
                .Attribute("y1", line.Y1)
                .Attribute("x2", line.X2)
                .Attribute("y2", line.Y2)
                .Attribute("stroke", "#fff")
                .Attribute("stroke-opacity", 0.18)
                .Attribute("stroke-width", 2)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement().Line();
        }
    }
}

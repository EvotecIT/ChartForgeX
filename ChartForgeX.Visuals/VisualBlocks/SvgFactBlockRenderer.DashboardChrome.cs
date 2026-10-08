using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

using static ChartForgeX.VisualBlocks.SvgVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    private static void RenderSegmentedMetricHeading(SvgMarkupWriter writer, SegmentedMetricBlock card, ref double y, double x, double width) {
        if (card.HeaderSymbol.Length > 0) RenderSegmentedMetricHeader(writer, card, ref y, x, width);
        else RenderBlockHeading(writer, card, ref y, x, width);
    }

    private static void RenderSegmentedMetricHeader(SvgMarkupWriter writer, SegmentedMetricBlock card, ref double y, double x, double width) {
        var theme = card.Options.Theme;
        var layout = VisualFactBlockRendering.SegmentedHeaderLayout(card, x, y, width);
        if (layout.BadgeSize > 0) {
            writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-header-badge").Attribute("x", x).Attribute("y", y).Attribute("width", layout.BadgeSize).Attribute("height", layout.BadgeSize).Attribute("rx", 14).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            WriteText(writer, card.HeaderSymbol, x, y + 31, layout.BadgeSize, TextAlignment.Center, theme.Text, theme.FontFamily, 18, "850");
        }


        if (card.Title.Length > 0) WriteText(writer, card.Title, layout.TextX, layout.TitleTop + theme.TitleFontSize * 0.75, layout.TextWidth, TextAlignment.Left, theme.Text, theme.FontFamily, theme.TitleFontSize, "800");
        if (card.Subtitle.Length > 0) WriteText(writer, card.Subtitle, layout.TextX, layout.SubtitleTop + theme.SubtitleFontSize * 0.75, layout.TextWidth, TextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "500");
        writer.StartElement("line").Attribute("data-cfx-role", "segmented-metric-header-divider").Attribute("x1", x).Attribute("y1", layout.DividerY).Attribute("x2", x + width).Attribute("y2", layout.DividerY).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        y = layout.NextY;
    }

}

using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderSegmentedMetricHeading(SvgMarkupWriter writer, SegmentedMetricBlock card, ref double y, double x, double width) {
        if (card.HeaderSymbol.Length > 0 || card.ShowMenu) RenderSegmentedMetricHeader(writer, card, ref y, x, width);
        else RenderBlockHeading(writer, card, ref y, x, width);
    }

    private static void RenderSegmentedMetricHeader(SvgMarkupWriter writer, SegmentedMetricBlock card, ref double y, double x, double width) {
        var theme = card.Options.Theme;
        var layout = VisualBlockRendering.SegmentedHeaderLayout(card, x, y, width);
        if (layout.BadgeSize > 0) {
            writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-header-badge").Attribute("x", x).Attribute("y", y).Attribute("width", layout.BadgeSize).Attribute("height", layout.BadgeSize).Attribute("rx", 14).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            WriteText(writer, card.HeaderSymbol, x, y + 31, layout.BadgeSize, VisualTextAlignment.Center, theme.Text, theme.FontFamily, 18, "850");
        }

        if (card.ShowMenu) {
            for (var i = 0; i < 3; i++) writer.StartElement("circle").Attribute("data-cfx-role", "segmented-metric-menu-dot").Attribute("cx", layout.MenuDotStartX + i * 7).Attribute("cy", layout.MenuDotY).Attribute("r", 2.1).Attribute("fill", theme.MutedText.ToCss()).EndEmptyElement().Line();
        }

        if (card.Title.Length > 0) WriteText(writer, card.Title, layout.TextX, layout.TitleTop + theme.TitleFontSize * 0.75, layout.TextWidth, VisualTextAlignment.Left, theme.Text, theme.FontFamily, theme.TitleFontSize, "800");
        if (card.Subtitle.Length > 0) WriteText(writer, card.Subtitle, layout.TextX, layout.SubtitleTop + theme.SubtitleFontSize * 0.75, layout.TextWidth, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "500");
        writer.StartElement("line").Attribute("data-cfx-role", "segmented-metric-header-divider").Attribute("x1", x).Attribute("y1", layout.DividerY).Attribute("x2", x + width).Attribute("y2", layout.DividerY).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        y = layout.NextY;
    }

}

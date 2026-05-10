using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderSegmentedProgressHeader(SvgMarkupWriter writer, SegmentedProgressCard card, ref double y, double x, double width) {
        var theme = card.Options.Theme;
        var badgeSize = card.HeaderSymbol.Length > 0 ? 48.0 : 0.0;
        var textX = x + (badgeSize > 0 ? badgeSize + 18 : 0);
        var menuReserve = card.ShowMenu ? 42.0 : 0.0;
        if (badgeSize > 0) {
            writer.StartElement("rect").Attribute("data-cfx-role", "segmented-progress-header-badge").Attribute("x", x).Attribute("y", y).Attribute("width", badgeSize).Attribute("height", badgeSize).Attribute("rx", 14).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            WriteText(writer, card.HeaderSymbol, x, y + 31, badgeSize, VisualTextAlignment.Center, theme.Text, theme.FontFamily, 18, "850");
        }

        if (card.ShowMenu) {
            var dotY = y + 22;
            for (var i = 0; i < 3; i++) writer.StartElement("circle").Attribute("data-cfx-role", "segmented-progress-menu-dot").Attribute("cx", x + width - 22 + i * 7).Attribute("cy", dotY).Attribute("r", 2.1).Attribute("fill", theme.MutedText.ToCss()).EndEmptyElement().Line();
        }

        if (card.Title.Length > 0) WriteText(writer, card.Title, textX, y + theme.TitleFontSize * 0.75, width - (textX - x) - menuReserve, VisualTextAlignment.Left, theme.Text, theme.FontFamily, theme.TitleFontSize, "800");
        if (card.Subtitle.Length > 0) WriteText(writer, card.Subtitle, textX, y + theme.TitleFontSize + 8 + theme.SubtitleFontSize * 0.75, width - (textX - x) - menuReserve, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "500");
        y += Math.Max(badgeSize, card.Title.Length > 0 ? theme.TitleFontSize + (card.Subtitle.Length > 0 ? theme.SubtitleFontSize + 13 : 8) : 0) + 18;
        writer.StartElement("line").Attribute("data-cfx-role", "segmented-progress-header-divider").Attribute("x1", x).Attribute("y1", y).Attribute("x2", x + width).Attribute("y2", y).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        y += 24;
    }

}

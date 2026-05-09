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

    private static void RenderActivityChrome(SvgMarkupWriter writer, ActivityTimelineBlock block, ref double y, double x, double width) {
        var theme = block.Options.Theme;
        if (block.ToolbarActions.Count > 0) {
            var cursor = x + width;
            for (var i = block.ToolbarActions.Count - 1; i >= 0; i--) {
                var action = block.ToolbarActions[i];
                var actionWidth = Math.Min(92, VisualBlockRendering.EstimateTextWidth(action, theme.SubtitleFontSize) + 24);
                cursor -= actionWidth;
                writer.StartElement("rect").Attribute("data-cfx-role", "activity-toolbar-action").Attribute("x", cursor).Attribute("y", y).Attribute("width", actionWidth).Attribute("height", 28).Attribute("rx", 8).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
                WriteText(writer, action, cursor, y + 18, actionWidth, VisualTextAlignment.Center, theme.Text, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "700");
                cursor -= 8;
            }

            y += 38;
        }

        if (block.Tabs.Count > 0) {
            var cursor = x;
            foreach (var tab in block.Tabs) {
                var labelWidth = VisualBlockRendering.EstimateTextWidth(tab.Label, Math.Max(12, theme.SubtitleFontSize + 1));
                var detailWidth = tab.Detail.Length == 0 ? 0 : VisualBlockRendering.EstimateTextWidth(tab.Detail, Math.Max(10, theme.SubtitleFontSize - 1));
                var tabWidth = Math.Min(210, Math.Max(96, Math.Max(labelWidth, detailWidth) + 32));
                writer.StartElement("rect").Attribute("data-cfx-role", "activity-tab").Attribute("x", cursor).Attribute("y", y).Attribute("width", tabWidth).Attribute("height", 52).Attribute("rx", 8).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
                WriteText(writer, tab.Label, cursor + 14, y + 19, tabWidth - 26, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(12, theme.SubtitleFontSize + 1), "750");
                if (tab.Detail.Length > 0) WriteText(writer, tab.Detail, cursor + 14, y + 39, tabWidth - 26, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "500");
                cursor += tabWidth + 10;
            }

            y += 66;
        }

        if (block.NotePlaceholder.Length > 0) {
            writer.StartElement("rect").Attribute("data-cfx-role", "activity-note-box").Attribute("x", x).Attribute("y", y).Attribute("width", width).Attribute("height", 54).Attribute("rx", 8).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            WriteText(writer, block.NotePlaceholder, x + 14, y + 32, width - 58, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(12, theme.SubtitleFontSize + 1), "500");
            var sendColor = VisualBlockRendering.PaletteAt(theme, 0);
            writer.StartElement("rect").Attribute("data-cfx-role", "activity-note-send").Attribute("x", x + width - 38).Attribute("y", y + 17).Attribute("width", 22).Attribute("height", 22).Attribute("rx", 7).Attribute("fill", sendColor.WithAlpha(72).ToCss()).EndEmptyElement().Line();
            WriteText(writer, ">", x + width - 37, y + 32, 20, VisualTextAlignment.Center, sendColor, theme.FontFamily, 12, "850");
            y += 76;
        }
    }
}

using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderDateStrip(SvgMarkupWriter writer, DateStripBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var headerHeight = block.Header.Length > 0 || block.ShowNavigation ? 36.0 : 0;
        var navReserve = block.ShowNavigation ? 72.0 : 0;
        if (block.Header.Length > 0 || block.ShowNavigation) {
            writer.StartElement("g").Attribute("data-cfx-role", "date-strip-header").EndStartElement().Line();
            if (block.Header.Length > 0) {
                writer.StartElement("rect").Attribute("data-cfx-role", "date-strip-calendar-badge").Attribute("x", content.X).Attribute("y", y + 3).Attribute("width", 22).Attribute("height", 22).Attribute("rx", 6).Attribute("fill", theme.Text.WithAlpha(24).ToCss()).Attribute("stroke", theme.Text.WithAlpha(85).ToCss()).EndEmptyElement().Line();
                writer.StartElement("line").Attribute("x1", content.X + 5).Attribute("y1", y + 9).Attribute("x2", content.X + 17).Attribute("y2", y + 9).Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
                writer.StartElement("circle").Attribute("cx", content.X + 8).Attribute("cy", y + 15).Attribute("r", 1.5).Attribute("fill", theme.Text.ToCss()).EndEmptyElement().Line();
                writer.StartElement("circle").Attribute("cx", content.X + 14).Attribute("cy", y + 15).Attribute("r", 1.5).Attribute("fill", theme.Text.ToCss()).EndEmptyElement().Line();
                WriteText(writer, block.Header, content.X + 32, y + 22, Math.Max(1, content.Width - 32 - navReserve), VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(13, theme.SubtitleFontSize + 1), "750");
            }

            if (block.ShowNavigation) {
                var navY = y + 2;
                RenderDateNavButton(writer, block.PreviousSymbol, content.X + content.Width - 66, navY, 28, theme, muted: true);
                RenderDateNavButton(writer, block.NextSymbol, content.X + content.Width - 30, navY, 28, theme, muted: false);
            }

            writer.EndElement().Line();
            y += headerHeight;
        }

        var stripHeight = Math.Max(50, options.Size.Height - options.Padding.Bottom - y);
        writer.StartElement("g").Attribute("data-cfx-role", "date-strip-block").EndStartElement().Line();
        writer.StartElement("rect").Attribute("data-cfx-role", "date-strip-surface").Attribute("x", content.X).Attribute("y", y).Attribute("width", content.Width).Attribute("height", stripHeight).Attribute("rx", Math.Min(18, Math.Max(8, theme.PlotCornerRadius + 6))).Attribute("fill", theme.PlotBackground.WithAlpha(150).ToCss()).Attribute("stroke", theme.PlotBorder.WithAlpha(130).ToCss()).EndEmptyElement().Line();
        var innerX = content.X + 12;
        var innerY = y + 10;
        var innerWidth = Math.Max(1, content.Width - 24);
        var innerHeight = Math.Max(1, stripHeight - 20);
        var cellWidth = innerWidth / block.Items.Count;
        var pillWidth = Math.Min(54, Math.Max(42, cellWidth * 0.68));
        for (var i = 0; i < block.Items.Count; i++) {
            var item = block.Items[i];
            var cellX = innerX + i * cellWidth;
            var x = cellX + (cellWidth - pillWidth) / 2;
            var accent = item.Color ?? VisualBlockRendering.PaletteAt(theme, 0);
            var textColor = theme.Text;
            var valueTextColor = item.Selected ? ChartColorMath.TextOnBackground(accent) : theme.Text;
            var itemRadius = Math.Min(24, pillWidth * 0.48);
            writer.StartElement("g").Attribute("data-cfx-role", "date-strip-item").Attribute("data-cfx-selected", item.Selected ? "true" : "false").EndStartElement().Line();
            writer.StartElement("rect").Attribute("x", x).Attribute("y", innerY).Attribute("width", pillWidth).Attribute("height", innerHeight).Attribute("rx", itemRadius).Attribute("fill", (item.Selected ? theme.CardBackground.WithAlpha(230) : theme.CardBackground.WithAlpha(160)).ToCss()).Attribute("stroke", (item.Selected ? theme.CardBorder.WithAlpha(95) : theme.CardBorder.WithAlpha(70)).ToCss()).EndEmptyElement().Line();
            WriteText(writer, item.Label, x, innerY + 18, pillWidth, VisualTextAlignment.Center, item.Selected ? textColor : theme.MutedText, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "800");
            writer.StartElement("circle").Attribute("data-cfx-role", "date-strip-value-badge").Attribute("cx", x + pillWidth / 2).Attribute("cy", innerY + innerHeight - 18).Attribute("r", 17).Attribute("fill", item.Selected ? accent.ToCss() : theme.Background.WithAlpha(170).ToCss()).Attribute("stroke", item.Selected ? ChartColor.White.WithAlpha(130).ToCss() : theme.CardBorder.WithAlpha(70).ToCss()).EndEmptyElement().Line();
            WriteText(writer, item.Value, x, innerY + innerHeight - 13, pillWidth, VisualTextAlignment.Center, valueTextColor, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "800");
            writer.EndElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void RenderDateNavButton(SvgMarkupWriter writer, string symbol, double x, double y, double size, ChartForgeX.Themes.ChartTheme theme, bool muted) {
        writer.StartElement("g").Attribute("data-cfx-role", "date-strip-nav").EndStartElement().Line();
        writer.StartElement("circle").Attribute("cx", x + size / 2).Attribute("cy", y + size / 2).Attribute("r", size / 2).Attribute("fill", muted ? ChartColor.White.WithAlpha(150).ToCss() : ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.WithAlpha(90).ToCss()).EndEmptyElement().Line();
        WriteText(writer, symbol, x, y + size * 0.66, size, VisualTextAlignment.Center, muted ? theme.MutedText : theme.Text, theme.FontFamily, 15, "900");
        writer.EndElement().Line();
    }

    private static void RenderEntityStrip(SvgMarkupWriter writer, EntityStripBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        var actionWidth = block.ActionLabel.Length == 0 ? 0 : Math.Min(120, VisualBlockRendering.EstimateTextWidth(block.ActionSymbol + " " + block.ActionLabel, theme.SubtitleFontSize + 1) + 12);
        if (block.Title.Length > 0 || block.ActionLabel.Length > 0) {
            var titleSize = Math.Max(14, Math.Min(theme.TitleFontSize, theme.SubtitleFontSize + 8));
            if (block.Title.Length > 0) WriteText(writer, block.Title, content.X, y + titleSize * 0.75, Math.Max(1, content.Width - actionWidth - 10), VisualTextAlignment.Left, theme.Text, theme.FontFamily, titleSize, "800");
            if (block.ActionLabel.Length > 0) {
                if (block.ActionUrl.Length > 0) writer.StartElement("a").Attribute("data-cfx-role", "entity-strip-action-link").Attribute("href", block.ActionUrl).Attribute("target", "_top").EndStartElement().Line();
                WriteText(writer, block.ActionSymbol + " " + block.ActionLabel, content.X + content.Width - actionWidth, y + titleSize * 0.75, actionWidth, VisualTextAlignment.Right, VisualBlockRendering.PaletteAt(theme, 0), theme.FontFamily, Math.Max(12, theme.SubtitleFontSize + 1), "750");
                if (block.ActionUrl.Length > 0) writer.EndElement().Line();
            }

            y += titleSize + 14;
        }

        if (block.Subtitle.Length > 0) {
            WriteText(writer, block.Subtitle, content.X, y + theme.SubtitleFontSize * 0.75, content.Width, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "500");
            y += theme.SubtitleFontSize + 12;
        }

        var stripHeight = Math.Max(56, options.Size.Height - options.Padding.Bottom - y);
        writer.StartElement("g").Attribute("data-cfx-role", "entity-strip-block").EndStartElement().Line();
        writer.StartElement("rect").Attribute("data-cfx-role", "entity-strip-surface").Attribute("x", content.X).Attribute("y", y).Attribute("width", content.Width).Attribute("height", stripHeight).Attribute("rx", Math.Min(18, Math.Max(8, theme.PlotCornerRadius + 6))).Attribute("fill", theme.PlotBackground.WithAlpha(150).ToCss()).Attribute("stroke", theme.PlotBorder.WithAlpha(130).ToCss()).EndEmptyElement().Line();
        var cellWidth = Math.Max(1, (content.Width - 20) / block.Items.Count);
        var avatarRadius = Math.Min(22, Math.Max(16, cellWidth * 0.18));
        var startX = content.X + 10;
        for (var i = 0; i < block.Items.Count; i++) {
            var item = block.Items[i];
            var x = startX + i * cellWidth;
            var centerX = x + cellWidth / 2;
            var color = item.Color ?? (item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, item.Status));
            writer.StartElement("g").Attribute("data-cfx-role", "entity-strip-item").Attribute("data-cfx-label", item.Label).EndStartElement().Line();
            writer.StartElement("circle").Attribute("data-cfx-role", "entity-strip-avatar").Attribute("cx", centerX).Attribute("cy", y + 28).Attribute("r", avatarRadius).Attribute("fill", color.WithAlpha(45).ToCss()).Attribute("stroke", color.WithAlpha(115).ToCss()).EndEmptyElement().Line();
            if (item.AvatarText.Length > 0) WriteText(writer, item.AvatarText, centerX - avatarRadius, y + 33, avatarRadius * 2, VisualTextAlignment.Center, color, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "850");
            else WriteIcon(writer, VisualIcon.Person, centerX, y + 28, avatarRadius * 0.56, color);
            WriteText(writer, item.Label, x, y + stripHeight - 11, cellWidth, VisualTextAlignment.Center, theme.Text, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize), "500");
            writer.EndElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void RenderSectionHeader(SvgMarkupWriter writer, SectionHeaderBlock block) {
        var theme = block.Options.Theme;
        var content = VisualBlockRendering.ContentRect(block.Options);
        var titleSize = Math.Max(16, Math.Min(theme.TitleFontSize, theme.SubtitleFontSize + 10));
        var actionText = block.ActionLabel.Length == 0 ? string.Empty : (block.ActionSymbol.Length == 0 ? block.ActionLabel : block.ActionSymbol + " " + block.ActionLabel);
        var actionWidth = actionText.Length == 0 ? 0 : Math.Min(150, VisualBlockRendering.EstimateTextWidth(actionText, Math.Max(12, theme.SubtitleFontSize + 1)) + 12);
        var baseline = content.Y + content.Height * 0.60;
        writer.StartElement("text").Attribute("data-cfx-role", "section-header-title").Attribute("x", content.X).Attribute("y", baseline).Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", titleSize).Attribute("font-weight", "800").Text(VisualBlockRendering.FitText(block.Title, titleSize, Math.Max(1, content.Width - actionWidth - 12))).EndElement().Line();
        if (actionText.Length > 0) {
            if (block.ActionUrl.Length > 0) writer.StartElement("a").Attribute("data-cfx-role", "section-header-action-link").Attribute("href", block.ActionUrl).Attribute("target", "_top").EndStartElement().Line();
            writer.StartElement("text").Attribute("data-cfx-role", "section-header-action").Attribute("x", content.X + content.Width).Attribute("y", baseline).Attribute("text-anchor", "end").Attribute("fill", VisualBlockRendering.PaletteAt(theme, 0).ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", Math.Max(12, theme.SubtitleFontSize + 1)).Attribute("font-weight", "750").Text(VisualBlockRendering.FitText(actionText, Math.Max(12, theme.SubtitleFontSize + 1), actionWidth)).EndElement().Line();
            if (block.ActionUrl.Length > 0) writer.EndElement().Line();
        }
    }
}

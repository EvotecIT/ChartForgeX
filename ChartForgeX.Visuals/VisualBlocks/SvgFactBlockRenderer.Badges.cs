using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

using static ChartForgeX.VisualBlocks.SvgVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    private static void RenderVisualBadge(SvgMarkupWriter writer, ChartTable table, ChartTableCell cell, double x, double y, double width, double height) {
        var theme = table.Options.ResolvedTheme;
        var color = cell.BadgeColor ?? VisualBlockRendering.StatusColor(theme, cell.BadgeStatus);
        var bodyScale = theme.DataLabelFontSize / 12;
        var fontSize = Math.Max(table.Dense ? 10 : 9, (table.Dense ? 10.5 : 11.5) * bodyScale);
        var caption = VisualBlockRendering.SingleLineText(cell.BadgeText);
        var horizontalPadding = 18 * bodyScale;
        var badgeWidth = Math.Min(width, Math.Max(28 * bodyScale, VisualBlockRendering.MeasureTextWidth(caption, fontSize, theme.FontFamily, 700) + horizontalPadding));
        var badgeHeight = Math.Min(22 * bodyScale, Math.Max(18 * bodyScale, height));
        var badgeX = BadgeX(x, width, badgeWidth, cell.Alignment ?? TextAlignment.Left);
        var badgeY = y + Math.Max(0, (height - badgeHeight) / 2);
        BadgeColors(theme, color, cell.BadgeStyle, out var fill, out var stroke, out var text);
        writer.StartElement("rect")
            .Attribute("data-cfx-role", "visual-badge")
            .Attribute("data-cfx-style", cell.BadgeStyle.ToString())
            .Attribute("x", badgeX)
            .Attribute("y", badgeY)
            .Attribute("width", badgeWidth)
            .Attribute("height", badgeHeight)
            .Attribute("rx", Math.Min(8 * bodyScale, badgeHeight / 2))
            .Attribute("fill", fill.ToCss())
            .Attribute("stroke", stroke.ToCss())
            .EndEmptyElement().Line();
        WriteText(writer, caption, badgeX + 7 * bodyScale, badgeY + badgeHeight * 0.66,
            badgeWidth - 14 * bodyScale, TextAlignment.Center, text, theme.FontFamily, fontSize, "700");
    }

    private static double BadgeX(double x, double width, double badgeWidth, TextAlignment alignment) {
        if (alignment == TextAlignment.Center) return x + (width - badgeWidth) / 2;
        if (alignment == TextAlignment.Right) return x + width - badgeWidth;
        return x;
    }

    private static void BadgeColors(ChartForgeX.Themes.ResolvedVisualTheme theme, ChartColor color, VisualBadgeStyle style, out ChartColor fill, out ChartColor stroke, out ChartColor text) {
        if (style == VisualBadgeStyle.Solid) {
            fill = color;
            stroke = color;
            text = ChartColor.White;
            return;
        }

        if (style == VisualBadgeStyle.Outline) {
            fill = ChartColor.Transparent;
            stroke = color.WithAlpha(190);
            text = color;
            return;
        }

        fill = color.WithAlpha(42);
        stroke = color.WithAlpha(84);
        text = color;
    }
}

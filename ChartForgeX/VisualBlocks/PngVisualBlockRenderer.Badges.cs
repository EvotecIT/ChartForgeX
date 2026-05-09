using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawVisualBadge(RgbaCanvas canvas, ChartTable table, ChartTableCell cell, double x, double y, double width, double height) {
        var theme = table.Options.Theme;
        var color = cell.BadgeColor ?? VisualBlockRendering.StatusColor(theme, cell.BadgeStatus);
        var fontSize = Math.Max(9, table.Dense ? 10.5 : 11.5);
        var badgeWidth = Math.Min(width, Math.Max(28, RgbaCanvas.MeasureTextEmphasizedWidth(cell.BadgeText, fontSize, null) + 18));
        var badgeHeight = Math.Min(22, Math.Max(18, height));
        var badgeX = BadgeX(x, width, badgeWidth, cell.Alignment ?? VisualTextAlignment.Left);
        var badgeY = y + Math.Max(0, (height - badgeHeight) / 2);
        BadgeColors(theme, color, cell.BadgeStyle, out var fill, out var stroke, out var text);
        if (fill.A > 0) canvas.FillRoundedRect(badgeX, badgeY, badgeWidth, badgeHeight, Math.Min(8, badgeHeight / 2), fill);
        canvas.StrokeRoundedRect(badgeX, badgeY, badgeWidth, badgeHeight, Math.Min(8, badgeHeight / 2), stroke, 1);
        DrawAlignedText(canvas, cell.BadgeText, badgeX + 7, badgeY + (badgeHeight - fontSize) * 0.52, badgeWidth - 14, VisualTextAlignment.Center, text, fontSize, true);
    }

    private static double BadgeX(double x, double width, double badgeWidth, VisualTextAlignment alignment) {
        if (alignment == VisualTextAlignment.Center) return x + (width - badgeWidth) / 2;
        if (alignment == VisualTextAlignment.Right) return x + width - badgeWidth;
        return x;
    }

    private static void BadgeColors(ChartForgeX.Themes.ChartTheme theme, ChartColor color, VisualBadgeStyle style, out ChartColor fill, out ChartColor stroke, out ChartColor text) {
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

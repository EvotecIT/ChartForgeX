using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawFooterAction(RgbaCanvas canvas, string label, string symbol, double footerY, double footerHeight, double x, double width, ChartForgeX.Themes.ChartTheme theme) {
        canvas.DrawLine(x, footerY, x + width, footerY, theme.PlotBorder, 1);
        var fontSize = Math.Max(10, theme.SubtitleFontSize);
        var y = footerY + (footerHeight - fontSize) * 0.52;
        DrawAlignedText(canvas, label, x, y, Math.Max(1, width - 38), VisualTextAlignment.Left, theme.MutedText, fontSize, false);
        DrawActionSymbol(canvas, symbol, x + width - 18, footerY + footerHeight * 0.5, 12, theme.Text, fontSize);
    }

    private static void DrawActionSymbol(RgbaCanvas canvas, string symbol, double centerX, double centerY, double size, ChartColor color, double fontSize) {
        if (symbol == ">") {
            var glyph = ChartActionGlyphGeometry.RightChevron(centerX, centerY, size);
            canvas.DrawLine(glyph.X1, glyph.Y1, glyph.X2, glyph.Y2, color, 1.8);
            canvas.DrawLine(glyph.X2, glyph.Y2, glyph.X3, glyph.Y3, color, 1.8);
            return;
        }

        DrawAlignedText(canvas, symbol, centerX - 14, centerY - fontSize * 0.5, 28, VisualTextAlignment.Right, color, fontSize, true);
    }
}

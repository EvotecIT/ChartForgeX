using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderActionSymbol(SvgMarkupWriter writer, string symbol, double centerX, double centerY, double size, ChartColor color, ChartForgeX.Themes.ChartTheme theme, double fontSize) {
        if (symbol == ">") {
            var glyph = ChartActionGlyphGeometry.RightChevron(centerX, centerY, size);
            var leftX = glyph.X1.ToString("0.###", CultureInfo.InvariantCulture);
            var rightX = glyph.X2.ToString("0.###", CultureInfo.InvariantCulture);
            var topY = glyph.Y1.ToString("0.###", CultureInfo.InvariantCulture);
            var middleY = glyph.Y2.ToString("0.###", CultureInfo.InvariantCulture);
            var bottomY = glyph.Y3.ToString("0.###", CultureInfo.InvariantCulture);
            writer.StartElement("path")
                .Attribute("data-cfx-role", "visual-action-chevron")
                .Attribute("d", "M " + leftX + " " + topY + " L " + rightX + " " + middleY + " L " + leftX + " " + bottomY)
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", 1.8)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement().Line();
            return;
        }

        WriteText(writer, symbol, centerX - 14, centerY + fontSize * 0.34, 28, TextAlignment.Right, color, theme.FontFamily, fontSize, "800");
    }
}

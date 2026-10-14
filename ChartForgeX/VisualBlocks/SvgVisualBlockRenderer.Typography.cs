using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void WriteFittedBodyText(SvgMarkupWriter writer, string text, double x, double baseline, double width, double height,
        TextAlignment alignment, ChartColor color, ResolvedVisualTheme theme, double defaultSize, int weight, string role = "visual-text") {
        var style = VisualBlockRendering.FitBodyTextStyle(theme, text, defaultSize, width, height, weight);
        WriteText(writer, text, x, baseline, width, alignment, color, theme.FontFamily, style.FontSize, weight.ToString(CultureInfo.InvariantCulture), role);
    }
}

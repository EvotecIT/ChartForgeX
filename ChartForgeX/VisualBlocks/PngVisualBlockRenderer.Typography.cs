using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawFittedBodyText(RgbaCanvas canvas, string text, double x, double baseline, double width, double height,
        TextAlignment alignment, ChartColor color, ChartTheme theme, double defaultSize, int weight) {
        var style = VisualBlockRendering.FitBodyTextStyle(theme, text, defaultSize, width, height, weight);
        var fitted = VisualBlockRendering.FitMeasuredText(text, style.FontSize, width, theme.FontFamily, weight);
        var metrics = TextLayoutEngine.Measure(fitted, style);
        var left = alignment == TextAlignment.Center ? x + (width - metrics.Width) / 2 : alignment == TextAlignment.Right ? x + width - metrics.Width : x;
        var ascent = VisualBlockRendering.TextAscent(style);
        style.Color = color;
        canvas.DrawStyledText(left, baseline - ascent, width, fitted, style, TextWrapMode.NoWrap, maximumLines: 1, trimming: TextTrimming.None);
    }
}

using System;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    // Ordinary long captions retain truncation at their default size. Larger requested
    // sizes fit the measured width and height of their semantic cell first.
    public static TextStyle FitBodyTextStyle(ChartTheme theme, string text, double defaultSize, double width, double height, int weight) {
        var style = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = 1, LineHeight = 1 };
        var measured = ChartLabelScene.MeasureText(SingleLineText(text), style);
        var lineHeight = ChartLabelScene.MeasureText("Mg", style).Height;
        var widthLimit = measured.Width > 0 ? Math.Max(defaultSize, Math.Max(1, width) / measured.Width) : defaultSize;
        style.FontSize = Math.Min(BodyFontSize(theme, defaultSize), Math.Min(widthLimit, Math.Max(1, height) / lineHeight));
        return style;
    }
}

using System;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    // Ordinary long captions retain truncation at their default size. Larger requested
    // sizes fit the measured width and height of their semantic cell first.
    public static TextStyle FitBodyTextStyle(ChartTheme theme, string text, double defaultSize, double width, double height, int weight) {
        var preferred = BodyFontSize(theme, defaultSize);
        var style = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = preferred, LineHeight = 1 };
        var normalized = SingleLineText(text);
        width = Math.Max(1, width); height = Math.Max(1, height);
        // Bitmap fallbacks and synthetic emphasis have fixed-size components. Measure
        // the actual candidate instead of assuming font metrics scale from one pixel.
        if (Fits(preferred, true)) return style;
        var minimum = Math.Min(defaultSize, preferred);
        var preserveWidth = preferred > defaultSize || Fits(minimum, true);
        var lower = Fits(minimum, preserveWidth) ? minimum : 0;
        var upper = preserveWidth ? preferred : minimum;
        for (var iteration = 0; iteration < 18; iteration++) {
            var candidate = (lower + upper) / 2;
            if (Fits(candidate, preserveWidth)) lower = candidate;
            else upper = candidate;
        }
        style.FontSize = Math.Max(.001, Math.Floor(lower * 1000) / 1000);
        return style;

        bool Fits(double size, bool includeWidth) {
            style.FontSize = size;
            return ChartLabelScene.MeasureText("Mg", style).Height <= height &&
                (!includeWidth || ChartLabelScene.MeasureText(normalized, style).Width <= width);
        }
    }
}

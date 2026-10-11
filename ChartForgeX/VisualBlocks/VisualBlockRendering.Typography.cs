using System;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    // Bitmap text uses discrete raster scales. Use its native ascent rather than a
    // proportional outline-font estimate when positioning the measured line box.
    public static double TextAscent(TextStyle style) =>
        TypographyFontResolver.ResolveFace(style.Font).Font?.Ascent(style.FontSize) ?? TextLayoutEngine.Measure("Mg", style).LineHeight;

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
        style.FontSize = Math.Max(.001, ChartTextFitting.FitFontSize(lower, upper, size => Fits(size, preserveWidth)));
        return style;

        bool Fits(double size, bool includeWidth) {
            style.FontSize = size;
            return TextLayoutEngine.Measure("Mg", style).Height <= height &&
                (!includeWidth || TextLayoutEngine.Measure(normalized, style).Width <= width);
        }
    }
}

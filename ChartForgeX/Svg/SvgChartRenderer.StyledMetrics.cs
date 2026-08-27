using System;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static double EstimateSvgStyledTextWidth(Chart chart, string text, double fontSize, TextStyleOverride style, bool emphasized = false) {
        return MeasureSvgStyledTextWidth(chart, StyleText(style, text), fontSize, style, emphasized);
    }

    private static double MeasureSvgStyledTextWidth(Chart chart, string text, double fontSize, TextStyleOverride style, bool emphasized = false) {
        var resolved = new TextStyle {
            Font = FontSpec.FromFamily(StyleFontFamily(chart, style)),
            FontSize = fontSize
        };
        resolved.Font.Italic = style.Italic;
        var weight = style.ResolveFontWeight(emphasized ? 700 : 400);
        resolved.Font.Weight = Math.Max(100, Math.Min(900, (int)Math.Round(weight / 100.0) * 100));
        return TextLayoutEngine.Measure(text, resolved).Width;
    }

    private static string TrimSvgLabelToWidth(Chart chart, string value, double fontSize, double maxWidth, TextStyleOverride style, bool emphasized = false) {
        if (string.IsNullOrEmpty(value) || EstimateSvgStyledTextWidth(chart, value, fontSize, style, emphasized) <= maxWidth) return StyleText(style, value);
        const string suffix = "...";
        if (EstimateSvgStyledTextWidth(chart, suffix, fontSize, style, emphasized) > maxWidth) return string.Empty;
        var transformed = StyleText(style, value);
        var low = 0;
        var high = transformed.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = transformed.Substring(0, mid).TrimEnd() + suffix;
            if (MeasureSvgStyledTextWidth(chart, candidate, fontSize, style, emphasized) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : transformed.Substring(0, low).TrimEnd() + suffix;
    }

    private static double TextFontSizeForSvgWidth(Chart chart, string text, double maxWidth, double preferredFontSize, TextStyleOverride style, bool emphasized = false, double minFontSize = 8) {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) return preferredFontSize;
        var fontSize = preferredFontSize;
        while (fontSize > minFontSize && EstimateSvgStyledTextWidth(chart, text, fontSize, style, emphasized) > maxWidth) fontSize -= 0.5;
        return Math.Max(minFontSize, fontSize);
    }
}

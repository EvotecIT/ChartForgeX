using System;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static bool TryFitSvgDataLabel(string label, Chart chart, ChartRect plot, ChartSeries? series, int pointIndex, out TextStyleOverride style, out string fittedLabel, out double fontSize) {
        style = DataLabelStyle(chart, series, pointIndex);
        fittedLabel = StyleText(style, label);
        fontSize = StyleFontSize(style, chart.Options.Theme.DataLabelFontSize);
        fittedLabel = TrimSvgLabelToWidth(fittedLabel, fontSize, PlotLabelMaxWidth(plot));
        return fittedLabel.Length > 0;
    }

    private static double EstimateSvgStyledTextHeight(double fontSize, TextStyleOverride? style) {
        var height = fontSize * 1.2;
        var underline = style?.UnderlineStyle ?? (style?.Underline == true ? TextDecorationStyle.Single : TextDecorationStyle.None);
        if (underline != TextDecorationStyle.None) {
            var thickness = Math.Max(1, fontSize / 13.0);
            height = Math.Max(height, fontSize + 2 + TextDecorationMetrics.OuterExtent(underline, thickness));
        }
        return height + Math.Abs(SvgBaselineOffset(style, fontSize));
    }

    private static double SvgBaselineOffset(TextStyleOverride? style, double fontSize) => style?.Baseline switch {
        TextBaseline.Superscript => -fontSize * 0.35,
        TextBaseline.Subscript => fontSize * 0.22,
        _ => 0
    };

    private static double TextFontSizeForSvgBounds(string text, double maxWidth, double maxHeight, double preferredFontSize, TextStyleOverride? style, double minFontSize = 8) {
        var fontSize = TextFontSizeForSvgWidth(text, maxWidth, preferredFontSize, minFontSize);
        while (fontSize > minFontSize && EstimateSvgStyledTextHeight(fontSize, style) > maxHeight) fontSize -= 0.5;
        return Math.Max(minFontSize, fontSize);
    }
}

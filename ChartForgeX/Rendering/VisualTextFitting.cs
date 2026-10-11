using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Fits oversized authored typography against measured native layout bounds.</summary>
internal static class VisualTextFitting {
    internal static TextStyle FitOversized(VisualSceneBuilder builder, TextStyle style, double availableHeight, Func<TextStyle, bool> fits) {
        if (availableHeight <= 0 || builder.MeasureText("Mg", style).Height <= availableHeight) return style;
        var fitted = style.Clone();
        fitted.FontSize = ChartTextFitting.FitFontSize(Math.Min(8, style.FontSize), style.FontSize, size => {
            fitted.FontSize = size;
            return fits(fitted);
        });
        return fitted;
    }
}

using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Fits oversized authored typography against measured native layout bounds.</summary>
internal static class VisualTextFitting {
    internal const double MinimumOrdinaryFontSize = 10;
    internal const double MaximumOrdinaryFontSize = 18;
    internal static TextStyle FitOversized(VisualSceneBuilder builder, TextStyle style, double availableHeight, Func<TextStyle, bool> fits) {
        // Ordinary captions keep readable type; their owner places, wraps, shortens or thins them.
        // A per-tick or per-mark allocation must not force the complete axis below its requested size.
        if (availableHeight <= 0 || style.EffectiveFontSize <= MaximumOrdinaryFontSize) return style;
        if (builder.MeasureText("Mg", style).Height <= availableHeight && fits(style)) return style;
        var fitted = style.Clone();
        var minimum = Math.Min(style.FontSize, MinimumOrdinaryFontSize * style.FontSize / style.EffectiveFontSize);
        fitted.FontSize = ChartTextFitting.FitFontSize(minimum, style.FontSize, size => {
            fitted.FontSize = size;
            return fits(fitted);
        });
        return fitted;
    }
}

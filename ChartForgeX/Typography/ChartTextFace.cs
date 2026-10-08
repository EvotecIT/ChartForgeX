using ChartForgeX.Core;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>Resolves the complete chart text style identically for SVG layout and native raster drawing.</summary>
internal static class ChartTextFace {
    internal static ResolvedTypeface Resolve(string? family, TextStyleOverride style, int fallbackWeight, TrueTypeFont? explicitFont = null) {
        var weight = style.ResolveFontWeight(fallbackWeight);
        var face = explicitFont == null
            ? TypographyFontResolver.ResolveFace(style.FontFamily ?? family, weight, style.Italic)
            : new ResolvedTypeface(explicitFont, weight >= 600, style.Italic);
        return TypographyFontResolver.WithColorPalette(TypographyFontResolver.WithVariations(
            TypographyFontResolver.WithLanguage(face, style.OpenTypeLanguageTag), style.Variations), style.ColorPaletteIndex);
    }

    internal static double Measure(string text, double size, TextStyleOverride style, ResolvedTypeface face) =>
        TextLayoutEngine.MeasureWidth(text, new TextStyle {
            FontSize = size,
            Font = new FontSpec { Italic = style.Italic },
            OpenTypeLanguageTag = style.OpenTypeLanguageTag == "normal" ? null : style.OpenTypeLanguageTag
        }, face);
}

using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static bool UsesLetterSpacing(SvgRasterElement element, SvgRasterStyle style, SvgRasterDefinitions definitions, List<SvgRasterElement> ancestors) {
        if (style.LetterSpacing != 0) return true;
        foreach (var child in element.Children) if (child.Name == "tspan") {
            var selected = SvgRasterStyle.Resolve(style, child, definitions.StyleSheet, ancestors);
            ancestors.Add(child); var uses = UsesLetterSpacing(child, selected, definitions, ancestors); ancestors.RemoveAt(ancestors.Count - 1);
            if (uses) return true;
        }
        return false;
    }
    private static double SpacedTextAdvance(string text, double size, ResolvedTypeface face, double spacing) {
        var glyphs = face.Font == null ? null : ChartForgeX.Typography.TextShaper.ShapeWithLetterSpacing(face.Font, text, size, spacing);
        var width = glyphs == null ? TinyFont.SpacedWidth(text, size, spacing) : TrueTypeFont.MeasureGlyphs(glyphs, size);
        if (face.SynthesizeBold && text.Length > 0 && (face.Font == null || face.Font.NeedsSyntheticBold(glyphs!))) width += TextEmphasisOffset(size);
        return width;
    }
}

using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    /// <summary>Whether any painted glyph still needs the primary face's synthetic bold offset.</summary>
    internal bool NeedsSyntheticBold(string text) => IsSimpleRun(text) || NeedsSyntheticBold(TextShaper.Shape(this, text));

    internal bool NeedsSyntheticBold(IReadOnlyList<ShapedGlyph> glyphs) {
        foreach (var glyph in glyphs) if (ReferenceEquals(glyph.Face, this) || glyph.Face.Weight < 600) return true;
        return false;
    }
    /// <summary>Measures already shaped visual glyphs without resolving bidi or joining a second time.</summary>
    internal static double MeasureGlyphs(IReadOnlyList<ShapedGlyph> glyphs, double fontSize) {
        var width = 0.0;
        ShapedGlyph? previous = null;
        foreach (var glyph in glyphs) {
            width += GlyphAdvance(glyph, previous, fontSize);
            previous = glyph;
        }
        return width;
    }

    /// <summary>Advance for a visual glyph, including pair kerning when the previous glyph uses the same face.</summary>
    internal static double GlyphAdvance(ShapedGlyph glyph, ShapedGlyph? previous, double fontSize) =>
        (glyph.Advance ?? (glyph.Face.AdvanceWidth(glyph.Glyph) + (previous.HasValue && !previous.Value.Advance.HasValue && ReferenceEquals(previous.Value.Face, glyph.Face)
            ? glyph.Face.Kerning(previous.Value.Glyph, glyph.Glyph) : 0))) * glyph.Face.ScaleFor(fontSize);

    /// <summary>Paints a pre-shaped visual run at the primary face's baseline, preserving fallback face metrics.</summary>
    internal bool DrawGlyphs(RgbaCanvas canvas, double x, double y, IReadOnlyList<ShapedGlyph> glyphs, ChartColor color, double fontSize, bool italic, bool syntheticBoldCopyOnly = false, double boldOffset = 0) {
        var fit = GlyphGridFit.Create(canvas, fontSize, y + Ascent(fontSize));
        var baseline = fit?.Baseline ?? y + Ascent(fontSize);
        ShapedGlyph? previous = null;
        var rendered = false;
        foreach (var glyph in glyphs) {
            var scale = glyph.Face.ScaleFor(fontSize);
            var advance = GlyphAdvance(glyph, previous, fontSize);
            x += advance - GlyphAdvance(glyph, null, fontSize);
            if (!syntheticBoldCopyOnly || ReferenceEquals(glyph.Face, this) || glyph.Face.Weight < 600)
                rendered |= glyph.Face.DrawGlyph(canvas, glyph.Glyph, x + glyph.OffsetX * scale, baseline - glyph.OffsetY * scale, scale, italic && !glyph.Face.IsItalic, color, fit,
                    ReferenceEquals(glyph.Face, this) || glyph.Face.Weight < 600 ? boldOffset : 0);
            x += GlyphAdvance(glyph, null, fontSize);
            previous = glyph;
        }
        return rendered;
    }
}

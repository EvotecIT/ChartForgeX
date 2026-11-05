using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class TextShaper {
    // Culture data writes Unicode spaces into ordinary captions: U+202F in ICU time and unit patterns ("1:00 AM"),
    // U+00A0 and U+202F as thousands separators. A face without one draws its own space glyph at the width the
    // character asks for, so the caption keeps one face instead of depending on the machine's fallback fonts.
    private static bool DrawsAsSpace(TrueTypeFont face, int cp) => !face.HasGlyph(cp) && SpaceAdvance(face, cp).HasValue;

    /// <summary>The font-unit advance of a Unicode space separator drawn with the face's U+0020 glyph, or null.</summary>
    private static double? SpaceAdvance(TrueTypeFont face, int cp) {
        var space = face.MapGlyph(0x20);
        if (space == 0) return null;
        double em = face.UnitsPerEm;
        return cp switch {
            0x00A0 => face.AdvanceWidth(space),
            0x2000 or 0x2002 => em / 2,
            0x2001 or 0x2003 or 0x3000 => em,
            0x2004 => em / 3,
            0x2005 => em / 4,
            0x2006 => em / 6,
            0x2007 => GlyphAdvance(face, '0') ?? em / 2,
            0x2008 => GlyphAdvance(face, '.') ?? face.AdvanceWidth(space),
            0x2009 => em / 5,
            0x200A => em / 10,
            0x202F => GlyphAdvance(face, 0x2009) ?? em / 5,
            0x205F => em * 4 / 18,
            _ => null
        };
    }

    private static double? GlyphAdvance(TrueTypeFont face, int cp) {
        var glyph = face.MapGlyph(cp);
        return glyph == 0 ? null : face.AdvanceWidth(glyph);
    }
}

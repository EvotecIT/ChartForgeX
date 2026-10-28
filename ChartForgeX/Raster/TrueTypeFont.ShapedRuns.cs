using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    /// <summary>Whether any painted glyph still needs the primary face's synthetic bold offset.</summary>
    internal bool NeedsSyntheticBold(string text) =>
        !HasSelectedAxis("wght") && _colors == null && IsSimpleRun(text) || NeedsSyntheticBold(TextShaper.Shape(this, text));

    /// <summary>Preserves the native simple-run short circuit without resolving fallback during captured replay.</summary>
    internal bool NeedsSyntheticBoldCaptured(string text, IReadOnlyList<ShapedGlyph> glyphs) =>
        !HasSelectedAxis("wght") && _colors == null && IsSimpleRun(text) || NeedsSyntheticBold(glyphs);

    internal bool NeedsSyntheticBold(IReadOnlyList<ShapedGlyph> glyphs) {
        foreach (var glyph in glyphs) if (CanSynthesizeBold(glyph)) return true;
        return false;
    }

    /// <summary>Authored variable weight and colour paint exclude every synthetic-bold entry point.</summary>
    private bool CanSynthesizeBold(ShapedGlyph glyph) =>
        (ReferenceEquals(glyph.Face, this) || glyph.Face.Weight < 600) &&
        !glyph.Face.HasSelectedAxis("wght") && !glyph.Face.TryColorInk(glyph.Glyph, out _);
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
    internal static double GlyphAdvance(ShapedGlyph glyph, ShapedGlyph? previous, double fontSize) {
        var advance = (glyph.Advance ?? (glyph.Face.AdvanceWidth(glyph.Glyph) + (previous.HasValue && !previous.Value.Advance.HasValue && ReferenceEquals(previous.Value.Face, glyph.Face)
            ? glyph.Face.Kerning(previous.Value.Glyph, glyph.Glyph) : 0))) * glyph.Face.ScaleFor(fontSize);
        return glyph.TrailingAdvance == 0 ? advance : advance + glyph.TrailingAdvance * glyph.Face.ScaleFor(fontSize);
    }

    /// <summary>A regular fallback retains its advance but receives the requested run's bold ink.</summary>
    private double FallbackBoldOffset(ShapedGlyph glyph, double size) =>
        FallbackWeight >= 600 && !ReferenceEquals(glyph.Face.Root, Root) && CanSynthesizeBold(glyph)
            ? RgbaCanvas.EmphasisOffset(size) : 0;

    /// <summary>Paints a pre-shaped visual run at the primary face's baseline, preserving fallback face metrics.</summary>
    internal bool DrawGlyphs(RgbaCanvas canvas, double x, double y, IReadOnlyList<ShapedGlyph> glyphs, ChartColor color, double fontSize, bool italic, double boldOffset = 0) {
        var fit = GlyphGridFit.Create(canvas, fontSize, y + Ascent(fontSize));
        var baseline = fit?.Baseline ?? y + Ascent(fontSize);
        var run = new PositionedGlyphRun(this, glyphs, x, baseline, fontSize, italic, boldOffset);
        var rendered = false;
        while (run.MoveNext(out var glyph))
            rendered |= glyph.Face.DrawGlyph(canvas, glyph.Glyph, glyph.X, glyph.Baseline, glyph.Scale, glyph.Italic, color, fit, glyph.BoldOffset);
        return rendered;
    }
}

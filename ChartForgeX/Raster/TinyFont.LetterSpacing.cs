using System;
using System.Collections.Generic;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal static partial class TinyFont {
    /// <summary>One placement owner for the incumbent built-in repertoire; adds gaps only at native source-group boundaries.</summary>
    internal static IReadOnlyList<SpacedGlyph> SpacedGlyphs(string text, double size, double spacing, double origin = 0) {
        TextStyle.ValidateLetterSpacing(spacing);
        var groups = TextShaper.SourceTrackingGroups(text); var result = new List<SpacedGlyph>(text.Length);
        var scalar = 0; var cursor = origin; var groupAdvance = 0d;
        for (var index = 0; index < text.Length;) {
            var end = index; TrueTypeFont.ReadCodePoint(text, ref end);
            for (; index < end; index++) {
                result.Add(new SpacedGlyph(text[index], cursor));
                var advance = AdvanceFor(text[index]) * ScaleForFontSize(size); cursor += advance; groupAdvance += advance;
            }
            var boundary = scalar + 1 < groups.Length && groups[scalar] != groups[scalar + 1]; scalar++;
            if (boundary && spacing != 0) { TextShaper.ValidateProgression(groupAdvance + spacing); cursor += spacing; groupAdvance = 0; }
            if (double.IsNaN(cursor) || double.IsInfinity(cursor)) throw new ArgumentOutOfRangeException(nameof(spacing));
        }
        return result.AsReadOnly();
    }
    internal static double SpacedWidth(string text, double size, double spacing) {
        var glyphs = SpacedGlyphs(text, size, spacing);
        return glyphs.Count == 0 ? 0 : glyphs[glyphs.Count - 1].X + AdvanceFor(glyphs[glyphs.Count - 1].Character) * ScaleForFontSize(size);
    }
    internal readonly struct SpacedGlyph {
        internal SpacedGlyph(char character, double x) { Character = character; X = x; }
        internal char Character { get; }
        internal double X { get; }
    }
}

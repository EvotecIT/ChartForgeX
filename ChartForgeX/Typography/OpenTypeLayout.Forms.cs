using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    private readonly Dictionary<string, Dictionary<ulong, bool>> _formProbes = new();

    /// <summary>Classifies a two- or three-glyph Indic form using the font's context-free feature, after locl.</summary>
    internal bool SubstitutesForm(string script, string feature, LayoutGlyph first, LayoutGlyph second, LayoutGlyph? third = null) {
        if (!HasFeature(script, feature)) return false;
        var key = script + ":" + feature;
        // The length bit keeps a two-glyph probe distinct from a three-glyph probe ending in .notdef.
        var pair = (ulong)first.Glyph << 32 | (ulong)second.Glyph << 16 | (third == null ? 0ul : 1ul << 48 | third.Glyph);
        lock (_formProbes) {
            if (!_formProbes.TryGetValue(key, out var pairs)) _formProbes[key] = pairs = new Dictionary<ulong, bool>();
            if (pairs.TryGetValue(pair, out var result)) return result;
            var glyphs = new List<LayoutGlyph> { first.Copy(first.Glyph), second.Copy(second.Glyph) };
            if (third != null) glyphs.Add(third.Copy(third.Glyph));
            foreach (var glyph in glyphs) { glyph.Features = uint.MaxValue; glyph.SkipForSubstitution = false; }
            Apply(glyphs, script, new[] { "locl" });
            var before = new ushort[glyphs.Count];
            for (var i = 0; i < glyphs.Count; i++) before[i] = glyphs[i].Glyph;
            Apply(glyphs, script, new[] { feature });
            result = before.Length != glyphs.Count;
            for (var i = 0; !result && i < before.Length; i++) result = before[i] != glyphs[i].Glyph;
            if (pairs.Count >= 2048) pairs.Clear();
            return pairs[pair] = result;
        }
    }
}

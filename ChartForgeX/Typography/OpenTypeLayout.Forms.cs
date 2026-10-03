using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    private readonly Dictionary<string, Dictionary<uint, bool>> _formProbes = new();

    /// <summary>Classifies an Indic consonant using the font's context-free form feature, after locl.</summary>
    internal bool SubstitutesPair(string script, string feature, LayoutGlyph first, LayoutGlyph second) {
        if (!HasFeature(script, feature)) return false;
        var key = script + ":" + feature;
        var pair = (uint)first.Glyph << 16 | second.Glyph;
        lock (_formProbes) {
            if (!_formProbes.TryGetValue(key, out var pairs)) _formProbes[key] = pairs = new Dictionary<uint, bool>();
            if (pairs.TryGetValue(pair, out var result)) return result;
            var glyphs = new List<LayoutGlyph> { first.Copy(first.Glyph), second.Copy(second.Glyph) };
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

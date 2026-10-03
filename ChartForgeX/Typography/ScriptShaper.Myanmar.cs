using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class ScriptShaper {
    private static readonly string[] MyanmarForms = { "rphf", "pref", "blwf", "pstf" };

    /// <summary>Myanmar's first base, kinzi, medial-ra and pre-vowels precede font-owned forms.</summary>
    private static void ShapeMyanmar(TrueTypeFont face, List<LayoutGlyph> glyphs, string tag, OpenTypeLayout.LayoutExecution budget) {
        var result = new List<LayoutGlyph>(glyphs.Count);
        for (var from = 0; from < glyphs.Count;) {
            var end = MyanmarSyllableEnd(glyphs, from);
            var syllable = glyphs.GetRange(from, end - from);
            var kinzi = HasKinzi(syllable, 0) ? 3 : 0;
            var originalCount = syllable.Count;
            if ((kinzi == syllable.Count || !MyanmarBase(syllable[kinzi].CodePoint)) && face.HasGlyph(0x25cc) &&
                (kinzi != 0 || MyanmarMark(syllable[0].CodePoint)))
                syllable.Insert(kinzi, new LayoutGlyph(face.MapGlyph(0x25cc), 0x25cc, syllable[0].Cluster));
            budget.AccountGrowth(syllable.Count - originalCount);
            // Classify before preprocessing: GSUB contraction/expansion retains these positions,
            // while a numeric prefix length would no longer identify the base after ccmp.
            for (var i = 0; i < syllable.Count; i++) {
                var glyph = syllable[i]; glyph.Features &= ~16u;
                glyph.ScriptPosition = MyanmarPreVowel(glyph.CodePoint) ? PreMatra : glyph.CodePoint == 0x103c ? PreConsonant :
                    i < kinzi ? AfterMain : i == kinzi ? Base : Below;
                if (i < kinzi) glyph.Features |= 16u;
            }
            face.Layout.Apply(syllable, tag, Common, required: true, budget: budget);
            StableOrder(syllable);
            var reordered = syllable;
            ReorderMyanmarAnusvara(reordered);
            foreach (var feature in MyanmarForms) Feature(face.Layout, reordered, tag, feature, budget);
            foreach (var glyph in reordered) glyph.SkipForSubstitution = glyph.Ignorable;
            face.Layout.Apply(reordered, tag, Presentation, budget: budget);
            MergeClusters(reordered); result.AddRange(reordered); from = end;
        }
        glyphs.Clear(); glyphs.AddRange(result);
    }

    private static bool HasKinzi(List<LayoutGlyph> glyphs, int from) => from + 2 < glyphs.Count &&
        (glyphs[from].CodePoint == 0x1004 || glyphs[from].CodePoint == 0x101b || glyphs[from].CodePoint == 0x105a) &&
        glyphs[from + 1].CodePoint == 0x103a && glyphs[from + 2].CodePoint == 0x1039;

    private static bool MyanmarBase(int cp) => IndicCharacterData.Category(cp) == IndicCategory.Consonant ||
        IndicCharacterData.Category(cp) == IndicCategory.Vowel || IndicCharacterData.Category(cp) == IndicCategory.Number ||
        cp == 0x25cc || cp == 0xa0;
    private static bool MyanmarMark(int cp) {
        var kind = IndicCharacterData.Category(cp);
        return kind == IndicCategory.Matra || kind == IndicCategory.Halant || kind == IndicCategory.Sign ||
            kind == IndicCategory.Accent || kind == IndicCategory.Medial;
    }
    private static bool MyanmarPreVowel(int cp) => cp == 0x1031 || cp == 0x1084;
    private static bool MyanmarBelowVowel(int cp) => cp == 0x102f || cp == 0x1030 || cp == 0x1058 || cp == 0x1059;

    // Each contiguous block is stably partitioned once; repeated marks cannot rescan or shift it.
    private static void ReorderMyanmarAnusvara(List<LayoutGlyph> glyphs) {
        for (var start = 0; start < glyphs.Count;) {
            if (!MyanmarBelowVowel(glyphs[start].CodePoint)) { start++; continue; }
            var end = start + 1;
            while (end < glyphs.Count && (MyanmarBelowVowel(glyphs[end].CodePoint) || glyphs[end].CodePoint == 0x1036)) end++;
            var block = new LayoutGlyph[end - start]; var at = 0;
            for (var i = start; i < end; i++) if (glyphs[i].CodePoint == 0x1036) block[at++] = glyphs[i];
            for (var i = start; i < end; i++) if (glyphs[i].CodePoint != 0x1036) block[at++] = glyphs[i];
            for (var i = start; i < end; i++) glyphs[i] = block[i - start];
            start = end;
        }
    }

    private static int MyanmarSyllableEnd(List<LayoutGlyph> glyphs, int from) {
        var cursor = from;
        if (HasKinzi(glyphs, cursor)) cursor += 3;
        var connected = cursor > from;
        if (cursor < glyphs.Count && MyanmarBase(glyphs[cursor].CodePoint)) { cursor++; connected = false; }
        else if (cursor == from && !MyanmarMark(glyphs[cursor].CodePoint)) return from + 1;
        for (; cursor < glyphs.Count; cursor++) {
            var cp = glyphs[cursor].CodePoint;
            if (cp == 0x1039) { connected = true; continue; }
            if (IsJoiner(cp)) return cursor + 1;
            if (MyanmarBase(cp)) { if (!connected) break; connected = false; continue; }
            if (!MyanmarMark(cp)) break;
        }
        return cursor;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>Unicode syllable rules and feature stages above the shared, font-driven lookup engine.</summary>
internal static partial class ScriptShaper {
    private static readonly string[] Common = { "locl", "ccmp" };
    private static readonly string[] Presentation = { "pres", "abvs", "blws", "psts", "haln", "rclt", "calt", "clig" };
    private const uint FormMasks = 16u | 32u | 64u | 128u | 256u | 512u | 1024u;
    private const byte Reph = 0, PreMatra = 1, PreConsonant = 2, Base = 4, AfterMain = 5, Below = 6, AfterBelow = 7, Post = 8, AfterPost = 9, Sign = 12;

    internal static string SelectTag(OpenTypeLayout layout, string script, bool positioning = false) {
        if (script == "mymr") return layout.HasScript("mym2", positioning) ? "mym2" : script;
        if (!IndicScriptProfile.TryGet(script, out var profile)) return script;
        return layout.HasScript(profile.ModernTag, positioning) ? profile.ModernTag : script;
    }

    internal static bool Shape(TrueTypeFont face, List<LayoutGlyph> glyphs, string script, string tag, OpenTypeLayout.LayoutExecution budget) {
        if (script == "thai" || script == "lao ") {
            ShapeThai(face, glyphs, tag, script == "lao ", budget); return true;
        }
        if (script == "khmr") { ShapeKhmer(face, glyphs, tag, budget); return true; }
        if (script == "sinh") { ShapeSinhala(face, glyphs, tag, budget); return true; }
        if (script == "mymr" && tag == "mym2") { ShapeMyanmar(face, glyphs, tag, budget); return true; }
        if (!IndicScriptProfile.TryGet(script, out var profile)) return false;
        var originalCount = glyphs.Count; Decompose(face, glyphs); budget.AccountGrowth(glyphs.Count - originalCount);
        var output = new List<LayoutGlyph>(glyphs.Count);
        for (var from = 0; from < glyphs.Count;) {
            var end = SyllableEnd(glyphs, from);
            var syllable = glyphs.GetRange(from, end - from);
            ShapeIndic(face, syllable, profile, tag, tag != script, from == 0 || !IsLetter(glyphs[from - 1].CodePoint), budget);
            output.AddRange(syllable); from = end;
        }
        glyphs.Clear(); glyphs.AddRange(output); return true;
    }
    private static void Feature(OpenTypeLayout layout, List<LayoutGlyph> glyphs, string tag, string feature, OpenTypeLayout.LayoutExecution budget) => layout.Apply(glyphs, tag, new[] { feature }, budget: budget);

    private static bool IsConsonant(LayoutGlyph glyph) => IndicCharacterData.Category(glyph.CodePoint) == IndicCategory.Consonant;
    private static bool IsLetter(int cp) { var kind = IndicCharacterData.Category(cp); return kind == IndicCategory.Consonant || kind == IndicCategory.Vowel || kind == IndicCategory.Matra; }
    private static bool IsJoiner(int cp) => cp == 0x200c || cp == 0x200d;
    private static bool IsBase(LayoutGlyph glyph) => IsConsonant(glyph) || IndicCharacterData.Category(glyph.CodePoint) == IndicCategory.Vowel || glyph.CodePoint == 0x25cc || glyph.CodePoint == 0xa0;

    /// <summary>Consonants connected by a halant remain one syllable; ordinary adjacent letters begin another.</summary>
    private static int SyllableEnd(List<LayoutGlyph> glyphs, int from, bool numberBase = false) {
        var first = IndicCharacterData.Category(glyphs[from].CodePoint);
        if (!IsBase(glyphs[from]) && (first == IndicCategory.Other || first == IndicCategory.Number && !numberBase)) return from + 1;
        var connected = first == IndicCategory.Halant || first == IndicCategory.Repha;
        var hasBase = IsBase(glyphs[from]);
        var i = from + 1;
        for (; i < glyphs.Count; i++) {
            var cp = glyphs[i].CodePoint; var kind = IndicCharacterData.Category(cp);
            if (kind == IndicCategory.Halant) { connected = true; continue; }
            if (IsJoiner(cp)) continue;
            if (kind == IndicCategory.Consonant || kind == IndicCategory.Vowel) {
                if (!connected) break;
                hasBase = true; connected = false; continue;
            }
            if (kind == IndicCategory.Matra || kind == IndicCategory.Nukta || kind == IndicCategory.Sign || kind == IndicCategory.Accent || kind == IndicCategory.Shifter || kind == IndicCategory.Medial) continue;
            if (kind == IndicCategory.Repha && !hasBase) { connected = true; continue; }
            break;
        }
        return i;
    }
    private static void DottedCircle(TrueTypeFont face, List<LayoutGlyph> glyphs) {
        if (glyphs.Count == 0 || IsBase(glyphs[0]) || !face.HasGlyph(0x25cc)) return;
        var kind = IndicCharacterData.Category(glyphs[0].CodePoint);
        if (kind != IndicCategory.Matra && kind != IndicCategory.Halant && kind != IndicCategory.Nukta && kind != IndicCategory.Accent && kind != IndicCategory.Sign && kind != IndicCategory.Shifter) return;
        glyphs.Insert(0, new LayoutGlyph(face.MapGlyph(0x25cc), 0x25cc, glyphs[0].Cluster));
    }
    private static void Decompose(TrueTypeFont face, List<LayoutGlyph> glyphs) {
        var decomposed = new List<LayoutGlyph>(glyphs.Count);
        foreach (var original in glyphs) {
            // Truncated UTF-16 retains the same missing-glyph path as other unsupported characters.
            if (original.CodePoint >= 0xd800 && original.CodePoint <= 0xdfff) { decomposed.Add(original); continue; }
            var normalized = char.ConvertFromUtf32(original.CodePoint).Normalize(NormalizationForm.FormD);
            if (normalized.Length <= 1) { decomposed.Add(original); continue; }
            var points = new List<int>(normalized.Length);
            for (var cursor = 0; cursor < normalized.Length;) points.Add(TrueTypeFont.ReadCodePoint(normalized, ref cursor));
            var covered = true;
            foreach (var cp in points) if (!face.HasGlyph(cp)) { covered = false; break; }
            if (!covered || points.Count == 1 && points[0] == original.CodePoint) { decomposed.Add(original); continue; }
            foreach (var cp in points) decomposed.Add(new LayoutGlyph(face.MapGlyph(cp), cp, original.Cluster));
        }
        glyphs.Clear(); glyphs.AddRange(decomposed);
    }
    private static void StableOrder(List<LayoutGlyph> glyphs) {
        // Counting positions keeps repeated marks linear while preserving equal-position order.
        var counts = new int[Sign + 1];
        foreach (var glyph in glyphs) counts[glyph.ScriptPosition]++;
        var sum = 0;
        for (var i = 0; i < counts.Length; i++) { var count = counts[i]; counts[i] = sum; sum += count; }
        var ordered = new LayoutGlyph[glyphs.Count];
        foreach (var glyph in glyphs) ordered[counts[glyph.ScriptPosition]++] = glyph;
        glyphs.Clear(); glyphs.AddRange(ordered);
    }
    private static void MergeClusters(List<LayoutGlyph> glyphs) {
        var source = int.MaxValue;
        foreach (var glyph in glyphs) source = Math.Min(source, glyph.Cluster);
        foreach (var glyph in glyphs) glyph.Cluster = source;
    }
}

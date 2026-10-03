using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class ScriptShaper {
    private static readonly string[] SinhalaForms = { "akhn", "rphf", "vatu", "pstf" };

    /// <summary>Normalises Sinhala vowels, applies font-owned forms, then reorders their visual components.</summary>
    private static void ShapeSinhala(TrueTypeFont face, List<LayoutGlyph> glyphs, string tag, OpenTypeLayout.LayoutExecution budget) {
        var result = new List<LayoutGlyph>(glyphs.Count);
        for (var from = 0; from < glyphs.Count;) {
            var end = SinhalaSyllableEnd(glyphs, from);
            var syllable = glyphs.GetRange(from, end - from);
            var originalCount = syllable.Count; DottedCircle(face, syllable);
            Decompose(face, syllable);
            var expanded = new List<LayoutGlyph>(syllable.Count);
            foreach (var glyph in syllable) {
                var cp = glyph.CodePoint;
                if (cp == 0x0dde && face.HasGlyph(0x0dd9) && face.HasGlyph(0x0ddf)) {
                    expanded.Add(new LayoutGlyph(face.MapGlyph(0x0dd9), 0x0dd9, glyph.Cluster));
                    expanded.Add(new LayoutGlyph(face.MapGlyph(0x0ddf), 0x0ddf, glyph.Cluster));
                    continue;
                }
                expanded.Add(glyph);
            }
            syllable = expanded; budget.AccountGrowth(syllable.Count - originalCount);
            face.Layout.Apply(syllable, tag, Common, required: true, budget: budget);
            foreach (var glyph in syllable) { glyph.Features &= ~16u; glyph.ScriptPosition = glyph.CodePoint == 0x0dd9 || glyph.CodePoint == 0x0ddb ? PreMatra : Base; }
            var repaya = syllable.Count >= 3 && syllable[0].CodePoint == 0x0dbb && syllable[1].CodePoint == 0x0dca && syllable[2].CodePoint == 0x200d;
            if (repaya) {
                for (var i = 0; i < 3; i++) syllable[i].Features |= 16u;
            }
            var rephGlyph = repaya ? syllable[0] : null; var rephBefore = rephGlyph?.Glyph;
            foreach (var feature in SinhalaForms) Feature(face.Layout, syllable, tag, feature, budget);
            if (rephGlyph != null && rephGlyph.Glyph != rephBefore && syllable.Contains(rephGlyph)) {
                syllable.Remove(rephGlyph);
                var target = syllable.Count;
                for (var i = 0; i < syllable.Count; i++) if (IndicCharacterData.Category(syllable[i].CodePoint) == IndicCategory.Matra ||
                    syllable[i].CodePoint == 0x0dca && syllable[i].ComponentClusters == null || IndicCharacterData.Category(syllable[i].CodePoint) == IndicCategory.Sign) { target = i; break; }
                syllable.Insert(target, rephGlyph);
            }
            StableOrder(syllable);
            var baseGlyph = syllable.FindLast(IsBase);
            if (baseGlyph != null) ReorderPreMatras(syllable, baseGlyph, face.MapGlyph(0x0dca));
            // Unlike Indic half-form stages, Sinhala joiners also block presentation ligatures.
            face.Layout.Apply(syllable, tag, Presentation, budget: budget);
            MergeClusters(syllable); result.AddRange(syllable); from = end;
        }
        glyphs.Clear(); glyphs.AddRange(result);
    }

    private static int SinhalaSyllableEnd(List<LayoutGlyph> glyphs, int from) {
        var end = SyllableEnd(glyphs, from);
        for (var i = from + 1; i < end; i++) if (IsBase(glyphs[i])) {
            var previous = i - 1;
            var joiner = false;
            while (previous >= from && IsJoiner(glyphs[previous].CodePoint)) { joiner |= glyphs[previous].CodePoint == 0x200d; previous--; }
            if (previous >= from && glyphs[previous].CodePoint == 0x0dca && previous > from && glyphs[previous - 1].CodePoint == 0x200d) joiner = true;
            if (!joiner) return i;
        }
        return end;
    }
}

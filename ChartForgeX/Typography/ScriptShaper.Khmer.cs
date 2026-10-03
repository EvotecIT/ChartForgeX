using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class ScriptShaper {
    private static readonly string[] KhmerForms = { "pref", "blwf", "abvf", "pstf" };
    private static void ShapeKhmer(TrueTypeFont face, List<LayoutGlyph> glyphs, string tag, OpenTypeLayout.LayoutExecution budget) {
        var result = new List<LayoutGlyph>(glyphs.Count);
        for (var from = 0; from < glyphs.Count;) {
            var end = SyllableEnd(glyphs, from, numberBase: true); var syllable = glyphs.GetRange(from, end - from);
            var originalCount = syllable.Count; DottedCircle(face, syllable);
            var expanded = new List<LayoutGlyph>(syllable.Count);
            foreach (var glyph in syllable) {
                var cp = glyph.CodePoint;
                if ((cp == 0x17be || cp == 0x17bf || cp == 0x17c0 || cp == 0x17c4 || cp == 0x17c5) && face.HasGlyph(0x17c1)) {
                    expanded.Add(new LayoutGlyph(face.MapGlyph(0x17c1), 0x17c1, glyph.Cluster));
                }
                expanded.Add(glyph);
            }
            syllable = expanded;
            budget.AccountGrowth(syllable.Count - originalCount);
            face.Layout.Apply(syllable, tag, Common, required: true, budget: budget);
            for (var i = 0; i < syllable.Count; i++) {
                var glyph = syllable[i]; var cp = glyph.CodePoint; var category = IndicCharacterData.Category(cp);
                glyph.Features &= ~FormMasks;
                glyph.ScriptPosition = Base;
                if (cp >= 0x17c1 && cp <= 0x17c3) glyph.ScriptPosition = PreMatra;
                else if (category == IndicCategory.Matra || category == IndicCategory.Sign || category == IndicCategory.Accent || category == IndicCategory.Shifter) glyph.ScriptPosition = AfterPost;
                glyph.Features |= 1024u | 128u; // Above/post vowels and robat are font-driven.
                if (cp == 0x17d2 && i + 1 < syllable.Count && IsBase(syllable[i + 1])) {
                    var next = syllable[i + 1]; var ro = next.CodePoint == 0x179a;
                    glyph.ScriptPosition = next.ScriptPosition = ro ? PreConsonant : Below;
                    glyph.Features |= ro ? 256u : 64u;
                    next.Features = glyph.Features; i++;
                } else if (category == IndicCategory.Shifter) {
                    var forced = i > 0 && syllable[i - 1].CodePoint == 0x200c || i + 1 < syllable.Count && syllable[i + 1].CodePoint == 0x200c;
                    if (!forced) for (var n = i + 1; n < syllable.Count; n++) {
                        var vowel = syllable[n].CodePoint;
                        if (vowel == 0x17b7 || vowel == 0x17b8 || vowel == 0x17b9 || vowel == 0x17ba || vowel == 0x17be) { glyph.Features |= 64u; break; }
                    }
                }
            }
            StableOrder(syllable);
            foreach (var feature in KhmerForms) Feature(face.Layout, syllable, tag, feature, budget);
            foreach (var glyph in syllable) glyph.SkipForSubstitution = glyph.Ignorable;
            face.Layout.Apply(syllable, tag, Presentation, budget: budget);
            MergeClusters(syllable); result.AddRange(syllable); from = end;
        }
        glyphs.Clear(); glyphs.AddRange(result);
    }
}

using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class ScriptShaper {
    private static readonly string[] IndicForms = { "nukt", "akhn", "rphf", "rkrf", "pref", "blwf", "half", "pstf", "vatu", "cjct" };

    private static void ShapeIndic(TrueTypeFont face, List<LayoutGlyph> glyphs, IndicScriptProfile profile, string tag, bool modern, bool wordInitial, OpenTypeLayout.LayoutExecution budget) {
        var originalCount = glyphs.Count; DottedCircle(face, glyphs); budget.AccountGrowth(glyphs.Count - originalCount);
        var layout = face.Layout;
        layout.Apply(glyphs, tag, Common, required: true, budget: budget);
        if (glyphs.Count == 0) return;
        var halant = new LayoutGlyph(face.MapGlyph(profile.Halant), profile.Halant, 0);
        var consonants = new List<int>();
        for (var i = 0; i < glyphs.Count; i++) if (IsBase(glyphs[i])) consonants.Add(i);
        if (consonants.Count == 0) { layout.Apply(glyphs, tag, Presentation, budget: budget); return; }
        var reph = false;
        if (glyphs.Count > 2 && glyphs[0].CodePoint == profile.Ra && glyphs[1].CodePoint == profile.Halant) {
            // An implicit reph cannot replace the syllable's only base. Telugu requires an explicit ZWJ;
            // Malayalam's logical repha is a character of its own rather than an initial Ra-halant form.
            if (profile.RephMode == IndicRephMode.Implicit && consonants.Count > 1 && !IsJoiner(glyphs[2].CodePoint))
                reph = layout.SubstitutesForm(tag, "rphf", glyphs[0], halant);
            if (profile.RephMode == IndicRephMode.Explicit && glyphs[2].CodePoint == 0x200d)
                reph = layout.SubstitutesForm(tag, "rphf", glyphs[0], halant) || layout.SubstitutesForm(tag, "rphf", glyphs[0], halant, glyphs[2]);
        }
        var below = new bool[glyphs.Count]; var post = new bool[glyphs.Count]; var pref = new bool[glyphs.Count];
        foreach (var i in consonants) {
            var first = modern ? halant : glyphs[i]; var second = modern ? glyphs[i] : halant;
            below[i] = layout.SubstitutesForm(tag, "blwf", first, second);
            post[i] = layout.SubstitutesForm(tag, "pstf", first, second);
            pref[i] = layout.SubstitutesForm(tag, "pref", first, second);
        }
        var firstBase = reph && consonants.Count > 1 ? 1 : 0;
        var baseIndex = consonants[firstBase]; var seenBelow = false;
        for (var c = consonants.Count - 1; c >= firstBase; c--) {
            var i = consonants[c]; baseIndex = i;
            if (!below[i] && !post[i] && !pref[i] || seenBelow && post[i]) break;
            seenBelow |= below[i];
        }
        if (profile.ModernTag == "knd2") for (var i = 0; i + 2 < glyphs.Count; i++) {
            if (IsConsonant(glyphs[i]) && glyphs[i + 1].CodePoint == profile.Halant && glyphs[i + 2].CodePoint == 0x200d) { baseIndex = i; break; }
        }
        var baseGlyph = glyphs[baseIndex];
        for (var i = 0; i < glyphs.Count; i++) {
            var glyph = glyphs[i]; var category = IndicCharacterData.Category(glyph.CodePoint);
            glyph.Features &= ~FormMasks;
            glyph.Features |= 512u;
            glyph.ScriptPosition = category switch {
                IndicCategory.Matra => MatraPosition(glyph.CodePoint, profile),
                IndicCategory.Sign or IndicCategory.Accent => Sign,
                IndicCategory.Repha => Reph,
                _ => i < baseIndex ? PreConsonant : i == baseIndex ? Base : AfterMain
            };
            if (IsBase(glyph)) {
                if (i > baseIndex) glyph.ScriptPosition = below[i] ? Below : Post;
                if (i < baseIndex) glyph.Features |= 32u;
                if (i > baseIndex) glyph.Features |= 64u | 128u | 256u;
                if (profile.BelowBeforeBase(modern) && i < baseIndex) glyph.Features |= 64u;
            }
        }
        if (reph) {
            glyphs[0].ScriptPosition = glyphs[1].ScriptPosition = Reph; glyphs[0].Features |= 16u;
            if (profile.RephMode == IndicRephMode.Explicit) glyphs[2].ScriptPosition = Reph;
        }
        // A post-base form starts with its preceding halant; a half form ends with its following halant.
        for (var i = 1; i < glyphs.Count; i++) {
            var glyph = glyphs[i]; var category = IndicCharacterData.Category(glyph.CodePoint);
            if (category == IndicCategory.Nukta || IsJoiner(glyph.CodePoint)) {
                glyph.ScriptPosition = glyphs[i - 1].ScriptPosition;
                if (profile.ModernTag == "knd2" && glyph.CodePoint == 0x200d && i >= 2 && glyphs[i - 1].CodePoint == profile.Halant)
                    glyph.ScriptPosition = glyphs[i - 2].ScriptPosition;
            }
            if (category == IndicCategory.Halant && !(reph && i == 1)) {
                var next = i + 1;
                while (next < glyphs.Count && IsJoiner(glyphs[next].CodePoint)) next++;
                glyph.ScriptPosition = next > baseIndex && next < glyphs.Count && IsBase(glyphs[next]) ? glyphs[next].ScriptPosition : glyphs[i - 1].ScriptPosition;
                if (glyph.ScriptPosition > Base) glyph.Features |= 64u | 128u | 256u;
                if (profile.BelowBeforeBase(modern) && glyph.ScriptPosition < Base) glyph.Features |= 64u;
            }
        }
        // ZWNJ requests the explicit virama; ZWJ requests a half form instead of a full conjunct.
        for (var i = 1; i < glyphs.Count; i++) if (IsJoiner(glyphs[i].CodePoint)) {
            for (var previous = i - 1; previous >= 0; previous--) {
                if (glyphs[i].CodePoint == 0x200c) glyphs[previous].Features &= ~32u;
                if (glyphs[i].CodePoint == 0x200d) glyphs[previous].Features &= ~2048u;
                if (IsConsonant(glyphs[previous])) break;
            }
        }
        if (!modern) {
            // Older Indic fonts encode post-base forms as consonant + halant.
            var terminal = consonants[consonants.Count - 1];
            if (baseIndex < terminal) {
                var at = baseIndex + 1;
                while (at < glyphs.Count && IndicCharacterData.Category(glyphs[at].CodePoint) == IndicCategory.Nukta) at++;
                if (at < glyphs.Count && glyphs[at].CodePoint == profile.Halant && (at + 1 >= glyphs.Count || !IsJoiner(glyphs[at + 1].CodePoint))) {
                    var moved = glyphs[at]; moved.ScriptPosition = glyphs[terminal].ScriptPosition;
                    glyphs.RemoveAt(at); glyphs.Insert(terminal, moved);
                }
            }
        }
        StableOrder(glyphs);
        var rephGlyph = reph || IndicCharacterData.Category(glyphs[0].CodePoint) == IndicCategory.Repha ? glyphs[0] : null;
        var rephBefore = rephGlyph?.Glyph;
        var prefOutputs = new List<LayoutGlyph>();
        foreach (var feature in IndicForms) {
            if (feature == "half") foreach (var glyph in glyphs) glyph.SkipForSubstitution = glyph.CodePoint == 0x200d;
            if (feature == "pref") {
                var before = glyphs.ToArray(); var ids = new ushort[before.Length];
                for (var i = 0; i < before.Length; i++) ids[i] = before[i].Glyph;
                Feature(layout, glyphs, tag, feature, budget);
                for (var i = 0; i < before.Length; i++) if (before[i].Glyph != ids[i] && glyphs.Contains(before[i])) prefOutputs.Add(before[i]);
            } else Feature(layout, glyphs, tag, feature, budget);
        }
        // Keep final reordering in the glyph buffer, after the font has decided which forms exist.
        if (!glyphs.Contains(baseGlyph)) {
            foreach (var glyph in glyphs) if (glyph.ScriptPosition >= PreConsonant && glyph.ScriptPosition <= Base && !glyph.IsMark) baseGlyph = glyph;
        }
        ReorderPreMatras(glyphs, baseGlyph, halant.Glyph);
        if (rephGlyph != null && (rephGlyph.Glyph != rephBefore || IndicCharacterData.Category(rephGlyph.CodePoint) == IndicCategory.Repha) && glyphs.Contains(rephGlyph)) {
            glyphs.Remove(rephGlyph);
            var target = glyphs.Count;
            for (var i = 0; i < glyphs.Count; i++) if (glyphs[i].ScriptPosition >= profile.RephPosition) { target = i; break; }
            if (profile.RephPosition != AfterPost) {
                var baseAt = glyphs.IndexOf(baseGlyph);
                for (var i = 0; i < baseAt; i++) if (glyphs[i].Glyph == halant.Glyph && glyphs[i].ComponentClusters == null) { target = AfterJoiners(glyphs, i + 1); break; }
            }
            glyphs.Insert(target, rephGlyph);
        }
        foreach (var glyph in prefOutputs) if (glyphs.Contains(glyph)) {
            glyphs.Remove(glyph); var target = Math.Max(0, glyphs.IndexOf(baseGlyph));
            for (var i = 0; i < target; i++) if (glyphs[i].Glyph == halant.Glyph && glyphs[i].ComponentClusters == null) target = AfterJoiners(glyphs, i + 1);
            glyphs.Insert(target, glyph);
        }
        foreach (var glyph in glyphs) { glyph.SkipForSubstitution = glyph.Ignorable; glyph.Features &= ~4u; }
        if (wordInitial) foreach (var glyph in glyphs) if (glyph.ScriptPosition == PreMatra) glyph.Features |= 4u;
        Feature(layout, glyphs, tag, "init", budget);
        layout.Apply(glyphs, tag, Presentation, budget: budget);
        MergeClusters(glyphs);
    }
    private static byte MatraPosition(int cp, IndicScriptProfile profile) => IndicCharacterData.Matra(cp) switch {
        IndicMatra.Left => PreMatra, IndicMatra.Top => profile.TopMatra, IndicMatra.Bottom => profile.BottomMatra,
        _ => profile.RightMatra
    };
    private static int AfterJoiners(List<LayoutGlyph> glyphs, int at) {
        while (at < glyphs.Count && IsJoiner(glyphs[at].CodePoint)) at++;
        return at;
    }
    private static void ReorderPreMatras(List<LayoutGlyph> glyphs, LayoutGlyph baseGlyph, ushort halant) {
        var baseAt = glyphs.IndexOf(baseGlyph); var target = 0;
        for (var i = 0; i < baseAt; i++) if (glyphs[i].Glyph == halant && glyphs[i].ComponentClusters == null) target = AfterJoiners(glyphs, i + 1);
        if (target == 0) return;
        var matras = glyphs.FindAll(glyph => glyph.ScriptPosition == PreMatra);
        foreach (var matra in matras) { var at = glyphs.IndexOf(matra); glyphs.RemoveAt(at); if (at < target) target--; }
        glyphs.InsertRange(target, matras);
    }
}

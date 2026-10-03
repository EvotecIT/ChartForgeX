using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class TextShaper {
    private static readonly string[] CommonSubstitution = { "ccmp", "locl" };
    private static readonly string[] ArabicForms = { "isol", "fina", "init", "medi" };
    private static readonly string[] StandardSubstitution = { "rlig", "rclt", "calt", "liga", "clig" };
    private static readonly string[] StandardPositioning = { "kern", "dist", "abvm", "blwm", "curs", "mark", "mkmk" };

    private static bool TryShapeAscii(TrueTypeFont face, string text, out ShapedGlyph[] shaped) {
        var script = "DFLT";
        foreach (var cp in text) {
            if (cp < 32 || cp > 126 || !face.HasGlyph(cp)) { shaped = Array.Empty<ShapedGlyph>(); return false; }
            if (cp >= 'A' && cp <= 'Z' || cp >= 'a' && cp <= 'z') script = "latn";
        }
        var run = new FontRun(face, script, 0, 0);
        for (var i = 0; i < text.Length; i++) run.Glyphs.Add(new LayoutGlyph(face.MapGlyph(text[i]), text[i], i));
        var result = new List<ShapedGlyph>(text.Length); FinishRun(run, result); shaped = result.ToArray();
        return true;
    }

    private static bool UsesArabicLayout(TrueTypeFont face) => face.Layout.HasFeature("arab", "init") || face.Layout.HasFeature("arab", "medi") || face.Layout.HasFeature("arab", "fina");

    private static void RetainJoiners(List<int> codePoints, Cluster cluster) {
        var hasJoiner = false;
        for (var i = cluster.Start; i < cluster.Start + cluster.Count; i++) if (codePoints[i] == 0x200d || codePoints[i] == 0x200c) hasJoiner = true;
        if (!hasJoiner) return;
        // Preserve logical joiner glyphs until GSUB can consume a ZWJ ligature. Coverage and fallback still use visible characters.
        var visible = new List<int>();
        for (var i = cluster.Start; i < cluster.Start + cluster.Count; i++) if (!IsIgnorable(codePoints[i])) visible.Add(codePoints[i]);
        var same = visible.Count == cluster.Output.Count;
        for (var i = 0; same && i < visible.Count; i++) same = visible[i] == cluster.Output[i];
        if (!same) {
            // Keep normalized/composed visible text; a hidden joiner must not undo a covered composed glyph.
            for (var i = cluster.Start; i < cluster.Start + cluster.Count; i++) if (codePoints[i] == 0x200d || codePoints[i] == 0x200c) cluster.Output.Add(codePoints[i]);
            return;
        }
        cluster.Output.Clear();
        for (var i = cluster.Start; i < cluster.Start + cluster.Count; i++) {
            var cp = codePoints[i];
            if (!IsIgnorable(cp) || cp == 0x200d || cp == 0x200c) cluster.Output.Add(cp);
        }
    }
    private static ShapedGlyph[] ShapeFontRuns(List<int> codePoints, List<Cluster> clusters, byte[]? levels) {
        if (clusters.Count == 0) return Array.Empty<ShapedGlyph>();
        var forms = ArabicShaping.MayJoin(codePoints) ? ArabicShaping.ResolveForms(codePoints) : null;
        var runs = new List<FontRun>(); FontRun? current = null; var script = "DFLT";
        // Common punctuation follows the surrounding script; leading punctuation adopts the first strong script.
        foreach (var cluster in clusters) { var candidate = ScriptOf(cluster.Base); if (candidate != "DFLT") { script = candidate; break; } }
        foreach (var cluster in clusters) {
            var candidate = ScriptOf(cluster.Base);
            if (candidate != "DFLT") script = candidate;
            var level = levels == null ? (byte)0 : levels[cluster.Start];
            if (current == null || !ReferenceEquals(current.Face, cluster.Face) || current.Owner != cluster.Owner || current.Script != script || current.Level != level) {
                current = new FontRun(cluster.Face!, script, cluster.Owner, level); runs.Add(current);
            }
            foreach (var cp in cluster.Output) {
                var glyph = new LayoutGlyph(cluster.Face!.MapGlyph(cp), cp, cluster.Start);
                if (forms != null && script == "arab") {
                    glyph.Features &= ~15u;
                    glyph.Features |= 1u << (int)forms[cluster.Start];
                }
                current.Glyphs.Add(glyph);
            }
        }
        var runLevels = new byte[runs.Count];
        for (var i = 0; i < runs.Count; i++) runLevels[i] = runs[i].Level;
        var order = UnicodeBidi.VisualOrder(runLevels); var output = new List<ShapedGlyph>(codePoints.Count);
        foreach (var r in order) FinishRun(runs[r], output);
        return output.ToArray();
    }
    private static void FinishRun(FontRun run, List<ShapedGlyph> output) {
        var face = run.Face; var glyphs = run.Glyphs; var layout = face.Layout;
        if (run.Script == "arab") foreach (var glyph in glyphs) glyph.SkipForSubstitution = glyph.CodePoint == 0x200d;
        var positioned = layout.HasLayout(run.Script);
        if (positioned) {
            layout.Apply(glyphs, run.Script, CommonSubstitution, required: true);
            if (run.Script == "arab") layout.Apply(glyphs, run.Script, ArabicForms);
            layout.Apply(glyphs, run.Script, StandardSubstitution);
        }
        glyphs.RemoveAll(glyph => glyph.Ignorable);
        foreach (var glyph in glyphs) glyph.XAdvance = face.AdvanceWidth(glyph.Glyph);
        if (positioned) {
            if (!layout.HasFeature(run.Script, "kern", positioning: true)) {
                for (var i = 1; i < glyphs.Count; i++) glyphs[i - 1].XAdvance += face.Kerning(glyphs[i - 1].Glyph, glyphs[i].Glyph);
            }
            layout.Apply(glyphs, run.Script, StandardPositioning, positioning: true, required: true, rightToLeft: (run.Level & 1) != 0);
        }
        if ((run.Level & 1) != 0) ReverseClusters(glyphs);
        if (positioned) OpenTypeLayout.ResolveAttachments(glyphs);
        var advanceY = 0.0;
        foreach (var glyph in glyphs) {
            output.Add(new ShapedGlyph(face, glyph.Glyph, glyph.Cluster,
                positioned ? glyph.XAdvance : null, glyph.XOffset, glyph.YOffset + advanceY));
            advanceY += glyph.YAdvance;
        }
    }
    private static void ReverseClusters(List<LayoutGlyph> glyphs) {
        var reversed = new List<LayoutGlyph>(glyphs.Count);
        for (var last = glyphs.Count - 1; last >= 0;) {
            var first = last;
            while (first > 0 && glyphs[first - 1].Cluster == glyphs[last].Cluster) first--;
            for (var i = first; i <= last; i++) reversed.Add(glyphs[i]);
            last = first - 1;
        }
        glyphs.Clear(); glyphs.AddRange(reversed);
    }
    private static string ScriptOf(int cp) => OpenTypeScriptData.Script(cp);
    private sealed class FontRun {
        internal FontRun(TrueTypeFont face, string script, int owner, byte level) { Face = face; Script = script; Owner = owner; Level = level; }
        internal readonly TrueTypeFont Face;
        internal readonly string Script;
        internal readonly int Owner;
        internal readonly byte Level;
        internal readonly List<LayoutGlyph> Glyphs = new();
    }
}

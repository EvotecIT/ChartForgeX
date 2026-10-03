using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class ScriptShaper {
    private static void ShapeThai(TrueTypeFont face, List<LayoutGlyph> glyphs, string tag, bool lao, OpenTypeLayout.LayoutExecution budget) {
        var originalCount = glyphs.Count;
        var am = lao ? 0x0eb3 : 0x0e33; var ring = lao ? 0x0ecd : 0x0e4d; var aa = lao ? 0x0eb2 : 0x0e32;
        var expanded = new List<LayoutGlyph>(glyphs.Count);
        foreach (var glyph in glyphs) {
            if (glyph.CodePoint != am || !face.HasGlyph(ring) || !face.HasGlyph(aa)) { expanded.Add(glyph); continue; }
            var at = expanded.Count;
            while (at > 0 && ThaiTone(expanded[at - 1].CodePoint, lao)) at--;
            expanded.Insert(at, new LayoutGlyph(face.MapGlyph(ring), ring, glyph.Cluster));
            expanded.Add(new LayoutGlyph(face.MapGlyph(aa), aa, glyph.Cluster));
        }
        glyphs.Clear(); glyphs.AddRange(expanded);
        // Keep standalone marks font-driven, like native SVG text; AM alone requires decomposition.
        budget.AccountGrowth(glyphs.Count - originalCount);
        face.Layout.Apply(glyphs, tag, Common, required: true, budget: budget);
        face.Layout.Apply(glyphs, tag, new[] { "rlig", "liga", "clig", "calt" }, budget: budget);
    }
    private static bool ThaiTone(int cp, bool lao) => lao ? cp >= 0x0ec8 && cp <= 0x0ecb : cp >= 0x0e48 && cp <= 0x0e4b;
}

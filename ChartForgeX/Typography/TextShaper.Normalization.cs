using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Typography;

internal static partial class TextShaper {
    private static readonly List<KeyValuePair<int, int[]>> HebrewCompositions = HebrewCanonicalCompositions();

    // Hebrew presentation forms have canonical decompositions but are excluded from NFC composition.
    // Some text faces use those glyphs instead of GSUB for shin dots and dagesh. Use a covered,
    // canonically equivalent glyph while keeping unrelated vowels available for GPOS attachment.
    private static void ComposeHebrew(Cluster cluster) {
        var output = cluster.Output;
        if (output.Count < 2 || output[0] < 0x0590 || output[0] > 0x05ff) return;
        KeyValuePair<int, int[]>? best = null; List<int>? matched = null;
        foreach (var entry in HebrewCompositions) {
            if (entry.Value[0] != output[0] || !cluster.Face!.HasGlyph(entry.Key) || best.HasValue && best.Value.Value.Length >= entry.Value.Length) continue;
            var positions = new List<int>(); var from = 1;
            for (var i = 1; i < entry.Value.Length; i++) {
                var found = output.IndexOf(entry.Value[i], from);
                if (found < 0) { positions.Clear(); break; }
                positions.Add(found); from = found + 1;
            }
            if (positions.Count != entry.Value.Length - 1) continue;
            best = entry; matched = positions;
        }
        if (!best.HasValue) return;
        output[0] = best.Value.Key;
        for (var i = matched!.Count - 1; i >= 0; i--) output.RemoveAt(matched[i]);
    }
    private static List<KeyValuePair<int, int[]>> HebrewCanonicalCompositions() {
        var result = new List<KeyValuePair<int, int[]>>();
        for (var cp = 0xfb1d; cp <= 0xfb4f; cp++) {
            var normalized = ((char)cp).ToString().Normalize(NormalizationForm.FormD);
            if (normalized.Length <= 1) continue;
            var points = new int[normalized.Length];
            for (var i = 0; i < points.Length; i++) points[i] = normalized[i];
            result.Add(new KeyValuePair<int, int[]>(cp, points));
        }
        return result;
    }
}

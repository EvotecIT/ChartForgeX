using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class TextShaper {
    /// <summary>Uses the incumbent shape unchanged at zero; tracked copies retain real cluster/attachment boundaries.</summary>
    internal static IReadOnlyList<ShapedGlyph> ShapeWithLetterSpacing(TrueTypeFont face, string text, double size,
        double letterSpacing, bool strictPrimary = false, CapturedClusters? capture = null) {
        if (letterSpacing == 0) return Shape(face, text, size, strictPrimary, capture);
        TextStyle.ValidateLetterSpacing(letterSpacing);
        var count = 0; for (var index = 0; index < text.Length;) { TrueTypeFont.ReadCodePoint(text, ref index); count++; }
        var spacings = new double[count]; var sizes = new double[count];
        for (var index = 0; index < count; index++) { spacings[index] = letterSpacing; sizes[index] = size; }
        // Numeric tracking is not another unbounded shaping-cache dimension. Existing zero caches remain exact.
        var shaped = ShapeCore(face, text, NormalizePixelSize(size), strictPrimary: strictPrimary,
            capturedClusters: capture, letterSpacings: spacings);
        return ApplyLetterSpacing(shaped, sizes, spacings);
    }

    private static bool HasLetterSpacing(IReadOnlyList<double> spacings) {
        var tracked = false;
        foreach (var value in spacings) { TextStyle.ValidateLetterSpacing(value); tracked |= value != 0; }
        return tracked;
    }

    private static IReadOnlyList<ShapedGlyph> ApplyLetterSpacing(IReadOnlyList<ShapedGlyph> glyphs,
        IReadOnlyList<double> sizes, IReadOnlyList<double> spacings) {
        if (glyphs.Count == 0) return glyphs;
        var output = new ShapedGlyph[glyphs.Count]; var pen = 0d; ShapedGlyph? previous = null;
        var groupEnds = new Dictionary<int, int>();
        for (var index = 0; index < glyphs.Count; index++) groupEnds[glyphs[index].TrackingGroup] = index + 1;
        for (var first = 0; first < glyphs.Count;) {
            // A reordered attachment can enclose another group. Keep that complete visual interval together.
            var last = groupEnds[glyphs[first].TrackingGroup];
            for (var index = first + 1; index < last; index++) last = Math.Max(last, groupEnds[glyphs[index].TrackingGroup]);
            var advance = 0d;
            for (var index = first; index < last; index++) {
                var glyph = glyphs[index]; output[index] = glyph;
                advance += TrueTypeFont.GlyphAdvance(glyph, previous, sizes[glyph.SourceIndex]); previous = glyph;
            }
            var tail = glyphs[last - 1]; var gap = last < glyphs.Count ? spacings[tail.SourceIndex] : 0;
            if (gap != 0) {
                ValidateProgression(advance + gap);
                var units = gap / tail.Face.ScaleFor(sizes[tail.SourceIndex]);
                if (!Finite(units)) throw new ArgumentOutOfRangeException(nameof(spacings), "Letter spacing exceeds the native font-unit range.");
                output[last - 1] = tail.WithTrailingAdvance(units);
            }
            pen += advance + gap;
            if (!Finite(pen)) throw new ArgumentOutOfRangeException(nameof(spacings), "Tracked text advance must remain finite.");
            first = last;
        }
        return Array.AsReadOnly(output);
    }

    /// <summary>Retains the existing segmentation and actual Arabic joining components, without resolving any font.</summary>
    private static int[] SourceTrackingGroups(List<int> points, List<Cluster> clusters) {
        var groups = new int[points.Count];
        var forms = ArabicShaping.MayJoin(points) ? ArabicShaping.ResolveForms(points) : null;
        var previous = -1;
        foreach (var cluster in clusters) {
            var group = cluster.Start;
            if (forms != null && previous >= 0 && (forms[cluster.Start] == ArabicForm.Final || forms[cluster.Start] == ArabicForm.Medial))
                group = groups[previous];
            for (var index = cluster.Start; index < cluster.Start + cluster.Count; index++) groups[index] = group;
            for (var index = cluster.Start; index < cluster.Start + cluster.Count; index++)
                if (!IsIgnorable(points[index])) { previous = cluster.Start; break; }
        }
        return groups;
    }

    internal static int[] SourceTrackingGroups(string text) {
        var points = new List<int>();
        for (var index = 0; index < text.Length;) points.Add(TrueTypeFont.ReadCodePoint(text, ref index));
        return SourceTrackingGroups(points, Segment(points, null));
    }

    // Attachments and required ligatures are already known here, before their identities leave the shaping owner.
    private static int[] RunTrackingGroups(List<LayoutGlyph> glyphs, int[] sourceGroups) {
        var parents = new int[glyphs.Count]; var indices = new Dictionary<LayoutGlyph, int>(); var groups = new Dictionary<int, int>();
        for (var index = 0; index < glyphs.Count; index++) { parents[index] = index; indices[glyphs[index]] = index; }
        int Find(int value) { while (parents[value] != value) { parents[value] = parents[parents[value]]; value = parents[value]; } return value; }
        void Join(int first, int second) { first = Find(first); second = Find(second); if (first != second) parents[second] = first; }
        for (var index = 0; index < glyphs.Count; index++) {
            var group = sourceGroups[glyphs[index].Cluster];
            if (groups.TryGetValue(group, out var first)) Join(first, index); else groups[group] = index;
            if (glyphs[index].Attachment is {} parent && indices.TryGetValue(parent, out var attached)) Join(index, attached);
        }
        var minimum = new Dictionary<int, int>();
        for (var index = 0; index < glyphs.Count; index++) {
            var component = Find(index); var source = sourceGroups[glyphs[index].Cluster];
            minimum[component] = minimum.TryGetValue(component, out var prior) ? Math.Min(prior, source) : source;
        }
        var result = new int[glyphs.Count];
        for (var index = 0; index < result.Length; index++) result[index] = minimum[Find(index)];
        return result;
    }

    internal static double EstimateTrackedWidth(string text, double ordinaryWidth, double spacing) {
        if (spacing == 0 || text.Length == 0) return ordinaryWidth;
        var groups = SourceTrackingGroups(text); var count = 0; var previous = -1;
        foreach (var group in groups) if (group != previous) { count++; previous = group; }
        if (count < 2) return ordinaryWidth;
        ValidateProgression(ordinaryWidth / count + spacing);
        var width = ordinaryWidth + (count - 1) * spacing; ValidateProgression(width); return width;
    }
    internal static void ValidateProgression(double advance) {
        if (!Finite(advance) || advance < 0) throw new ArgumentOutOfRangeException("letterSpacing", "Letter spacing must retain finite nonnegative group progression.");
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

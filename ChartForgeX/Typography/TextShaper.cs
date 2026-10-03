using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>One glyph of shaped text: the face that draws it and its glyph id, in visual order.</summary>
internal readonly struct ShapedGlyph {
    public ShapedGlyph(TrueTypeFont face, ushort glyph, int sourceIndex = 0, double? advance = null, double offsetX = 0, double offsetY = 0) {
        Face = face;
        Glyph = glyph;
        SourceIndex = sourceIndex;
        Advance = advance; OffsetX = offsetX; OffsetY = offsetY;
    }

    public TrueTypeFont Face { get; }
    public ushort Glyph { get; }
    /// <summary>Index of the logical source cluster in Unicode code points, before visual reordering.</summary>
    public int SourceIndex { get; }
    /// <summary>Font-unit advance after layout; null retains the unshaped face's legacy pair kerning.</summary>
    public double? Advance { get; }
    public double OffsetX { get; }
    public double OffsetY { get; }
}

/// <summary>
/// Turns one line of text into positioned-by-advance glyphs for measuring and drawing, so both
/// always agree. The text is split into clusters (a base character with its combining marks,
/// joiners, and variation selectors); each cluster is drawn by the first face that covers all of
/// it (composed to NFC first when that helps), trying the primary face and then its
/// <see cref="FontFallbackChain"/>. Arabic letters take their contextual forms, and a line with
/// right-to-left text is reordered by <see cref="UnicodeBidi"/> with a left-to-right paragraph
/// level, as SVG and CSS text default to, with mirrored brackets. Default-ignorable characters
/// (joiners, bidi controls, variation selectors, soft hyphens) draw nothing.
/// </summary>
internal static partial class TextShaper {
    private const int MaximumCachedRuns = 1024;
    private const int MaximumCachedTextLength = 4096;
    private const int MaximumCachedGlyphs = 65536;
    private static readonly ConditionalWeakTable<TrueTypeFont, RunCache> Caches = new();

    /// <summary>
    /// True for code points that a face covering them draws exactly as written: no fallback,
    /// reordering, joining, composition, or hiding. Text made only of these skips shaping.
    /// </summary>
    internal static bool IsSimple(int cp) =>
        (cp < 0x0300 && cp != 0x00AD) || (cp >= 0x0370 && cp < 0x0483) || (cp >= 0x048A && cp < 0x0590) ||
        (cp >= 0x1E00 && cp < 0x200B) || (cp >= 0x2010 && cp < 0x2028) || (cp >= 0x2030 && cp < 0x205F) ||
        (cp >= 0x20A0 && cp < 0x20D0) || (cp >= 0x2100 && cp < 0x2400) || (cp >= 0x2500 && cp < 0x2600);

    /// <summary>The glyphs of <paramref name="text"/> in visual order, cached per face.</summary>
    internal static IReadOnlyList<ShapedGlyph> Shape(TrueTypeFont primary, string text) {
        if (text.Length > MaximumCachedTextLength) return ShapeCore(primary, text);
        var cache = Caches.GetValue(primary, _ => new RunCache());
        var version = FontFallbackChain.Version;
        lock (cache) {
            if (cache.Version != version) {
                cache.Runs.Clear();
                cache.GlyphCount = 0;
                cache.Version = version;
            }

            if (cache.Runs.TryGetValue(text, out var cached)) return cached;
        }

        var shaped = ShapeCore(primary, text);
        if (shaped.Length > MaximumCachedGlyphs) return shaped;
        lock (cache) {
            if (cache.Version == version) {
                if (cache.Runs.TryGetValue(text, out var concurrent)) return concurrent;
                if (cache.Runs.Count >= MaximumCachedRuns || cache.GlyphCount + shaped.Length > MaximumCachedGlyphs) { cache.Runs.Clear(); cache.GlyphCount = 0; }
                cache.Runs[text] = shaped;
                cache.GlyphCount += shaped.Length;
            }
        }

        return shaped;
    }

    /// <summary>Shapes one complete text chunk while retaining the primary face of each logical code point.</summary>
    internal static IReadOnlyList<ShapedGlyph> ShapeStyled(string text, IReadOnlyList<TrueTypeFont> faces, IReadOnlyList<int> owners) =>
        ShapeCore(faces[0], text, faces, owners);

    private static ShapedGlyph[] ShapeCore(TrueTypeFont primary, string text, IReadOnlyList<TrueTypeFont>? faces = null, IReadOnlyList<int>? owners = null) {
        if (faces == null && TryShapeAscii(primary, text, out var simple)) return simple;
        var codePoints = new List<int>(text.Length);
        for (var index = 0; index < text.Length;) codePoints.Add(TrueTypeFont.ReadCodePoint(text, ref index));
        var clusters = Segment(codePoints, owners);
        FontFallbackChain? chain = null;
        foreach (var cluster in clusters) {
            if (faces != null) { primary = faces[cluster.Start]; chain = null; }
            AssignFace(primary, ref chain, codePoints, cluster);
            ComposeHebrew(cluster);
            RetainJoiners(codePoints, cluster);
        }
        if (ArabicShaping.MayJoin(codePoints)) Join(clusters);

        byte[]? levels = null;
        if (UnicodeBidi.NeedsResolution(codePoints)) {
            levels = UnicodeBidi.ResolveLevels(codePoints, 0);
            for (var i = 0; i < clusters.Count; i++) {
                var cluster = clusters[i];
                if ((levels[cluster.Start] & 1) == 1 && cluster.Output.Count > 0) Mirror(cluster);
            }
        }

        return ShapeFontRuns(codePoints, clusters, levels);
    }

    // A base character and what attaches to it: marks, joiners and what they join, variation selectors, emoji modifiers, tags.
    private static List<Cluster> Segment(List<int> codePoints, IReadOnlyList<int>? owners) {
        var clusters = new List<Cluster>();
        Cluster? current = null;
        for (var i = 0; i < codePoints.Count; i++) {
            var cp = codePoints[i];
            var arabicJoiner = cp == 0x200D &&
                ((current != null && ArabicShaping.IsJoiningLetter(current.First)) ||
                 (i + 1 < codePoints.Count && ArabicShaping.IsJoiningLetter(codePoints[i + 1])));
            var joinsPrevious = current != null && !arabicJoiner && (owners == null || owners[i] == owners[i - 1]) &&
                (Extends(cp) || (codePoints[i - 1] == 0x200D && current.First != 0x200D && !ArabicShaping.IsJoiningLetter(cp)));
            if (!joinsPrevious) {
                current = new Cluster(i, cp, owners == null ? 0 : owners[i]);
                clusters.Add(current);
            }

            current!.Count++;
        }

        return clusters;
    }

    private static void AssignFace(TrueTypeFont primary, ref FontFallbackChain? chain, List<int> codePoints, Cluster cluster) {
        var visible = new List<int>(cluster.Count);
        for (var i = cluster.Start; i < cluster.Start + cluster.Count; i++) if (!IsIgnorable(codePoints[i])) visible.Add(codePoints[i]);
        cluster.Face = primary;
        if (visible.Count == 0) return;
        cluster.Base = visible[0];
        var emoji = WantsEmoji(codePoints, cluster);
        if (visible.Count == 1) {
            cluster.Output.Add(visible[0]);
            if (emoji || !primary.HasGlyph(visible[0])) {
                var fallback = (chain ??= FontFallbackChain.For(primary)).FaceFor(visible[0], emoji);
                if (fallback != null || !primary.HasGlyph(visible[0])) cluster.Face = fallback ?? primary;
            }

            return;
        }

        // A base with marks: the composed character when a face has it, otherwise the sequence, in one face.
        var composed = Compose(visible);
        if (!emoji && composed != null && Covers(primary, composed)) { cluster.Output.AddRange(composed); return; }
        if (!emoji && Covers(primary, visible)) { cluster.Output.AddRange(visible); return; }
        chain ??= FontFallbackChain.For(primary);
        var face = chain.FaceForCluster(composed, visible, emoji, out var useComposed);
        if (face != null) {
            cluster.Face = face;
            cluster.Output.AddRange(useComposed ? composed! : visible);
            return;
        }

        // With no face for the whole cluster, the face of its base draws it; uncovered marks draw nothing.
        cluster.Face = primary.HasGlyph(visible[0]) ? primary : chain.FaceFor(visible[0]) ?? primary;
        cluster.Output.AddRange(visible);
    }

    // Emoji presentation: asked for with U+FE0F, or the default for pictographs, unless U+FE0E asks for text.
    private static bool WantsEmoji(List<int> codePoints, Cluster cluster) {
        var requested = false;
        for (var i = cluster.Start; i < cluster.Start + cluster.Count; i++) {
            if (codePoints[i] == 0xFE0E) return false;
            if (codePoints[i] == 0xFE0F) requested = true;
        }

        var cp = cluster.Base;
        return requested || (cp >= 0x1F000 && cp <= 0x1FAFF) || cp == 0x2705 || cp == 0x274C || cp == 0x2728 || cp == 0x2B50 || cp == 0x2B55 ||
            cp == 0x231A || cp == 0x231B || (cp >= 0x23E9 && cp <= 0x23EC) || cp == 0x23F0 || cp == 0x23F3 || cp == 0x26A1 || cp == 0x26D4 ||
            (cp >= 0x2795 && cp <= 0x2797) || cp == 0x27B0 || cp == 0x27BF || (cp >= 0x2753 && cp <= 0x2757);
    }

    private static bool Covers(TrueTypeFont face, IReadOnlyList<int> codePoints) {
        foreach (var cp in codePoints) if (!face.HasGlyph(cp)) return false;
        return true;
    }

    private static List<int>? Compose(List<int> visible) {
        var builder = new StringBuilder();
        foreach (var cp in visible) builder.Append(char.ConvertFromUtf32(cp));
        string composed;
        try {
            composed = builder.ToString().Normalize(NormalizationForm.FormC);
        } catch (ArgumentException) {
            return null;
        }

        var result = new List<int>();
        for (var index = 0; index < composed.Length;) result.Add(TrueTypeFont.ReadCodePoint(composed, ref index));
        if (result.Count != visible.Count) return result;
        for (var i = 0; i < result.Count; i++) if (result[i] != visible[i]) return result;
        return null;
    }

    // Arabic contextual forms over the clusters' base letters, with the lam-alef ligature.
    private static void Join(List<Cluster> clusters) {
        var letters = new int[clusters.Count];
        for (var i = 0; i < letters.Length; i++) letters[i] = clusters[i].Base == 0 ? clusters[i].First : clusters[i].Base;
        var forms = ArabicShaping.ResolveForms(letters);
        for (var i = 0; i < clusters.Count; i++) {
            var cluster = clusters[i];
            if (cluster.Output.Count == 0) continue;
            if (UsesArabicLayout(cluster.Face!)) continue;
            if (cluster.Base == 0x0644 && i + 1 < clusters.Count) {
                var alef = clusters[i + 1];
                var ligature = ArabicShaping.LamAlef(alef.Base, forms[i] == ArabicForm.Final || forms[i] == ArabicForm.Medial);
                if (ligature >= 0 && alef.Output.Count > 0 && alef.Owner == cluster.Owner && ReferenceEquals(alef.Face, cluster.Face) && cluster.Face!.HasGlyph(ligature)) {
                    cluster.Output[0] = ligature;
                    for (var m = 1; m < alef.Output.Count; m++) cluster.Output.Add(alef.Output[m]);
                    alef.Output.Clear();
                    continue;
                }
            }

            var form = ArabicShaping.PresentationForm(cluster.Base, forms[i]);
            if (form != cluster.Base && cluster.Face!.HasGlyph(form)) cluster.Output[0] = form;
        }
    }

    private static void Mirror(Cluster cluster) {
        var mirrored = BidiCharacterData.Mirror(cluster.Output[0]);
        if (mirrored != cluster.Output[0] && cluster.Face!.HasGlyph(mirrored)) cluster.Output[0] = mirrored;
    }

    internal static bool Extends(int cp) {
        if (cp == 0x200D || (cp >= 0xFE00 && cp <= 0xFE0F) || (cp >= 0x1F3FB && cp <= 0x1F3FF) || (cp >= 0xE0020 && cp <= 0xE007F) || (cp >= 0xE0100 && cp <= 0xE01EF)) return true;
        var category = BidiCharacterData.Category(cp);
        return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark;
    }

    /// <summary>Default-ignorable code points: drawn as nothing, with no advance.</summary>
    internal static bool IsIgnorable(int cp) =>
        cp == 0x00AD || cp == 0x034F || cp == 0x061C || cp == 0x115F || cp == 0x1160 || cp == 0x17B4 || cp == 0x17B5 ||
        (cp >= 0x180B && cp <= 0x180F) || (cp >= 0x200B && cp <= 0x200F) || (cp >= 0x202A && cp <= 0x202E) || (cp >= 0x2060 && cp <= 0x206F) ||
        cp == 0x3164 || (cp >= 0xFE00 && cp <= 0xFE0F) || cp == 0xFEFF || cp == 0xFFA0 || (cp >= 0xFFF0 && cp <= 0xFFF8) ||
        (cp >= 0x1BCA0 && cp <= 0x1BCA3) || (cp >= 0x1D173 && cp <= 0x1D17A) || (cp >= 0xE0000 && cp <= 0xE0FFF);

    private sealed class Cluster {
        public Cluster(int start, int first, int owner) {
            Start = start;
            First = first;
            Owner = owner;
        }

        public int Start { get; }
        public int Owner { get; }
        /// <summary>The first code point, which decides joining when nothing in the cluster is drawn.</summary>
        public int First { get; }
        public int Count { get; set; }
        public int Base { get; set; }
        public TrueTypeFont? Face { get; set; }
        public List<int> Output { get; } = new();
    }

    private sealed class RunCache {
        public int Version { get; set; }
        public int GlyphCount { get; set; }
        public Dictionary<string, ShapedGlyph[]> Runs { get; } = new(StringComparer.Ordinal);
    }
}

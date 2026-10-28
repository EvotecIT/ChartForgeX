using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Partitions one captured simple run at its existing glyph-cluster boundaries, never reshaping fragments.</summary>
internal static class VisualArcTextRuns {
    internal const int MaximumClusters = 256;

    internal static bool TryCreate(VisualScenePreparedText text, out IReadOnlyList<VisualArcTextRun> runs, out string reason) {
        runs = Array.Empty<VisualArcTextRun>(); reason = "unsupported-text";
        if (text.Lines.Count != 1 || text.Face.Font == null) return false;
        var style = text.Style;
        if (style.Baseline != TextBaseline.Normal || style.UnderlineStyle != TextDecorationStyle.None
            || style.StrikethroughStyle != TextDecorationStyle.None) { reason = "decorated-text"; return false; }
        var line = text.Lines[0]; var source = line.Text; var glyphs = line.Glyphs;
        if (source.Length == 0 || source.Length > MaximumClusters || glyphs.Count == 0 || glyphs.Count > MaximumClusters) return false;
        // Joining, bidi, combining and supplementary text retain the incumbent whole-run placement.
        // These safe BMP code points have identical UTF-16 and logical code-point indices.
        foreach (var character in source) if (character < ' ' || !TextShaper.IsSimple(character)) return false;
        for (var index = 0; index < glyphs.Count; index++)
            if (glyphs[index].Glyph == 0 || glyphs[index].SourceIndex < 0 || glyphs[index].SourceIndex >= source.Length
                || index > 0 && glyphs[index].SourceIndex < glyphs[index - 1].SourceIndex) return false;
        if (glyphs[0].SourceIndex != 0) return false;
        var output = new List<VisualArcTextRun>(); var pen = 0d; ShapedGlyph? previous = null;
        for (var first = 0; first < glyphs.Count;) {
            var next = first + 1;
            while (next < glyphs.Count && glyphs[next].SourceIndex == glyphs[first].SourceIndex) next++;
            var start = glyphs[first].SourceIndex; var end = next == glyphs.Count ? source.Length : glyphs[next].SourceIndex;
            if (end <= start) return false;
            var copy = new ShapedGlyph[next - first];
            for (var index = first; index < next; index++) copy[index - first] = glyphs[index];
            var kerning = TrueTypeFont.GlyphAdvance(glyphs[first], previous, text.Size) - TrueTypeFont.GlyphAdvance(glyphs[first], null, text.Size);
            var offset = pen + kerning;
            var width = TrueTypeFont.MeasureGlyphs(copy, text.Size);
            for (var index = first; index < next; index++) {
                pen += TrueTypeFont.GlyphAdvance(glyphs[index], previous, text.Size); previous = glyphs[index];
            }
            var fragment = new VisualScenePreparedText(text.Face, text.Style, text.Size, text.Ascent,
                new[] { new VisualSceneTextLine(source.Substring(start, end - start), width, copy) },
                new TextMetrics(width, text.Metrics.LineHeight, text.Metrics.LineHeight));
            var ink = text.Face.Font.MeasureGlyphInk(copy, text.Size, text.Face.SynthesizeItalic);
            if (ink.HasValue) {
                var box = ink.Value; var bold = text.Face.SynthesizeBold && text.Face.Font.NeedsSyntheticBold(copy)
                    ? Math.Max(.6, text.Size / 18) : 0;
                ink = new ChartRect(box.X - bold, box.Y - text.Ascent, box.Width + bold * 2, box.Height);
            }
            output.Add(new VisualArcTextRun(fragment, offset, width, ink)); first = next;
        }
        if (pen <= 0 || text.Metrics.Width <= 0) return false;
        runs = output; reason = string.Empty; return true;
    }
}

/// <summary>One immutable source cluster, with full-run positioning and its actual captured native ink bounds.</summary>
internal sealed class VisualArcTextRun {
    internal VisualArcTextRun(VisualScenePreparedText text, double offset, double width, ChartRect? ink) {
        Text = text; Offset = offset; Width = width; Ink = ink;
    }
    internal VisualScenePreparedText Text { get; }
    internal double Offset { get; }
    internal double Width { get; }
    /// <summary>Native ink bounds relative to the run's left baseline origin, before rotation.</summary>
    internal ChartRect? Ink { get; }
}

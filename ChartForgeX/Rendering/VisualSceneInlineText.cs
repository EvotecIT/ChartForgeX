using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Captures one native base face policy, then shapes complete inline lines through the existing multi-face shaper.</summary>
internal sealed class VisualSceneInlineTextContext {
    private readonly TextStyle _baseStyle;
    private readonly VisualSceneTextCapture? _capture;
    private readonly Dictionary<TextInlineEmphasis, VisualSceneTextFace> _faces = new();
    internal VisualSceneInlineTextContext(TextStyle style, VisualSceneTextCapture? capture = null) {
        _baseStyle = (style ?? throw new ArgumentNullException(nameof(style))).Clone();
        _baseStyle.TextCase = TextCaseTransform.None; _capture = capture ?? _baseStyle.Font.NativeTextCapture;
    }
    internal static double Baseline(TextLayout layout, VisualSceneInlineTextLine line, int row, double y, double height, bool centered) {
        var top = y + (centered ? Math.Max(0, height - layout.Metrics.Height) / 2 : 0);
        return top + row * layout.Metrics.LineHeight + line.Ascent;
    }

    private VisualSceneTextFace Face(TextInlineEmphasis emphasis) {
        if (!_faces.TryGetValue(emphasis, out var face)) {
            var style = TextContentStyle.Resolve(_baseStyle, emphasis); face = _capture?.Bind(style) ?? new VisualSceneTextFace(style); _faces.Add(emphasis, face);
        }
        return face;
    }
    internal VisualSceneInlineTextLine Prepare(TextContent content) => Prepare(content, null);
    /// <summary>Retains explicit paint ownership with the incumbent whole-chunk bidi/script shaping.</summary>
    internal VisualSceneInlineTextLine Prepare(TextContent content, IReadOnlyList<TextContentRun>? paintRuns) {
        var faces = new List<TrueTypeFont>(); var owners = new List<int>(); var sizes = new List<double>();
        var spacings = new List<double>();
        var runs = paintRuns ?? content.Runs; var captures = new List<VisualSceneTextFace>(); var clusterOwners = new List<TextShaper.CapturedClusters?>();
        var outline = true; var height = 0d; var ascent = 0d;
        for (var owner = 0; owner < runs.Count; owner++) {
            var face = Face(runs[owner].Emphasis); captures.Add(face); var style = face.CapturedStyle;
            var metrics = face.Prepare("Ag", style.EffectiveFontSize).Metrics;
            height = Math.Max(height, metrics.LineHeight); ascent = Math.Max(ascent, face.Ascent(style.EffectiveFontSize));
            if (face.CapturedFace.Font == null) outline = false;
        }
        var sourceOwner = 0; var ownerEnd = runs.Count == 0 ? 0 : runs[0].Text.Length;
        for (var index = 0; index < content.PlainText.Length;) {
            while (index >= ownerEnd && sourceOwner + 1 < runs.Count) { sourceOwner++; ownerEnd += runs[sourceOwner].Text.Length; }
            TrueTypeFont.ReadCodePoint(content.PlainText, ref index);
            var capture = captures[sourceOwner]; faces.Add(capture.CapturedFace.Font!); owners.Add(sourceOwner); sizes.Add(capture.CapturedStyle.EffectiveFontSize); clusterOwners.Add(capture.CapturedClusters);
            spacings.Add(capture.CapturedStyle.LetterSpacing);
        }
        if (runs.Count == 0) {
            var face = Face(TextInlineEmphasis.Normal); var style = face.CapturedStyle;
            var blank = face.Prepare(string.Empty, style.EffectiveFontSize);
            return new VisualSceneInlineTextLine(content, Array.Empty<VisualSceneInlineTextPiece>(), blank.Metrics.LineHeight, blank.Ascent, 0);
        }
        if (!outline) return BitmapFallback(content, runs, captures, height, ascent);
        var glyphs = TextShaper.ShapeStyled(content.PlainText, faces, owners, sizes, _capture == null ? null : clusterOwners, spacings);
        var pieces = new List<VisualSceneInlineTextPiece>(); var pen = 0d; var first = 0; ShapedGlyph? previous = null;
        while (first < glyphs.Count) {
            var owner = owners[glyphs[first].SourceIndex]; var last = first + 1;
            while (last < glyphs.Count && owners[glyphs[last].SourceIndex] == owner) last++;
            var capture = captures[owner]; var style = capture.CapturedStyle; var face = capture.CapturedFace; var size = style.EffectiveFontSize;
            var copy = new ShapedGlyph[last - first];
            var adjustment = TrueTypeFont.GlyphAdvance(glyphs[first], previous, size) - TrueTypeFont.GlyphAdvance(glyphs[first], null, size);
            pen += adjustment; var origin = pen;
            for (var index = first; index < last; index++) {
                var glyph = glyphs[index]; copy[index - first] = glyph;
                pen += TrueTypeFont.GlyphAdvance(glyph, index == first ? null : previous, size); previous = glyph;
            }
            var width = pen - origin;
            if (face.SynthesizeBold && face.Font!.NeedsSyntheticBold(copy)) { var extra = RgbaCanvas.EmphasisOffset(size); width += extra; pen += extra; }
            if (style.Font.Italic) { var extra = TrueTypeFont.ItalicOverhang(size); width += extra; pen += extra; }
            var logical = runs[owner].Text;
            var prepared = new VisualScenePreparedText(face, style, size, capture.Ascent(size),
                new[] { new VisualSceneTextLine(logical, width, copy) }, new TextMetrics(width, height, height), nativeGlyphInk: true);
            capture.RecordPrepared(prepared); pieces.Add(new VisualSceneInlineTextPiece(prepared, origin, owner)); first = last;
        }
        return new VisualSceneInlineTextLine(content, Array.AsReadOnly(pieces.ToArray()), height, ascent, pen);
    }
    private static VisualSceneInlineTextLine BitmapFallback(TextContent content, IReadOnlyList<TextContentRun> runs, List<VisualSceneTextFace> captures, double height, double ascent) {
        var pieces = new List<VisualSceneInlineTextPiece>(); var pen = 0d;
        var spacing = captures[0].CapturedStyle.LetterSpacing;
        var tracking = spacing == 0 ? null : TextShaper.SourceTrackingGroups(content.PlainText);
        var scalar = 0; var tailGroup = -1; var tailWidth = 0d;
        for (var index = 0; index < runs.Count; index++) {
            var face = captures[index]; var style = face.CapturedStyle; var prepared = face.Prepare(runs[index].Text, style.EffectiveFontSize);
            var captured = new VisualScenePreparedText(prepared.Face, prepared.Style, prepared.Size, prepared.Ascent,
                prepared.Lines, prepared.Metrics, nativeGlyphInk: true);
            pieces.Add(new VisualSceneInlineTextPiece(captured, pen, index)); pen += prepared.Metrics.Width;
            if (tracking != null && runs[index].Text.Length > 0) {
                var text = runs[index].Text; var suffix = 0; var firstGroup = tracking[scalar]; var lastGroup = firstGroup;
                for (var cursor = 0; cursor < text.Length;) {
                    var start = cursor; TrueTypeFont.ReadCodePoint(text, ref cursor);
                    if (tracking[scalar] != lastGroup) { suffix = start; lastGroup = tracking[scalar]; }
                    scalar++;
                }
                var width = suffix == 0 ? prepared.Metrics.Width : face.Prepare(text.Substring(suffix), style.EffectiveFontSize).Metrics.Width;
                tailWidth = suffix == 0 && firstGroup == tailGroup ? tailWidth + width : width; tailGroup = lastGroup;
                if (scalar < tracking.Length && tracking[scalar] != lastGroup) {
                    TextShaper.ValidateProgression(tailWidth + spacing); pen += spacing;
                }
            }
        }
        // This is the incumbent bitmap/family fallback, not a claim of cross-font or contextual-script parity.
        return new VisualSceneInlineTextLine(content, Array.AsReadOnly(pieces.ToArray()), height, ascent, pen);
    }
}

internal sealed class VisualSceneInlineTextPiece {
    internal VisualSceneInlineTextPiece(VisualScenePreparedText text, double offset, int owner = -1) { Text = text; Offset = offset; Owner = owner; }
    internal int Owner { get; }
    internal VisualScenePreparedText Text { get; }
    internal double Offset { get; }
}
internal sealed class VisualSceneInlineTextLine {
    internal VisualSceneInlineTextLine(TextContent content, IReadOnlyList<VisualSceneInlineTextPiece> pieces, double lineHeight, double ascent, double width) {
        Content = content; Pieces = pieces; LineHeight = lineHeight; Ascent = ascent; Width = width;
    }
    internal TextContent Content { get; }
    internal IReadOnlyList<VisualSceneInlineTextPiece> Pieces { get; }
    internal double LineHeight { get; }
    internal double Ascent { get; }
    internal double Width { get; }
}

using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static bool NeedsWholeChunkShaping(SvgRasterElement element) {
        var codePoints = new List<int>();
        AppendTextCodePoints(element, codePoints);
        foreach (var codePoint in codePoints) if (!TextShaper.IsSimple(codePoint)) return true;
        return false;
    }

    private static void AppendTextCodePoints(SvgRasterElement element, List<int> codePoints) {
        foreach (var content in element.Content) {
            if (content.Text != null) {
                for (var index = 0; index < content.Text.Length;) codePoints.Add(TrueTypeFont.ReadCodePoint(content.Text, ref index));
            } else if (content.Element != null && content.Element.Name == "tspan") AppendTextCodePoints(content.Element, codePoints);
        }
    }
    // Resolve shaping across style boundaries first; the existing paint traversal still owns
    // each span's transform, clipping, mask, opacity, baseline and paint order.
    private sealed class SvgTextLayout {
        private readonly List<TextChunk> _chunks = new();
        private readonly List<TextRun> _runs = new();
        private TextChunk? _chunk;
        private int _readIndex;
        private string _anchor;
        private bool _absoluteX = true;

        public SvgTextLayout(string anchor) { _anchor = anchor; }
        public bool Collecting { get; private set; } = true;

        public void BeginChunk(string anchor, bool absoluteX) {
            if (!Collecting) return;
            _chunk = null;
            _anchor = anchor;
            _absoluteX = absoluteX;
        }

        public void Add(string text, double x, double y, double advance, SvgRasterStyle style) {
            if (_chunk == null) {
                _chunk = new TextChunk(_anchor, _absoluteX);
                _chunks.Add(_chunk);
            }
            var run = new TextRun(text, x, y, advance, style);
            _chunk.Runs.Add(run);
            _runs.Add(run);
        }

        public TextRun Next() => _runs[_readIndex++];
        public void Rewind() => _readIndex = 0;

        public void Resolve() {
            Collecting = false;
            var carriedAdvance = 0.0;
            foreach (var chunk in _chunks) {
                if (chunk.AbsoluteX) carriedAdvance = 0;
                var text = new StringBuilder();
                var faces = new List<TrueTypeFont>();
                var owners = new List<TextRun>();
                var ownerIds = new List<int>();
                var pixelSizes = new List<double>();
                var canShape = true;
                var ownerId = 0;
                foreach (var run in chunk.Runs) {
                    var face = SvgTextFace(run.Style).Font;
                    if (face == null) { canShape = false; break; }
                    text.Append(run.Text);
                    for (var index = 0; index < run.Text.Length;) {
                        TrueTypeFont.ReadCodePoint(run.Text, ref index);
                        faces.Add(face);
                        owners.Add(run);
                        ownerIds.Add(ownerId);
                        pixelSizes.Add(run.Style.FontSize);
                    }
                    ownerId++;
                }
                if (!canShape) continue;

                var glyphs = TextShaper.ShapeStyled(text.ToString(), faces, ownerIds, pixelSizes);
                var first = chunk.Runs[0];
                var oldWidth = 0.0;
                var shifted = new HashSet<TextRun>();
                TextRun? previousRun = null;
                foreach (var run in chunk.Runs) {
                    run.Pieces = new List<TextPiece>();
                    run.DeltaX = previousRun == null ? 0 : run.X - (previousRun.X + previousRun.Advance);
                    oldWidth += run.Advance + run.DeltaX;
                    previousRun = run;
                }

                var x = first.X + carriedAdvance;
                TextPiece? piece = null;
                ShapedGlyph? previousGlyph = null;
                previousRun = null;
                foreach (var glyph in glyphs) {
                    var owner = owners[glyph.SourceIndex];
                    if (shifted.Add(owner)) x += owner.DeltaX;
                    var sameMetrics = previousRun != null && previousRun.Style.FontSize == owner.Style.FontSize;
                    var kerning = sameMetrics ? TrueTypeFont.GlyphAdvance(glyph, previousGlyph, owner.Style.FontSize) - TrueTypeFont.GlyphAdvance(glyph, null, owner.Style.FontSize) : 0;
                    if (!ReferenceEquals(previousRun, owner)) {
                        if (previousRun != null && NeedsPreparedBoldOffset(previousRun.Style, piece!.Glyphs)) x += TextEmphasisOffset(previousRun.Style.FontSize);
                        x += kerning;
                        piece = new TextPiece(x);
                        owner.Pieces!.Add(piece);
                    } else {
                        x += kerning;
                    }
                    piece!.Glyphs.Add(glyph);
                    x += TrueTypeFont.GlyphAdvance(glyph, null, owner.Style.FontSize);
                    previousGlyph = glyph;
                    previousRun = owner;
                }
                if (previousRun != null && NeedsPreparedBoldOffset(previousRun.Style, piece!.Glyphs)) x += TextEmphasisOffset(previousRun.Style.FontSize);
                var newWidth = x - (first.X + carriedAdvance);
                var anchorCorrection = TextAnchorOffset(chunk.Anchor, newWidth) - TextAnchorOffset(chunk.Anchor, oldWidth);
                foreach (var run in chunk.Runs) foreach (var part in run.Pieces!) part.X += anchorCorrection;
                carriedAdvance += newWidth - oldWidth + anchorCorrection;
            }
        }

        private sealed class TextChunk {
            public TextChunk(string anchor, bool absoluteX) { Anchor = anchor; AbsoluteX = absoluteX; }
            public string Anchor { get; }
            public bool AbsoluteX { get; }
            public List<TextRun> Runs { get; } = new();
        }
    }

    private sealed class TextRun {
        public TextRun(string text, double x, double y, double advance, SvgRasterStyle style) {
            Text = text; X = x; Y = y; Advance = advance; Style = style;
        }
        public string Text { get; }
        public double X { get; }
        public double Y { get; }
        public double Advance { get; }
        public SvgRasterStyle Style { get; }
        public double DeltaX { get; set; }
        public List<TextPiece>? Pieces { get; set; }
    }

    private sealed class TextPiece {
        public TextPiece(double x) { X = x; }
        public double X { get; set; }
        public List<ShapedGlyph> Glyphs { get; } = new();
    }

    private static double PreparedAdvance(string text, double size, ResolvedTypeface face, IReadOnlyList<ShapedGlyph>? glyphs) =>
        glyphs == null ? TextAdvanceWidth(text, size, face) : TrueTypeFont.MeasureGlyphs(glyphs, size) + (face.SynthesizeBold && face.Font!.NeedsSyntheticBold(glyphs) ? TextEmphasisOffset(size) : 0);

    private static bool NeedsPreparedBoldOffset(SvgRasterStyle style, IReadOnlyList<ShapedGlyph> glyphs) {
        var face = SvgTextFace(style);
        return face.SynthesizeBold && face.Font!.NeedsSyntheticBold(glyphs);
    }

    private static double PreparedPaintWidth(string text, double size, ResolvedTypeface face, bool italic, IReadOnlyList<ShapedGlyph>? glyphs) =>
        glyphs == null ? TextPaintWidth(text, size, face, italic) : PreparedAdvance(text, size, face, glyphs) + (italic ? TrueTypeFont.ItalicOverhang(size) : 0);

    private static double TextEmphasisOffset(double size) => RgbaCanvas.EmphasisOffset(size);
}

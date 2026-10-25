using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    /// <summary>One placement owner for native paint and detached monochrome export geometry.</summary>
    private struct PositionedGlyphRun {
        private readonly TrueTypeFont _owner;
        private readonly IReadOnlyList<ShapedGlyph> _glyphs;
        private readonly double _baseline, _size, _boldOffset;
        private readonly bool _italic;
        private int _index;
        private double _x;
        private ShapedGlyph? _previous;

        internal PositionedGlyphRun(TrueTypeFont owner, IReadOnlyList<ShapedGlyph> glyphs,
            double x, double baseline, double size, bool italic, double boldOffset) {
            _owner = owner; _glyphs = glyphs; _x = x; _baseline = baseline; _size = size;
            _italic = italic; _boldOffset = boldOffset; _index = 0; _previous = null;
        }

        internal bool MoveNext(out PositionedGlyph placed) {
            if (_index >= _glyphs.Count) { placed = default; return false; }
            var glyph = _glyphs[_index++];
            var scale = glyph.Face.ScaleFor(_size);
            _x += GlyphAdvance(glyph, _previous, _size) - GlyphAdvance(glyph, null, _size);
            var boldOffset = _owner.CanSynthesizeBold(glyph) ? _boldOffset : 0;
            if (boldOffset == 0) boldOffset = _owner.FallbackBoldOffset(glyph, _size);
            placed = new PositionedGlyph(glyph.Face, glyph.Glyph, _x + glyph.OffsetX * scale,
                _baseline - glyph.OffsetY * scale, scale, _italic && !glyph.Face.IsItalic, boldOffset);
            _x += GlyphAdvance(glyph, null, _size); _previous = glyph;
            return true;
        }
    }

    internal readonly struct PositionedGlyph {
        internal PositionedGlyph(TrueTypeFont face, ushort glyph, double x, double baseline,
            double scale, bool italic, double boldOffset) {
            Face = face; Glyph = glyph; X = x; Baseline = baseline; Scale = scale;
            Italic = italic; BoldOffset = boldOffset;
        }
        internal TrueTypeFont Face { get; }
        internal ushort Glyph { get; }
        internal double X { get; }
        internal double Baseline { get; }
        internal double Scale { get; }
        internal bool Italic { get; }
        internal double BoldOffset { get; }
    }

    /// <summary>Returns only captured placements eligible for monochrome outline transport.</summary>
    internal bool TryGetOutlineRun(IReadOnlyList<ShapedGlyph> glyphs, double x, double baseline,
        double size, bool italic, double boldOffset, out List<PositionedGlyph> placements, bool allowSyntheticBold = false) {
        placements = new List<PositionedGlyph>(glyphs.Count);
        var run = new PositionedGlyphRun(this, glyphs, x, baseline, size, italic, boldOffset);
        while (run.MoveNext(out var placed)) {
            if (placed.Glyph == 0) continue; // Native paint also omits the missing-glyph sentinel.
            if (!allowSyntheticBold && placed.BoldOffset != 0 || placed.Face.IsColorGlyph(placed.Glyph)
                || placed.Face._monochromeBitmaps != null) {
                placements.Clear(); return false;
            }
            placements.Add(placed);
        }
        return true;
    }

    /// <summary>The existing native outline owner at the captured logical scale; only placement remains.</summary>
    internal IReadOnlyList<List<ChartPoint>> OutlineContours(ushort glyph, double scale, bool italic) =>
        ReadGlyphContours(glyph, new FontTransform(scale, italic ? ObliqueShear * scale : 0, 0, -scale, 0, 0), 0);
}

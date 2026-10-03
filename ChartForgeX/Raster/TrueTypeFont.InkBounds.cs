using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private Dictionary<ushort, ChartRect?>? _glyphInkBounds;

    /// <summary>The painted run's bounds relative to its text origin, including positioned marks and fallback metrics.</summary>
    internal ChartRect? MeasureGlyphInk(IReadOnlyList<ShapedGlyph> glyphs, double size, bool italic) {
        var left = double.PositiveInfinity; var top = double.PositiveInfinity;
        var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
        var cursor = 0.0; var baseline = Ascent(size); ShapedGlyph? previous = null;
        foreach (var glyph in glyphs) {
            var scale = glyph.Face.ScaleFor(size); var advance = GlyphAdvance(glyph, previous, size);
            cursor += advance - GlyphAdvance(glyph, null, size);
            var box = glyph.Face.GlyphInk(glyph.Glyph);
            if (box.HasValue) {
                var bounds = box.Value; var shear = italic && !glyph.Face.IsItalic ? ObliqueShear : 0;
                var x1 = bounds.X + Math.Min(shear * bounds.Y, shear * (bounds.Y + bounds.Height));
                var x2 = bounds.X + bounds.Width + Math.Max(shear * bounds.Y, shear * (bounds.Y + bounds.Height));
                left = Math.Min(left, cursor + (glyph.OffsetX + x1) * scale);
                right = Math.Max(right, cursor + (glyph.OffsetX + x2) * scale);
                top = Math.Min(top, baseline - (glyph.OffsetY + bounds.Y + bounds.Height) * scale);
                bottom = Math.Max(bottom, baseline - (glyph.OffsetY + bounds.Y) * scale);
            }
            cursor += GlyphAdvance(glyph, null, size); previous = glyph;
        }
        return double.IsPositiveInfinity(left) ? null : new ChartRect(left, top, right - left, bottom - top);
    }
    private ChartRect? GlyphInk(ushort glyph) {
        lock (_root._viewLock) {
            _root._glyphInkBounds ??= new Dictionary<ushort, ChartRect?>();
            if (_root._glyphInkBounds.TryGetValue(glyph, out var cached)) return cached;
            var left = double.PositiveInfinity; var bottom = double.PositiveInfinity;
            var right = double.NegativeInfinity; var top = double.NegativeInfinity;
            foreach (var contour in ReadGlyphContours(glyph, new FontTransform(1, 0, 0, 1, 0, 0), 0)) foreach (var point in contour) {
                left = Math.Min(left, point.X); right = Math.Max(right, point.X); bottom = Math.Min(bottom, point.Y); top = Math.Max(top, point.Y);
            }
            ChartRect? bounds = double.IsPositiveInfinity(left) ? null : new ChartRect(left, bottom, right - left, top - bottom);
            return _root._glyphInkBounds[glyph] = bounds;
        }
    }
}

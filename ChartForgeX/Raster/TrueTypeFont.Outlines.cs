using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

/// <summary>Receives one glyph outline as moves, lines, cubic curves, and closes, in font units.</summary>
internal interface IGlyphOutlineSink {
    void MoveTo(double x, double y);
    void LineTo(double x, double y);
    void CubicTo(double x1, double y1, double x2, double y2, double x, double y);
    void Close();
}

internal sealed partial class TrueTypeFont {
    /// <summary>Reads a glyph as closed, flattened contours placed by <paramref name="transform"/>.</summary>
    private List<List<ChartPoint>> ReadGlyphContours(ushort glyph, FontTransform transform, int depth) {
        if (_compact != null) {
            var builder = new ContourBuilder(transform);
            if (glyph < _numGlyphs) _compact.DrawGlyph(glyph, builder);
            return builder.Finish();
        }

        var contours = new List<List<ChartPoint>>();
        if (_glyf < 0 || _loca < 0 || glyph >= _numGlyphs || depth > 8) return contours;
        // The left phantom sets the instance's horizontal origin. Apply it only
        // to the whole glyph; nested components retain their own placement.
        if (depth == 0 && _glyphVariations != null) transform = transform.Compose(1, 0, 0, 1, -GlyphOrigin(glyph), 0);
        var glyphStart = GlyphOffset(glyph);
        var glyphEnd = GlyphOffset((ushort)(glyph + 1));
        if (glyphStart == glyphEnd) return contours;
        var offset = _glyf + glyphStart;
        if (offset + 10 > _data.Length) return contours;
        var contourCount = ReadInt16(_data, offset);
        if (contourCount < 0) {
            ReadCompositeGlyphContours(offset, transform, depth, contours, _glyphVariations?.Get(glyph));
            return contours;
        }

        if (contourCount <= 0) return contours;

        var endPts = new ushort[contourCount];
        for (var i = 0; i < contourCount; i++) endPts[i] = ReadUInt16(_data, offset + 10 + i * 2);
        var pointCount = endPts[contourCount - 1] + 1;
        var instructionLengthOffset = offset + 10 + contourCount * 2;
        var instructionLength = ReadUInt16(_data, instructionLengthOffset);
        var p = instructionLengthOffset + 2 + instructionLength;
        var flags = new byte[pointCount];
        for (var i = 0; i < pointCount; i++) {
            var flag = _data[p++];
            flags[i] = flag;
            if ((flag & 8) == 0) continue;
            var repeat = _data[p++];
            for (var r = 0; r < repeat && i + 1 < pointCount; r++) flags[++i] = flag;
        }

        var xs = new short[pointCount];
        DecodeCoordinates(_data, flags, xs, ref p, true);
        var ys = new short[pointCount];
        DecodeCoordinates(_data, flags, ys, ref p, false);
        var variation = _glyphVariations?.Get(glyph);

        var start = 0;
        for (var c = 0; c < contourCount; c++) {
            var end = endPts[c];
            var points = new List<GlyphPoint>();
            for (var i = start; i <= end; i++) {
                var point = transform.Apply(xs[i] + (variation?.X[i] ?? 0), ys[i] + (variation?.Y[i] ?? 0));
                points.Add(new GlyphPoint(point.X, point.Y, (flags[i] & 1) != 0));
            }

            AddFlattenedContour(points, contours);
            start = end + 1;
        }

        return contours;
    }

    /// <summary>The highest point of a glyph in font units, or null when it has no outline.</summary>
    private double? GlyphTop(int codePoint) {
        var glyph = MapGlyph(codePoint);
        if (glyph == 0) return null;
        double? top = null;
        foreach (var contour in ReadGlyphContours(glyph, new FontTransform(1, 0, 0, 1, 0, 0), 0)) {
            foreach (var point in contour) top = top.HasValue ? Math.Max(top.Value, point.Y) : point.Y;
        }

        return top;
    }

    private void ReadCompositeGlyphContours(int glyphOffset, FontTransform transform, int depth, List<List<ChartPoint>> contours, GlyphDeltas? variation) {
        const ushort argWords = 1;
        const ushort argsAreXy = 2;
        const ushort haveScale = 8;
        const ushort moreComponents = 32;
        const ushort haveXyScale = 64;
        const ushort haveTwoByTwo = 128;

        var p = glyphOffset + 10;
        var component = 0;
        ushort flags;
        do {
            if (p + 4 > _data.Length) return;
            flags = ReadUInt16(_data, p);
            var componentGlyph = ReadUInt16(_data, p + 2);
            p += 4;
            double arg1;
            double arg2;
            if ((flags & argWords) != 0) {
                if (p + 4 > _data.Length) return;
                arg1 = ReadInt16(_data, p);
                arg2 = ReadInt16(_data, p + 2);
                p += 4;
            } else {
                if (p + 2 > _data.Length) return;
                arg1 = (sbyte)_data[p];
                arg2 = (sbyte)_data[p + 1];
                p += 2;
            }

            var dx = (flags & argsAreXy) != 0 ? arg1 : 0;
            var dy = (flags & argsAreXy) != 0 ? arg2 : 0;
            if ((flags & argsAreXy) != 0 && variation != null && component < variation.X.Length - 4) { dx += variation.X[component]; dy += variation.Y[component]; }
            component++;
            var a = 1.0;
            var b = 0.0;
            var c = 0.0;
            var d = 1.0;
            if ((flags & haveScale) != 0) {
                if (p + 2 > _data.Length) return;
                a = d = ReadF2Dot14(_data, p);
                p += 2;
            } else if ((flags & haveXyScale) != 0) {
                if (p + 4 > _data.Length) return;
                a = ReadF2Dot14(_data, p);
                d = ReadF2Dot14(_data, p + 2);
                p += 4;
            } else if ((flags & haveTwoByTwo) != 0) {
                if (p + 8 > _data.Length) return;
                a = ReadF2Dot14(_data, p);
                b = ReadF2Dot14(_data, p + 2);
                c = ReadF2Dot14(_data, p + 4);
                d = ReadF2Dot14(_data, p + 6);
                p += 8;
            }

            // SCALED_COMPONENT_OFFSET scales both the authored displacement and its
            // gvar component-point delta before composing the parent transform.
            if ((flags & argsAreXy) != 0 && (flags & 0x800) != 0 && (flags & 0x1000) == 0) {
                var originalX = dx;
                dx = a * dx + b * dy;
                dy = c * originalX + d * dy;
            }

            contours.AddRange(ReadGlyphContours(componentGlyph, transform.Compose(a, b, c, d, dx, dy), depth + 1));
        } while ((flags & moreComponents) != 0);
    }

    private int GlyphOffset(ushort glyph) {
        if (_indexToLocFormat == 0) return ReadUInt16(_data, _loca + glyph * 2) * 2;
        return CheckedOffset(_data, ReadUInt32(_data, _loca + glyph * 4));
    }

    private static void DecodeCoordinates(byte[] data, byte[] flags, short[] values, ref int p, bool xAxis) {
        var shortFlag = xAxis ? 2 : 4;
        var sameOrPositiveFlag = xAxis ? 16 : 32;
        var current = 0;
        for (var i = 0; i < flags.Length; i++) {
            var flag = flags[i];
            int delta;
            if ((flag & shortFlag) != 0) {
                delta = data[p++];
                if ((flag & sameOrPositiveFlag) == 0) delta = -delta;
            } else if ((flag & sameOrPositiveFlag) != 0) {
                delta = 0;
            } else {
                delta = ReadInt16(data, p);
                p += 2;
            }

            current += delta;
            values[i] = (short)current;
        }
    }

    private static void AddFlattenedContour(List<GlyphPoint> source, List<List<ChartPoint>> contours) {
        if (source.Count == 0) return;
        var result = new List<ChartPoint>();
        var last = source[source.Count - 1];
        var first = source[0];
        var current = first.OnCurve ? first : last.OnCurve ? last : Mid(last, first);
        result.Add(current.Point);
        var index = first.OnCurve ? 1 : 0;

        while (index < source.Count) {
            var point = source[index % source.Count];
            if (point.OnCurve) {
                result.Add(point.Point);
                current = point;
                index++;
                continue;
            }

            var next = source[(index + 1) % source.Count];
            var end = next.OnCurve ? next : Mid(point, next);
            FlattenQuadratic(current, point, end, result);
            current = end;
            index += next.OnCurve ? 2 : 1;
        }

        if (result.Count >= 3) contours.Add(result);
    }

    private static void FlattenQuadratic(GlyphPoint start, GlyphPoint control, GlyphPoint end, List<ChartPoint> output) {
        // Sized to the glyph on the page: a small letter needs two or three chords per curve, a headline more.
        var steps = Math.Max(2, ChartCurveFlattening.QuadraticSegments(start.Point, control.Point, end.Point, 1));
        for (var i = 1; i <= steps; i++) {
            var t = i / (double)steps;
            var mt = 1 - t;
            output.Add(new ChartPoint(mt * mt * start.X + 2 * mt * t * control.X + t * t * end.X, mt * mt * start.Y + 2 * mt * t * control.Y + t * t * end.Y));
        }
    }

    private static GlyphPoint Mid(GlyphPoint left, GlyphPoint right) => new((left.X + right.X) / 2.0, (left.Y + right.Y) / 2.0, true);

    private readonly struct FontTransform {
        public FontTransform(double xx, double xy, double yx, double yy, double dx, double dy) {
            Xx = xx;
            Xy = xy;
            Yx = yx;
            Yy = yy;
            Dx = dx;
            Dy = dy;
        }

        private double Xx { get; }
        private double Xy { get; }
        private double Yx { get; }
        private double Yy { get; }
        private double Dx { get; }
        private double Dy { get; }

        public ChartPoint Apply(double x, double y) => new(Dx + Xx * x + Xy * y, Dy + Yx * x + Yy * y);

        public FontTransform Compose(double xx, double xy, double yx, double yy, double dx, double dy) {
            return new FontTransform(
                Xx * xx + Xy * yx,
                Xx * xy + Xy * yy,
                Yx * xx + Yy * yx,
                Yx * xy + Yy * yy,
                Dx + Xx * dx + Xy * dy,
                Dy + Yx * dx + Yy * dy);
        }
    }

    private readonly struct GlyphPoint {
        public GlyphPoint(double x, double y, bool onCurve) {
            X = x;
            Y = y;
            OnCurve = onCurve;
            Point = new ChartPoint(x, y);
        }

        public double X { get; }
        public double Y { get; }
        public bool OnCurve { get; }
        public ChartPoint Point { get; }
    }

    /// <summary>Collects a cubic (CFF) outline as closed contours, flattened to the placed size.</summary>
    private sealed class ContourBuilder : IGlyphOutlineSink {
        private readonly FontTransform _transform;
        private readonly List<List<ChartPoint>> _contours = new();
        private List<ChartPoint>? _current;
        private ChartPoint _last;

        public ContourBuilder(FontTransform transform) => _transform = transform;

        public void MoveTo(double x, double y) {
            Close();
            _last = _transform.Apply(x, y);
            _current = new List<ChartPoint> { _last };
        }

        public void LineTo(double x, double y) {
            if (_current == null) MoveTo(0, 0);
            _last = _transform.Apply(x, y);
            _current!.Add(_last);
        }

        public void CubicTo(double x1, double y1, double x2, double y2, double x, double y) {
            if (_current == null) MoveTo(0, 0);
            var start = _last;
            var c1 = _transform.Apply(x1, y1);
            var c2 = _transform.Apply(x2, y2);
            var end = _transform.Apply(x, y);
            var steps = Math.Max(2, ChartCurveFlattening.CubicSegments(start, c1, c2, end, 1));
            for (var i = 1; i <= steps; i++) {
                var t = i / (double)steps;
                var mt = 1 - t;
                var a = mt * mt * mt;
                var b = 3 * mt * mt * t;
                var c = 3 * mt * t * t;
                var d = t * t * t;
                _current!.Add(new ChartPoint(a * start.X + b * c1.X + c * c2.X + d * end.X, a * start.Y + b * c1.Y + c * c2.Y + d * end.Y));
            }

            _last = end;
        }

        public void Close() {
            if (_current != null && _current.Count >= 3) _contours.Add(_current);
            _current = null;
        }

        public List<List<ChartPoint>> Finish() {
            Close();
            return _contours;
        }
    }
}

using System;
using System.Collections.Generic;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>Per-instance TrueType tuple deltas, including contour interpolation and composite offsets.</summary>
internal sealed class GlyphVariationData {
    private readonly FontTableReader _gvar, _glyf, _loca;
    private readonly double[] _coordinates;
    private readonly bool _longLoca;
    private readonly int _glyphCount;
    private readonly object _lock = new();
    private readonly Dictionary<int, GlyphDeltas?> _cache = new();
    private int _cachedBytes;
    private GlyphVariationData(FontTableReader gvar, FontTableReader glyf, FontTableReader loca, bool longLoca, int glyphCount, double[] coordinates) {
        _gvar = gvar; _glyf = glyf; _loca = loca; _longLoca = longLoca; _glyphCount = glyphCount; _coordinates = coordinates;
        if (gvar.U16(0) != 1 || gvar.U16(4) != coordinates.Length || gvar.U16(12) != glyphCount) throw new FontLayoutException();
        gvar.Require(20, (glyphCount + 1) * ((gvar.U16(14) & 1) == 0 ? 2 : 4));
    }
    internal static GlyphVariationData? Create(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int glyphCount, bool longLoca, double[] coordinates) {
        if (!tables.ContainsKey("gvar") || !tables.ContainsKey("glyf") || !tables.ContainsKey("loca")) return null;
        try { return new GlyphVariationData(Table("gvar"), Table("glyf"), Table("loca"), longLoca, glyphCount, coordinates); }
        catch (FontLayoutException) { return null; }
        FontTableReader Table(string tag) => new(data, tables[tag], lengths[tag]);
    }
    internal GlyphDeltas? Get(ushort glyph) {
        lock (_lock) {
            if (_cache.TryGetValue(glyph, out var cached)) return cached;
            try {
                var result = Read(glyph);
                if (result == null || result.X.Length * 16 <= 256 * 1024) {
                    var bytes = result == null ? 0 : result.X.Length * 16;
                    if (_cachedBytes + bytes > 256 * 1024 || _cache.Count >= 256) { _cache.Clear(); _cachedBytes = 0; }
                    _cache[glyph] = result; _cachedBytes += bytes;
                }
                return result;
            } catch (FontLayoutException) { if (_cache.Count >= 256) { _cache.Clear(); _cachedBytes = 0; } _cache[glyph] = null; return null; }
        }
    }
    private GlyphDeltas? Read(ushort glyph) {
        if (glyph >= _glyphCount) return null;
        var start = VariationOffset(glyph); var end = VariationOffset(glyph + 1);
        if (end == start) return null;
        var table = _gvar.Slice(start, end - start);
        var outlines = ReadPoints(glyph);
        var pointCount = outlines.X.Length; var flags = table.U16(0); var count = flags & 0x0fff;
        var data = table.U16(2); var header = 4;
        var shared = (flags & 0x8000) != 0 ? Points(table, ref data, pointCount) : null;
        var dx = new double[pointCount]; var dy = new double[pointCount];
        long work = 0;
        for (var tuple = 0; tuple < count; tuple++) {
            var size = table.U16(header); var tupleIndex = table.U16(header + 2); header += 4;
            var peak = new double[_coordinates.Length];
            if ((tupleIndex & 0x8000) != 0) {
                for (var axis = 0; axis < peak.Length; axis++) { peak[axis] = table.F2Dot14(header); header += 2; }
            } else {
                var index = tupleIndex & 0x0fff;
                if (index >= _gvar.U16(6)) throw new FontLayoutException();
                var at = _gvar.Offset(0, 8, wide: true) + index * peak.Length * 2;
                for (var axis = 0; axis < peak.Length; axis++) peak[axis] = _gvar.F2Dot14(at + axis * 2);
            }
            var scalar = 1.0;
            if ((tupleIndex & 0x4000) != 0) {
                for (var axis = 0; axis < peak.Length; axis++) {
                    scalar *= FontVariationContext.AxisScalar(_coordinates[axis], table.F2Dot14(header + axis * 2), peak[axis], table.F2Dot14(header + (peak.Length + axis) * 2));
                }
                header += peak.Length * 4;
            } else {
                for (var axis = 0; axis < peak.Length; axis++) {
                    var coordinate = _coordinates[axis]; var p = peak[axis];
                    if (p == 0) continue;
                    if (coordinate == 0 || coordinate * p < 0) { scalar = 0; break; }
                    scalar *= Math.Min(1, coordinate / p);
                }
            }
            if (header > table.U16(2)) throw new FontLayoutException();
            var block = table.Slice(data, size); data += size;
            if (scalar == 0) continue;
            if ((work += pointCount) > 4_000_000) throw new FontLayoutException();
            var cursor = 0;
            var points = (tupleIndex & 0x2000) != 0 ? Points(block, ref cursor, pointCount) : shared;
            var deltas = points?.Length ?? pointCount;
            var xs = Deltas(block, ref cursor, deltas); var ys = Deltas(block, ref cursor, deltas);
            if (points == null) {
                for (var i = 0; i < pointCount; i++) { dx[i] += scalar * xs[i]; dy[i] += scalar * ys[i]; }
            } else {
                var explicitPoints = new bool[pointCount]; var x = new double[pointCount]; var y = new double[pointCount];
                for (var i = 0; i < points.Length; i++) { var p = points[i]; explicitPoints[p] = true; x[p] = xs[i]; y[p] = ys[i]; }
                var first = 0;
                foreach (var last in outlines.EndPoints) {
                    Interpolate(first, last, outlines.X, x, explicitPoints);
                    Interpolate(first, last, outlines.Y, y, explicitPoints); first = last + 1;
                }
                for (var i = 0; i < pointCount; i++) { dx[i] += scalar * x[i]; dy[i] += scalar * y[i]; }
            }
        }
        return new GlyphDeltas(dx, dy);
    }
    private int VariationOffset(int glyph) {
        var at = (_gvar.U16(14) & 1) == 0 ? _gvar.U16(20 + glyph * 2) * 2L : _gvar.U32(20 + glyph * 4);
        var offset = (long)_gvar.U32(16) + at;
        if (offset > _gvar.Length) throw new FontLayoutException();
        return (int)offset;
    }
    private (double[] X, double[] Y, int[] EndPoints) ReadPoints(ushort glyph) {
        var at = _longLoca ? _loca.U32(glyph * 4) : (uint)(_loca.U16(glyph * 2) * 2);
        var end = _longLoca ? _loca.U32((glyph + 1) * 4) : (uint)(_loca.U16((glyph + 1) * 2) * 2);
        if (at > int.MaxValue || end > int.MaxValue || end < at) throw new FontLayoutException();
        var table = _glyf.Slice((int)at, (int)(end - at));
        if (table.Length == 0) return (new double[4], new double[4], Array.Empty<int>());
        var contours = table.I16(0);
        if (contours < 0) {
            var p = 10; var components = 0; int flags;
            do {
                flags = table.U16(p); table.Require(p, 4); p += 4 + ((flags & 1) != 0 ? 4 : 2);
                p += (flags & 8) != 0 ? 2 : (flags & 64) != 0 ? 4 : (flags & 128) != 0 ? 8 : 0;
                if (++components > 4096) throw new FontLayoutException();
                table.Require(p, 0);
            } while ((flags & 32) != 0);
            return (new double[components + 4], new double[components + 4], Array.Empty<int>());
        }
        var ends = new int[contours];
        for (var i = 0; i < contours; i++) { ends[i] = table.U16(10 + i * 2); if (i > 0 && ends[i] <= ends[i - 1]) throw new FontLayoutException(); }
        var count = contours == 0 ? 0 : ends[contours - 1] + 1;
        var instructions = 10 + contours * 2; var cursor = instructions + 2 + table.U16(instructions);
        var pointFlags = new int[count];
        for (var i = 0; i < count; i++) {
            var flag = table.U8(cursor++); pointFlags[i] = flag;
            if ((flag & 8) == 0) continue;
            var repeated = table.U8(cursor++); if (repeated > count - i - 1) throw new FontLayoutException();
            for (var n = 0; n < repeated; n++) pointFlags[++i] = flag;
        }
        var x = Coordinates(table, ref cursor, pointFlags, 2, 16); var y = Coordinates(table, ref cursor, pointFlags, 4, 32);
        return (x, y, ends);
    }
    private static double[] Coordinates(FontTableReader table, ref int p, int[] flags, int shortFlag, int positive) {
        var values = new double[flags.Length + 4]; var coordinate = 0;
        for (var i = 0; i < flags.Length; i++) {
            var flag = flags[i]; int delta;
            if ((flag & shortFlag) != 0) { delta = table.U8(p++); if ((flag & positive) == 0) delta = -delta; }
            else if ((flag & positive) != 0) delta = 0;
            else { delta = table.I16(p); p += 2; }
            coordinate += delta; values[i] = coordinate;
        }
        return values;
    }
    private static int[]? Points(FontTableReader table, ref int p, int total) {
        var count = table.U8(p++); if ((count & 0x80) != 0) count = ((count & 0x7f) << 8) | table.U8(p++);
        if (count == 0) return null;
        if (count > total) throw new FontLayoutException();
        var result = new int[count]; var point = 0; var index = 0;
        while (index < count) {
            var control = table.U8(p++); var run = (control & 0x7f) + 1;
            if (run > count - index) throw new FontLayoutException();
            for (var i = 0; i < run; i++) {
                var delta = (control & 0x80) == 0 ? table.U8(p++) : table.U16(p);
                if ((control & 0x80) != 0) p += 2;
                point += delta;
                if (point >= total || index > 0 && point <= result[index - 1]) throw new FontLayoutException();
                result[index++] = point;
            }
        }
        return result;
    }
    private static double[] Deltas(FontTableReader table, ref int p, int count) {
        var result = new double[count]; var index = 0;
        while (index < count) {
            var control = table.U8(p++); var run = (control & 0x3f) + 1;
            if (run > count - index) throw new FontLayoutException();
            for (var i = 0; i < run; i++) {
                result[index++] = (control & 0x80) != 0 ? 0 : (control & 0x40) != 0 ? table.I16(p) : table.I8(p);
                if ((control & 0x80) == 0) p += (control & 0x40) != 0 ? 2 : 1;
            }
        }
        return result;
    }
    private static void Interpolate(int first, int last, double[] coordinates, double[] deltas, bool[] specified) {
        var anchor = -1;
        for (var i = first; i <= last; i++) if (specified[i]) { anchor = i; break; }
        if (anchor < 0) return;
        var previous = anchor;
        do {
            var next = previous == last ? first : previous + 1;
            while (!specified[next]) next = next == last ? first : next + 1;
            var c1 = coordinates[previous]; var c2 = coordinates[next]; var d1 = deltas[previous]; var d2 = deltas[next];
            for (var i = previous == last ? first : previous + 1; i != next; i = i == last ? first : i + 1) {
                var c = coordinates[i];
                deltas[i] = c1 == c2 ? d1 == d2 ? d1 : 0 : c <= Math.Min(c1, c2) ? c1 < c2 ? d1 : d2 : c >= Math.Max(c1, c2) ? c1 > c2 ? d1 : d2 : d1 + (d2 - d1) * (c - c1) / (c2 - c1);
            }
            previous = next;
        } while (previous != anchor);
    }
}

internal sealed class GlyphDeltas {
    internal GlyphDeltas(double[] x, double[] y) { X = x; Y = y; }
    internal double[] X { get; }
    internal double[] Y { get; }
    internal double LeftPhantomDelta => X.Length < 4 ? 0 : X[X.Length - 4];
    internal double AdvanceDelta => X.Length < 4 ? 0 : X[X.Length - 3] - X[X.Length - 4];
}

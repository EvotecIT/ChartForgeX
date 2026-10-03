using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

internal sealed partial class ColorFontData {
    private ColorGlyphPaint? ReadVersion1(FontTableReader table, ushort glyph) {
        table.Require(0, 34);
        var root = PaintOffset(table, glyph); if (root < 0) return null;
        return Clip(table, glyph, ReadPaint(table, root, new PaintReadContext(), 0));
    }
    private static int PaintOffset(FontTableReader table, ushort glyph) {
        var list = table.Offset(0, 14, optional: true, wide: true); if (list < 0) return -1;
        var count = table.U32(list); table.Require(list + 4, CountBytes(count, 6));
        var low = 0L; var high = (long)count - 1;
        while (low <= high) {
            var middle = (low + high) / 2; var at = table.Record(list + 4, middle, 6); var id = table.U16(at);
            if (id < glyph) low = middle + 1;
            else if (id > glyph) high = middle - 1;
            else return table.Offset(list, at + 2, wide: true);
        }
        return -1;
    }
    private static ColorGlyphPaint Clip(FontTableReader table, ushort glyph, ColorGlyphPaint paint) {
        var list = table.Offset(0, 22, optional: true, wide: true); if (list < 0) return paint;
        if (table.U8(list) != 1) throw new FontLayoutException();
        var count = table.U32(list + 1); table.Require(list + 5, CountBytes(count, 7));
        var low = 0L; var high = (long)count - 1;
        while (low <= high) {
            var middle = (low + high) / 2; var at = table.Record(list + 5, middle, 7);
            if (glyph < table.U16(at)) high = middle - 1;
            else if (glyph > table.U16(at + 2)) low = middle + 1;
            else {
                var box = Relative24(table, list, at + 4); var format = table.U8(box);
                if (format != 1 && format != 2) throw new FontLayoutException();
                table.Require(box, format == 1 ? 9 : 13);
                var x = table.I16(box + 1); var y = table.I16(box + 3);
                var width = table.I16(box + 5) - x; var height = table.I16(box + 7) - y;
                if (width < 0 || height < 0) throw new FontLayoutException();
                return new ColorGlyphPaint { Kind = ColorPaintKind.Clip, Clip = new ChartRect(x, y, width, height), Children = new[] { paint } };
            }
        }
        return paint;
    }
    private ColorGlyphPaint ReadPaint(FontTableReader table, int at, PaintReadContext context, int depth) {
        if (depth > 64 || --context.Remaining < 0 || !context.Active.Add(at)) throw new FontLayoutException();
        try {
            if (context.Read.TryGetValue(at, out var existing)) return existing;
            var format = table.U8(at); ColorGlyphPaint result;
            if (format == 1) {
                var count = table.U8(at + 1); var first = table.U32(at + 2);
                var list = table.Offset(0, 18, wide: true); var length = table.U32(list);
                if ((long)first + count > length) throw new FontLayoutException();
                table.Require(list + 4, CountBytes(length, 4)); var children = new ColorGlyphPaint[count];
                for (var i = 0; i < count; i++) children[i] = ReadPaint(table, table.Offset(list, table.Record(list + 4, (long)first + i, 4), wide: true), context, depth + 1);
                result = new ColorGlyphPaint { Kind = ColorPaintKind.Layers, Children = children };
            } else if (format == 2 || format == 3) {
                table.Require(at, format == 2 ? 5 : 9); var palette = table.U16(at + 1); CheckPalette(palette);
                result = new ColorGlyphPaint { Kind = ColorPaintKind.Solid, PaletteIndex = palette, Alpha = Alpha(table.F2Dot14(at + 3)) };
            } else if (format >= 4 && format <= 9) {
                var basic = format - (format & 1); var variable = (format & 1) != 0;
                var size = basic == 8 ? 12 : 16; table.Require(at, size + (variable ? 4 : 0));
                var line = Relative24(table, at, at + 1); var stops = Stops(table, line, variable);
                var geometry = new double[basic == 8 ? 4 : 6];
                for (var i = 0; i < geometry.Length; i++) geometry[i] = basic == 8 && i >= 2 ? table.F2Dot14(at + 4 + i * 2) * Math.PI :
                    basic == 6 && (i == 2 || i == 5) ? table.U16(at + 4 + i * 2) : table.I16(at + 4 + i * 2);
                result = new ColorGlyphPaint { Kind = basic == 4 ? ColorPaintKind.Linear : basic == 6 ? ColorPaintKind.Radial : ColorPaintKind.Sweep,
                    Geometry = geometry, Stops = stops, Extend = table.U8(line) <= 2 ? table.U8(line) : 0 };
            } else if (format == 10) {
                var glyph = table.U16(at + 4); if (glyph >= _glyphCount) throw new FontLayoutException();
                result = new ColorGlyphPaint { Kind = ColorPaintKind.Glyph, Glyph = (ushort)glyph,
                    Children = new[] { ReadPaint(table, Relative24(table, at, at + 1), context, depth + 1) } };
            } else if (format == 11) {
                var glyph = table.U16(at + 1); if (glyph >= _glyphCount) throw new FontLayoutException();
                var root = PaintOffset(table, (ushort)glyph); if (root < 0) throw new FontLayoutException();
                result = Clip(table, (ushort)glyph, ReadPaint(table, root, context, depth + 1));
            } else if (format >= 12 && format <= 31) {
                result = new ColorGlyphPaint { Kind = ColorPaintKind.Transform, Transform = ReadTransform(table, at, format),
                    Children = new[] { ReadPaint(table, Relative24(table, at, at + 1), context, depth + 1) } };
            } else if (format == 32) {
                var mode = table.U8(at + 4); table.Require(at, 8);
                result = new ColorGlyphPaint { Kind = ColorPaintKind.Composite, CompositeMode = mode <= 27 ? mode : 0,
                    Children = new[] { ReadPaint(table, Relative24(table, at, at + 1), context, depth + 1), ReadPaint(table, Relative24(table, at, at + 5), context, depth + 1) } };
            } else result = ColorGlyphPaint.Empty; // Future paint formats are empty, bounded subgraphs.
            context.Read[at] = result; return result;
        } finally { context.Active.Remove(at); }
    }
    private ColorPaintStop[] Stops(FontTableReader table, int at, bool variable) {
        var count = table.U16(at + 1); if (count > 4096) throw new FontLayoutException();
        var stride = variable ? 10 : 6; table.Require(at + 3, count * stride); var stops = new ColorPaintStop[count];
        for (var i = 0; i < count; i++) {
            var record = at + 3 + i * stride; var palette = table.U16(record + 2); CheckPalette(palette);
            stops[i] = new ColorPaintStop(table.F2Dot14(record), palette, Alpha(table.F2Dot14(record + 4)), i);
        }
        Array.Sort(stops, (a, b) => { var compare = a.Offset.CompareTo(b.Offset); return compare == 0 ? a.Order.CompareTo(b.Order) : compare; });
        return stops;
    }
    private static int CountBytes(uint count, int stride) {
        var length = (long)count * stride; if (length > int.MaxValue) throw new FontLayoutException(); return (int)length;
    }
    private static int Relative24(FontTableReader table, int origin, int field) {
        var relative = table.U24(field); var result = (long)origin + relative;
        if (relative == 0 || result >= table.Length) throw new FontLayoutException(); return (int)result;
    }
    private static double Alpha(double value) => Math.Max(0, Math.Min(1, value));
    private sealed class PaintReadContext {
        internal int Remaining = 4096;
        internal readonly HashSet<int> Active = new();
        internal readonly Dictionary<int, ColorGlyphPaint> Read = new();
    }
}

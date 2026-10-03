using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

/// <summary>Optional colour-font tables, shared by all rendering identities and fallback views of a face.</summary>
internal sealed partial class ColorFontData {
    private readonly FontTableReader? _colr;
    private readonly ChartColor[] _palette;
    private readonly int _glyphCount;
    private readonly Dictionary<ushort, ColorGlyphPaint?> _paints = new();
    internal BitmapFontData Bitmaps { get; }

    private ColorFontData(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int glyphCount, int unitsPerEm) {
        _glyphCount = glyphCount;
        _palette = ReadPalette(Table(data, tables, lengths, "CPAL"));
        _colr = _palette.Length == 0 ? null : Table(data, tables, lengths, "COLR");
        Bitmaps = new BitmapFontData(data, tables, lengths, glyphCount, unitsPerEm);
    }
    internal static ColorFontData? Create(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int glyphCount, int unitsPerEm) =>
        tables.ContainsKey("COLR") || tables.ContainsKey("sbix") || tables.ContainsKey("CBDT")
            ? new ColorFontData(data, tables, lengths, glyphCount, unitsPerEm) : null;

    internal static FontTableReader? Table(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, string tag) {
        if (!tables.TryGetValue(tag, out var start) || !lengths.TryGetValue(tag, out var length)) return null;
        try { return new FontTableReader(data, start, length); } catch (FontLayoutException) { return null; }
    }
    private static ChartColor[] ReadPalette(FontTableReader? optional) {
        if (!optional.HasValue) return Array.Empty<ChartColor>();
        try {
            var table = optional.Value;
            if (table.U16(0) > 1 || table.U16(4) == 0) return Array.Empty<ChartColor>();
            var count = table.U16(2); var first = table.U16(12); var records = table.Offset(0, 8, wide: true);
            table.Require(12, table.U16(4) * 2); table.Require(records, table.U16(6) * 4);
            if (first > table.U16(6) - count) throw new FontLayoutException();
            var colors = new ChartColor[count];
            for (var i = 0; i < count; i++) {
                var at = table.Record(records, first + i, 4);
                colors[i] = new ChartColor((byte)table.U8(at + 2), (byte)table.U8(at + 1), (byte)table.U8(at), (byte)table.U8(at + 3));
            }
            return colors;
        } catch (FontLayoutException) { return Array.Empty<ChartColor>(); }
    }
    internal ChartColor Color(int index, ChartColor foreground) => index == 0xFFFF ? new ChartColor(foreground.R, foreground.G, foreground.B) : _palette[index];
    private void CheckPalette(int index) { if (index != 0xFFFF && index >= _palette.Length) throw new FontLayoutException(); }

    internal ColorGlyphPaint? Paint(ushort glyph) {
        if (!_colr.HasValue || glyph >= _glyphCount) return null;
        lock (_paints) {
            if (_paints.TryGetValue(glyph, out var cached)) return cached;
            ColorGlyphPaint? paint;
            try {
                var table = _colr.Value; var version = table.U16(0);
                paint = version == 1 ? ReadVersion1(table, glyph) ?? ReadVersion0(table, glyph) : version == 0 ? ReadVersion0(table, glyph) : null;
            } catch (FontLayoutException) { paint = null; }
            // A face can contain tens of thousands of large paint graphs. Retain a bounded working set.
            if (_paints.Count >= 128) _paints.Clear();
            return _paints[glyph] = paint;
        }
    }
    private ColorGlyphPaint? ReadVersion0(FontTableReader table, ushort glyph) {
        var count = table.U16(2); if (count == 0) return null;
        var records = table.Offset(0, 4, wide: true); table.Require(records, count * 6);
        var low = 0; var high = count - 1;
        while (low <= high) {
            var middle = (low + high) / 2; var record = records + middle * 6; var id = table.U16(record);
            if (id < glyph) low = middle + 1;
            else if (id > glyph) high = middle - 1;
            else {
                var first = table.U16(record + 2); var layers = table.U16(record + 4);
                if (layers > 4096 || first > table.U16(12) - layers) throw new FontLayoutException();
                var offset = table.Offset(0, 8, wide: true); var children = new ColorGlyphPaint[layers];
                for (var i = 0; i < layers; i++) {
                    var at = table.Record(offset, first + i, 4); var layerGlyph = table.U16(at); var palette = table.U16(at + 2);
                    if (layerGlyph >= _glyphCount) throw new FontLayoutException(); CheckPalette(palette);
                    children[i] = new ColorGlyphPaint { Kind = ColorPaintKind.Glyph, Glyph = (ushort)layerGlyph,
                        Children = new[] { new ColorGlyphPaint { Kind = ColorPaintKind.Solid, PaletteIndex = palette } } };
                }
                return new ColorGlyphPaint { Kind = ColorPaintKind.Layers, Children = children };
            }
        }
        return null;
    }
}

using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

/// <summary>Optional colour-font tables, shared by all rendering identities and fallback views of a face.</summary>
internal sealed partial class ColorFontData {
    private readonly FontTableReader? _colr;
    private readonly FontColorPalettes? _palettes;
    private readonly int _glyphCount;
    private readonly Dictionary<ushort, ColorGlyphPaint?> _paints = new();
    private int _paintBytes;
    private const int MaximumGraphBytes = 2 * 1024 * 1024;
    private const int MaximumCacheBytes = 8 * 1024 * 1024;
    internal BitmapFontData Bitmaps { get; }

    private ColorFontData(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int glyphCount, int unitsPerEm) {
        _glyphCount = glyphCount;
        _palettes = FontColorPalettes.Read(Table(data, tables, lengths, "CPAL"));
        _colr = _palettes == null ? null : Table(data, tables, lengths, "COLR");
        Bitmaps = new BitmapFontData(data, tables, lengths, glyphCount, unitsPerEm);
    }
    internal static ColorFontData? Create(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int glyphCount, int unitsPerEm) =>
        tables.ContainsKey("COLR") || tables.ContainsKey("sbix") || tables.ContainsKey("CBDT")
            ? new ColorFontData(data, tables, lengths, glyphCount, unitsPerEm) : null;

    internal static FontTableReader? Table(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, string tag) {
        if (!tables.TryGetValue(tag, out var start) || !lengths.TryGetValue(tag, out var length)) return null;
        try { return new FontTableReader(data, start, length); } catch (FontLayoutException) { return null; }
    }
    internal ChartColor Color(int index, ChartColor foreground, int palette = 0) => index == 0xFFFF ? new ChartColor(foreground.R, foreground.G, foreground.B) : _palettes!.Color(index, palette);
    private void CheckPalette(int index) { if (index != 0xFFFF && index >= (_palettes?.EntryCount ?? 0)) throw new FontLayoutException(); }

    internal ColorGlyphPaint? Paint(ushort glyph) {
        if (!_colr.HasValue || glyph >= _glyphCount) return null;
        lock (_paints) {
            if (_paints.TryGetValue(glyph, out var cached)) return cached;
            ColorGlyphPaint? paint; var bytes = 0;
            try {
                var table = _colr.Value; var version = table.U16(0);
                paint = version == 1 ? ReadVersion1(table, glyph, out bytes) ?? ReadVersion0(table, glyph, out bytes) : version == 0 ? ReadVersion0(table, glyph, out bytes) : null;
            } catch (FontLayoutException) { paint = null; bytes = 0; }
            // A face can contain tens of thousands of large paint graphs. Retain a bounded working set.
            if (_paints.Count >= 128 || _paintBytes > MaximumCacheBytes - bytes) { _paints.Clear(); _paintBytes = 0; }
            _paintBytes += bytes;
            return _paints[glyph] = paint;
        }
    }
    private ColorGlyphPaint? ReadVersion0(FontTableReader table, ushort glyph, out int bytes) {
        bytes = 0;
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
                bytes = 384 + layers * 800;
                if (bytes > MaximumGraphBytes) throw new FontLayoutException();
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

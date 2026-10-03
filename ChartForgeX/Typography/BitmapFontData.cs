using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>Bounded bitmap-strike decoding, using the same dependency-free codecs as chart images.</summary>
internal sealed partial class BitmapFontData {
    private readonly FontTableReader? _sbix, _cblc, _cbdt;
    private readonly int _glyphCount, _unitsPerEm;
    private readonly Dictionary<long, BitmapGlyph?> _cache = new();
    private readonly List<BitmapStrike> _strikes = new();
    private long _cachedBytes;
    private static readonly RasterDecodeOptions DecodeLimits = new() { MaximumEncodedBytes = 4 * 1024 * 1024, MaximumPixels = 1024 * 1024 };
    internal BitmapFontData(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int glyphCount, int unitsPerEm) {
        _glyphCount = glyphCount; _unitsPerEm = Math.Max(1, unitsPerEm);
        _sbix = ColorFontData.Table(data, tables, lengths, "sbix");
        _cblc = ColorFontData.Table(data, tables, lengths, "CBLC"); _cbdt = ColorFontData.Table(data, tables, lengths, "CBDT");
        ReadStrikes();
    }
    private void ReadStrikes() {
        if (_sbix.HasValue) try {
            var table = _sbix.Value; if (table.U16(0) == 1) {
                var count = table.U32(4); if (count > 32) throw new FontLayoutException(); table.Require(8, (int)count * 4);
                for (var i = 0; i < count; i++) {
                    var at = table.Offset(0, 8 + i * 4, wide: true); var ppem = table.U16(at);
                    table.Require(at + 4, (_glyphCount + 1) * 4);
                    if (ppem > 0) _strikes.Add(new BitmapStrike(_strikes.Count, at, ppem, ppem, true));
                }
            }
        } catch (FontLayoutException) { /* Optional strikes do not invalidate outlines. */ }
        if (_cblc.HasValue && _cbdt.HasValue) try {
            var table = _cblc.Value;
            if (table.U16(0) == 3 && _cbdt.Value.U16(0) == 3) {
                var count = table.U32(4); if (count > 32) throw new FontLayoutException(); table.Require(8, (int)count * 48);
                for (var i = 0; i < count; i++) {
                    var at = 8 + i * 48; var x = table.U8(at + 44); var y = table.U8(at + 45);
                    if (x > 0 && y > 0) _strikes.Add(new BitmapStrike(_strikes.Count, at, x, y, false));
                }
            }
        } catch (FontLayoutException) { }
    }
    internal BitmapGlyph? Nearest(ushort glyph, double pixelSize) {
        BitmapGlyph? best = null; var distance = double.PositiveInfinity; var bestSize = 0;
        foreach (var strike in _strikes) {
            var delta = Math.Abs(strike.PpemY - pixelSize);
            if (delta > distance || delta == distance && strike.PpemY <= bestSize) continue;
            var image = Get(strike, glyph); if (image == null) continue;
            best = image; bestSize = strike.PpemY; distance = delta;
        }
        return best;
    }
    internal IReadOnlyList<BitmapGlyph> All(ushort glyph) {
        var images = new List<BitmapGlyph>();
        foreach (var strike in _strikes) { var image = Get(strike, glyph); if (image != null) images.Add(image); }
        return images;
    }
    private BitmapGlyph? Get(BitmapStrike strike, ushort glyph) {
        if (glyph >= _glyphCount) return null;
        var key = ((long)strike.Key << 16) | glyph;
        lock (_cache) {
            if (_cache.TryGetValue(key, out var cached)) return cached;
            BitmapGlyph? bitmap;
            try { bitmap = strike.Sbix ? ReadSbix(strike, glyph) : ReadCbdt(strike, glyph); }
            catch (FontLayoutException) { bitmap = null; }
            var bytes = bitmap?.Image.Pixels.Length ?? 0;
            if (_cache.Count >= 128 || _cachedBytes + bytes > 16 * 1024 * 1024) { _cache.Clear(); _cachedBytes = 0; }
            _cachedBytes += bytes; return _cache[key] = bitmap;
        }
    }
    private static bool Decode(FontTableReader table, int at, int length, out RgbaImage image, bool pngOnly = false) {
        image = default; table.Require(at, length);
        if (length <= 0 || length > DecodeLimits.MaximumEncodedBytes) return false;
        var encoded = table.Copy(at, length);
        return (!pngOnly || PngReader.IsPng(encoded)) && RasterImageDecoder.TryDecode(encoded, DecodeLimits, out image);
    }
    private readonly struct BitmapStrike {
        internal BitmapStrike(int key, int at, int x, int y, bool sbix) { Key = key; At = at; PpemX = x; PpemY = y; Sbix = sbix; }
        internal readonly int Key, At, PpemX, PpemY;
        internal readonly bool Sbix;
    }
}

/// <summary>A decoded strike and its placement in font design units.</summary>
internal sealed class BitmapGlyph {
    internal RgbaImage Image { get; set; }
    internal ChartRect Bounds { get; set; }
    internal bool OutlineAnchor { get; set; }
    internal bool DrawOutline { get; set; }
}

using System;

namespace ChartForgeX.Raster;

/// <summary>
/// Maps Unicode code points to glyph ids through the best Unicode subtable of an OpenType
/// <c>cmap</c> table (format 12 before format 4). The subtable is chosen once; lookups are binary
/// searches, so a font or a coverage probe can answer many code points cheaply.
/// </summary>
internal sealed class FontCmap {
    private readonly byte[] _data;
    private readonly int _table;
    private readonly ushort _format;

    private FontCmap(byte[] data, int table, ushort format) {
        _data = data;
        _table = table;
        _format = format;
    }

    /// <summary>An empty map: every code point is missing.</summary>
    internal static FontCmap Empty { get; } = new(Array.Empty<byte>(), -1, 0);

    /// <summary>Reads the <c>cmap</c> table at <paramref name="cmapOffset"/> of <paramref name="data"/>.</summary>
    internal static FontCmap Read(byte[] data, int cmapOffset) {
        if (cmapOffset < 0 || cmapOffset + 4 > data.Length) return Empty;
        var subtableCount = ReadUInt16(data, cmapOffset + 2);
        var best = -1;
        var bestScore = 0;
        for (var i = 0; i < subtableCount; i++) {
            var record = cmapOffset + 4 + i * 8;
            if (record + 8 > data.Length) break;
            var platform = ReadUInt16(data, record);
            var encoding = ReadUInt16(data, record + 2);
            var offset = ReadUInt32(data, record + 4);
            if (offset >= (uint)data.Length) continue;
            var absolute = cmapOffset + (int)offset;
            if (absolute < 0 || absolute + 16 > data.Length) continue;
            var format = ReadUInt16(data, absolute);
            var score = platform == 3 && encoding == 10 ? 4 : platform == 3 && encoding == 1 ? 3 : platform == 0 ? 2 : 1;
            // Symbol fonts (3, 0) map their glyphs into the private-use area; they are a last resort.
            if (platform == 3 && encoding == 0) score = 0;
            if ((format == 4 || format == 12) && (best < 0 || score > bestScore)) {
                best = absolute;
                bestScore = score;
            }
        }

        return best < 0 ? Empty : new FontCmap(data, best, ReadUInt16(data, best));
    }

    /// <summary>The glyph id for <paramref name="codePoint"/>, or 0 when the font does not cover it.</summary>
    internal ushort Map(int codePoint) {
        if (codePoint < 0 || _table < 0) return 0;
        return _format == 12 ? MapFormat12(codePoint) : MapFormat4(codePoint);
    }

    private ushort MapFormat4(int code) {
        if (code > ushort.MaxValue) return 0;
        var segCount = ReadUInt16(_data, _table + 6) / 2;
        var endCodes = _table + 14;
        var startCodes = endCodes + segCount * 2 + 2;
        var idDeltas = startCodes + segCount * 2;
        var idRangeOffsets = idDeltas + segCount * 2;
        if (segCount == 0 || idRangeOffsets + segCount * 2 > _data.Length) return 0;

        // The first segment whose end code is at or above the code point.
        var low = 0;
        var high = segCount - 1;
        while (low < high) {
            var mid = (low + high) / 2;
            if (ReadUInt16(_data, endCodes + mid * 2) < code) low = mid + 1;
            else high = mid;
        }

        if (ReadUInt16(_data, endCodes + low * 2) < code) return 0;
        var start = ReadUInt16(_data, startCodes + low * 2);
        if (code < start) return 0;
        var delta = ReadInt16(_data, idDeltas + low * 2);
        var rangeOffset = ReadUInt16(_data, idRangeOffsets + low * 2);
        if (rangeOffset == 0) return (ushort)((code + delta) & 0xffff);
        var glyphOffset = idRangeOffsets + low * 2 + rangeOffset + (code - start) * 2;
        if (glyphOffset < 0 || glyphOffset + 2 > _data.Length) return 0;
        var glyph = ReadUInt16(_data, glyphOffset);
        return glyph == 0 ? (ushort)0 : (ushort)((glyph + delta) & 0xffff);
    }

    private ushort MapFormat12(int code) {
        var groups = ReadUInt32(_data, _table + 12);
        var groupOffset = _table + 16;
        if (groups == 0 || groups > (uint)((_data.Length - groupOffset) / 12)) return 0;
        var low = 0;
        var high = (int)groups - 1;
        while (low <= high) {
            var mid = low + (high - low) / 2;
            var record = groupOffset + mid * 12;
            var start = ReadUInt32(_data, record);
            var end = ReadUInt32(_data, record + 4);
            if ((uint)code < start) high = mid - 1;
            else if ((uint)code > end) low = mid + 1;
            else {
                var glyph = ReadUInt32(_data, record + 8) + (uint)code - start;
                return glyph > ushort.MaxValue ? (ushort)0 : (ushort)glyph;
            }
        }

        return 0;
    }

    private static ushort ReadUInt16(byte[] data, int offset) => (ushort)((data[offset] << 8) | data[offset + 1]);
    private static short ReadInt16(byte[] data, int offset) => (short)ReadUInt16(data, offset);
    private static uint ReadUInt32(byte[] data, int offset) => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
}

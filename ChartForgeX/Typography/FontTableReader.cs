using System;

namespace ChartForgeX.Typography;

/// <summary>Big-endian reads confined to one declared OpenType table, including relative offsets.</summary>
internal readonly struct FontTableReader {
    private readonly byte[] _data;
    private readonly int _start;
    internal FontTableReader(byte[] data, int start, int length) {
        if (start < 0 || length < 0 || start > data.Length - length) throw new FontLayoutException();
        _data = data; _start = start; Length = length;
    }
    internal int Length { get; }
    internal FontTableReader Slice(int at, int length) {
        Require(at, length); return new FontTableReader(_data, _start + at, length);
    }
    internal int U8(int at) { Require(at, 1); return _data[_start + at]; }
    internal int I8(int at) => (sbyte)U8(at);
    internal int U24(int at) { Require(at, 3); return (U8(at) << 16) | U16(at + 1); }
    internal double Fixed(int at) => unchecked((int)U32(at)) / 65536.0;
    internal double F2Dot14(int at) => I16(at) / 16384.0;
    internal byte[] Copy(int at, int length) {
        Require(at, length); var result = new byte[length];
        Buffer.BlockCopy(_data, _start + at, result, 0, length); return result;
    }
    internal void Require(int at, int length) {
        if (at < 0 || length < 0 || at > Length - length) throw new FontLayoutException();
    }
    internal int U16(int at) { Require(at, 2); return (_data[_start + at] << 8) | _data[_start + at + 1]; }
    internal int I16(int at) => (short)U16(at);
    internal int Record(int at, long index, int stride) {
        var position = at + index * stride;
        if (position < 0 || position > Length - stride) throw new FontLayoutException();
        return (int)position;
    }
    internal uint U32(int at) { Require(at, 4); return ((uint)U16(at) << 16) | (uint)U16(at + 2); }
    internal string Tag(int at) {
        Require(at, 4);
        return new string(new[] { (char)_data[_start + at], (char)_data[_start + at + 1], (char)_data[_start + at + 2], (char)_data[_start + at + 3] });
    }
    internal int Offset(int origin, int field, bool optional = false, bool wide = false) {
        var relative = wide ? U32(field) : (uint)U16(field);
        if (relative == 0) { if (optional) return -1; throw new FontLayoutException(); }
        var absolute = (long)origin + relative;
        if (absolute < 0 || absolute >= Length) throw new FontLayoutException();
        return (int)absolute;
    }
    internal int Coverage(int at, ushort glyph) {
        var format = U16(at); var count = U16(at + 2);
        if (format != 1 && format != 2) throw new FontLayoutException();
        var size = format == 1 ? 2 : 6;
        Require(at + 4, count * size);
        var low = 0; var high = count - 1;
        while (low <= high) {
            var middle = low + (high - low) / 2; var record = at + 4 + middle * size;
            var first = U16(record); var last = format == 1 ? first : U16(record + 2);
            if (glyph < first) high = middle - 1;
            else if (glyph > last) low = middle + 1;
            else return format == 1 ? middle : U16(record + 4) + glyph - first;
        }
        return -1;
    }
    internal int Class(int at, ushort glyph) {
        if (at < 0) return 0;
        var format = U16(at);
        if (format == 1) {
            var first = U16(at + 2); var count = U16(at + 4);
            Require(at + 6, count * 2);
            return glyph >= first && glyph - first < count ? U16(at + 6 + (glyph - first) * 2) : 0;
        }
        if (format != 2) throw new FontLayoutException();
        var ranges = U16(at + 2); Require(at + 4, ranges * 6);
        var low = 0; var high = ranges - 1;
        while (low <= high) {
            var middle = low + (high - low) / 2; var record = at + 4 + middle * 6;
            if (glyph < U16(record)) high = middle - 1;
            else if (glyph > U16(record + 2)) low = middle + 1;
            else return U16(record + 4);
        }
        return 0;
    }
}

/// <summary>Invalid optional layout data is isolated from the face's usable outlines and cmap.</summary>
internal sealed class FontLayoutException : Exception { }

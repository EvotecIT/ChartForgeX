using System;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    // Source ownership remains in the existing file/memory reader. No path is reopened to snapshot portable data.
    internal int CapturedByteLength => _root._data.Length;
    // Browser delivery never guesses a selected member of an sfnt collection.
    internal bool IsSingleCapturedFace => _root._data.Length >= 4
        && !(_root._data[0] == (byte)'t' && _root._data[1] == (byte)'t' && _root._data[2] == (byte)'c' && _root._data[3] == (byte)'f');
    internal TrueTypeFont? SelectCapturedFace(int? collectionIndex, string? faceName) => TryLoad(_root._data, collectionIndex, faceName);
    internal byte[] CopyCapturedBytes(int maximumBytes) {
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (_root._data.Length > maximumBytes) throw new NotSupportedException("Owned font bytes exceed the portable per-root limit.");
        return (byte[])_root._data.Clone();
    }
    /// <summary>
    /// This collection member as a standalone sfnt: its own table directory and byte-identical tables, without the
    /// collection's other members. Glyphs, metrics and layout are unchanged. Null for a standalone face, a member whose
    /// table records leave the collection, or one larger than <paramref name="maximumBytes"/>; nothing is allocated then.
    /// </summary>
    internal TrueTypeFont? CopyCollectionMember(int maximumBytes) {
        if (IsSingleCapturedFace || _collectionIndex is not int index) return null;
        var data = _root._data;
        if (12L + (index + 1) * 4L > data.Length) return null;
        long directory = ReadUInt32(data, 12 + index * 4);
        if (directory + 12 > data.Length) return null;
        var count = ReadUInt16(data, (int)directory + 4);
        long size = 12L + count * 16L;
        if (directory + size > data.Length) return null;
        for (var i = 0; i < count; i++) {
            var record = (int)directory + 12 + i * 16;
            long offset = ReadUInt32(data, record + 8), length = ReadUInt32(data, record + 12);
            if (offset + length > data.Length) return null;
            size += (length + 3) & ~3L;
            if (size > maximumBytes) return null;
        }
        if (size > maximumBytes) return null;
        var member = new byte[size];
        Buffer.BlockCopy(data, (int)directory, member, 0, 12 + count * 16);
        var target = 12 + count * 16;
        for (var i = 0; i < count; i++) {
            var record = (int)directory + 12 + i * 16; var length = (int)ReadUInt32(data, record + 12);
            Buffer.BlockCopy(data, (int)ReadUInt32(data, record + 8), member, target, length);
            var at = 12 + i * 16 + 8;
            member[at] = (byte)(target >> 24); member[at + 1] = (byte)(target >> 16); member[at + 2] = (byte)(target >> 8); member[at + 3] = (byte)target;
            target += (length + 3) & ~3;
        }
        return TryLoad(member);
    }
    internal bool SameCapturedBytes(TrueTypeFont other) {
        if (ReferenceEquals(_root, other._root)) return true;
        var a = _root._data; var b = other._root._data; if (a.Length != b.Length) return false;
        for (var index = 0; index < a.Length; index++) if (a[index] != b[index]) return false;
        return true;
    }
}

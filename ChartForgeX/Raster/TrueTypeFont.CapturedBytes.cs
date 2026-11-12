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
    /// collection's other members. Glyphs, metrics and layout are unchanged; null for a standalone face.
    /// </summary>
    internal TrueTypeFont? CopyCollectionMember() {
        if (IsSingleCapturedFace || _collectionIndex is not int index) return null;
        var data = _root._data; var directory = CheckedOffset(data, ReadUInt32(data, 12 + index * 4));
        var count = ReadUInt16(data, directory + 4); var size = 12 + count * 16;
        for (var i = 0; i < count; i++) size = checked(size + (int)((ReadUInt32(data, directory + 12 + i * 16 + 12) + 3) & ~3u));
        var member = new byte[size];
        Buffer.BlockCopy(data, directory, member, 0, 12 + count * 16);
        var offset = 12 + count * 16;
        for (var i = 0; i < count; i++) {
            var record = directory + 12 + i * 16; var length = (int)ReadUInt32(data, record + 12);
            Buffer.BlockCopy(data, CheckedOffset(data, ReadUInt32(data, record + 8)), member, offset, length);
            var target = 12 + i * 16 + 8;
            member[target] = (byte)(offset >> 24); member[target + 1] = (byte)(offset >> 16); member[target + 2] = (byte)(offset >> 8); member[target + 3] = (byte)offset;
            offset += (length + 3) & ~3;
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

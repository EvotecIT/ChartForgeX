using System;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    // Source ownership remains in the existing file/memory reader. No path is reopened to snapshot portable data.
    internal int CapturedByteLength => _root._data.Length;
    internal TrueTypeFont? SelectCapturedFace(int? collectionIndex, string? faceName) => TryLoad(_root._data, collectionIndex, faceName);
    internal byte[] CopyCapturedBytes(int maximumBytes) {
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (_root._data.Length > maximumBytes) throw new NotSupportedException("Owned font bytes exceed the portable per-root limit.");
        return (byte[])_root._data.Clone();
    }
    internal bool SameCapturedBytes(TrueTypeFont other) {
        if (ReferenceEquals(_root, other._root)) return true;
        var a = _root._data; var b = other._root._data; if (a.Length != b.Length) return false;
        for (var index = 0; index < a.Length; index++) if (a[index] != b[index]) return false;
        return true;
    }
}

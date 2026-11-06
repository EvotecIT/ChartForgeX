using System;
using System.Security.Cryptography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private string? _capturedByteIdentity;
    /// <summary>Hashes already-owned immutable bytes, never reopening an installed font path.</summary>
    internal string CapturedByteIdentity {
        get {
            lock (_root._viewLock) {
                if (_root._capturedByteIdentity != null) return _root._capturedByteIdentity;
                using var sha = SHA256.Create();
                return _root._capturedByteIdentity = BitConverter.ToString(sha.ComputeHash(_root._data)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }

    /// <summary>Names the outlines this face draws by content, so separately loaded or viewed copies of one face are interchangeable.</summary>
    internal string OutlineIdentity => CapturedByteIdentity + "#" + _head.ToString(System.Globalization.CultureInfo.InvariantCulture) + "#" + _variationSettings.Key;
}

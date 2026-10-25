using System;
using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Raster;

/// <summary>Bounds exact RGB nearest-colour searches using conservative candidates for 16³ cells.</summary>
/// <remarks>A cell stores every palette colour whose distance to the cell can beat an upper
/// bound valid throughout that cell. Queries still compare the actual RGB value, including
/// palette-order ties; a cell never substitutes its centre's nearest colour for a pixel.</remarks>
internal sealed class GifNearestColorIndex {
    private readonly byte[] _colors;
    private readonly byte[][] _candidates = new byte[4096][];

    public GifNearestColorIndex(GifPalette palette, CancellationToken cancellationToken) {
        _colors = palette.Colors;
        var slots = palette.HasTransparency ? palette.TransparentIndex : 256;
        for (var cell = 0; cell < _candidates.Length; cell++) {
            if ((cell & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var r = (cell >> 8) * 16;
            var g = ((cell >> 4) & 15) * 16;
            var b = (cell & 15) * 16;
            var upper = int.MaxValue;
            for (var i = 0; i < slots; i++) {
                var at = i * 3;
                var dr = Farthest(_colors[at], r);
                var dg = Farthest(_colors[at + 1], g);
                var db = Farthest(_colors[at + 2], b);
                upper = Math.Min(upper, dr * dr + dg * dg + db * db);
            }
            var candidates = new List<byte>();
            for (var i = 0; i < slots; i++) {
                var at = i * 3;
                var dr = Nearest(_colors[at], r);
                var dg = Nearest(_colors[at + 1], g);
                var db = Nearest(_colors[at + 2], b);
                if (dr * dr + dg * dg + db * db <= upper) candidates.Add((byte)i);
            }
            _candidates[cell] = candidates.ToArray();
        }
    }

    /// <summary>Returns the earliest palette entry at the minimum squared RGB distance.</summary>
    public byte Find(byte red, byte green, byte blue) {
        var candidates = _candidates[((red >> 4) << 8) | ((green >> 4) << 4) | (blue >> 4)];
        var best = (byte)0;
        var bestDistance = int.MaxValue;
        foreach (var index in candidates) {
            var at = index * 3;
            var dr = red - _colors[at];
            var dg = green - _colors[at + 1];
            var db = blue - _colors[at + 2];
            var distance = dr * dr + dg * dg + db * db;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = index;
        }
        return best;
    }

    private static int Farthest(int value, int low) => Math.Max(Math.Abs(value - low), Math.Abs(value - low - 15));
    private static int Nearest(int value, int low) => value < low ? low - value : value > low + 15 ? value - low - 15 : 0;
}

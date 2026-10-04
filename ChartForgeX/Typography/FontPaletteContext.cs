using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>A document's named base palette, resolved against each actual primary or fallback face.</summary>
internal sealed class FontPaletteContext {
    private static long _serial;
    internal long Identity { get; } = Interlocked.Increment(ref _serial);
    private readonly IReadOnlyDictionary<string, int> _families;
    internal FontPaletteContext(IReadOnlyDictionary<string, int> families) => _families = families;

    internal int Resolve(TrueTypeFont face) => face.SelectedFamily != null && _families.TryGetValue(face.SelectedFamily, out var selected) ? selected : 0;
}

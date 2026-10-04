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
    private readonly Dictionary<TrueTypeFont, int> _resolved = new();

    internal FontPaletteContext(IReadOnlyDictionary<string, int> families) => _families = families;

    internal int Resolve(TrueTypeFont face) {
        var root = face.Root;
        lock (_resolved) {
            if (_resolved.TryGetValue(root, out var selected)) return selected;
            selected = 0; var matched = false;
            foreach (var pair in _families) {
                var registered = FontRegistry.Ranked(pair.Key, face.FallbackWeight, face.FallbackItalic);
                var candidates = registered.Count == 0 ? InstalledFontCatalog.Ranked(pair.Key, face.FallbackWeight, face.FallbackItalic) : registered;
                foreach (var candidate in candidates) {
                    var loaded = TrueTypeFont.TryLoadFromPath(candidate.Path, candidate.CollectionIndex);
                    if (loaded != null && ReferenceEquals(loaded.Root, root)) { selected = pair.Value; matched = true; break; }
                }
                if (matched) break;
            }
            if (_resolved.Count >= 128) _resolved.Clear();
            return _resolved[root] = selected;
        }
    }
}

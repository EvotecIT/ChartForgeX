using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>
/// The faces a run falls back to, in order, for characters its own face does not cover: the rest
/// of the requested CSS stack, then every registered family (<see cref="FontRegistry"/>), then the
/// platform's broad-coverage fonts where they are installed. Each face is matched to the run's
/// weight and slant. Coverage is answered from each file's <c>cmap</c> alone, so a face is loaded
/// only once it actually draws a character, and every answer is cached per code point.
/// </summary>
internal sealed class FontFallbackChain {
    private const int MaximumChains = 64;
    private static readonly object Gate = new();
    private static Dictionary<string, FontFallbackChain> _chains = new(StringComparer.Ordinal);
    private static int _version;

    // Broad-coverage families by platform, in the order they are tried; missing ones are skipped.
    private static readonly string[] WindowsFamilies = {
        "Segoe UI", "Segoe UI Symbol", "Segoe UI Emoji", "Microsoft YaHei", "Microsoft JhengHei", "Yu Gothic", "Malgun Gothic",
        "Nirmala UI", "Leelawadee UI", "Ebrima", "Gadugi", "Arial Unicode MS", "Segoe UI Historic", "Myanmar Text", "Mongolian Baiti",
        "Microsoft Himalaya", "Sylfaen", "Cambria Math"
    };

    private static readonly string[] LinuxFamilies = {
        "Noto Sans", "DejaVu Sans", "Noto Sans CJK SC", "Noto Sans CJK JP", "Noto Sans CJK TC", "Noto Sans CJK KR", "Noto Sans SC",
        "Noto Sans JP", "Noto Sans TC", "Noto Sans KR", "Noto Sans Arabic", "Noto Naskh Arabic", "Noto Sans Hebrew", "Noto Sans Devanagari",
        "Noto Sans Bengali", "Noto Sans Tamil", "Noto Sans Thai", "Noto Sans Armenian", "Noto Sans Georgian", "Noto Sans Ethiopic",
        "Noto Sans Symbols", "Noto Sans Symbols 2", "Noto Sans Math", "Noto Emoji", "WenQuanYi Zen Hei", "WenQuanYi Micro Hei",
        "Droid Sans Fallback", "Liberation Sans", "FreeSans", "Unifont"
    };

    private static readonly string[] MacFamilies = {
        "Helvetica Neue", "Lucida Grande", "PingFang SC", "PingFang TC", "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Apple SD Gothic Neo",
        "Geeza Pro", "Arial Hebrew", "Kohinoor Devanagari", "Thonburi", "Apple Symbols", "Arial Unicode MS", "STHeiti"
    };

    // Families whose faces carry emoji outlines; a cluster asking for emoji presentation tries them first.
    private static readonly string[] EmojiFamilies = { "Segoe UI Emoji", "Noto Emoji", "Noto Color Emoji", "Apple Color Emoji" };

    private readonly object _lock = new();
    private readonly string[] _families;
    private readonly int _weight;
    private readonly bool _italic;
    private readonly FaceCandidate[]?[] _candidates;
    private readonly int[] _emojiFirst;
    private readonly Dictionary<int, TrueTypeFont?> _byCodePoint = new();
    private readonly Dictionary<int, TrueTypeFont?> _emojiByCodePoint = new();

    private FontFallbackChain(string[] families, int weight, bool italic) {
        _families = families;
        _weight = weight;
        _italic = italic;
        _candidates = new FaceCandidate[]?[families.Length];
        var order = new List<int>();
        for (var i = 0; i < families.Length; i++) if (Array.IndexOf(EmojiFamilies, families[i]) >= 0) order.Add(i);
        for (var i = 0; i < families.Length; i++) if (!order.Contains(i)) order.Add(i);
        _emojiFirst = order.ToArray();
    }

    /// <summary>Changes whenever registered fonts change; cached shaping keyed to an older version is stale.</summary>
    internal static int Version => System.Threading.Volatile.Read(ref _version);

    /// <summary>The chain for text drawn with <paramref name="primary"/>.</summary>
    internal static FontFallbackChain For(TrueTypeFont primary) {
        var stack = primary.FallbackFamilies;
        var key = string.Join("\n", stack) + "|" + primary.FallbackWeight.ToString(System.Globalization.CultureInfo.InvariantCulture) + (primary.FallbackItalic ? "|i" : "|n");
        lock (Gate) {
            if (_chains.TryGetValue(key, out var chain)) return chain;
            var families = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in stack) if (seen.Add(family)) families.Add(family);
            foreach (var family in FontRegistry.Families) if (seen.Add(family)) families.Add(family);
            foreach (var family in PlatformFamilies()) if (seen.Add(family)) families.Add(family);
            chain = new FontFallbackChain(families.ToArray(), primary.FallbackWeight, primary.FallbackItalic);
            if (_chains.Count >= MaximumChains) _chains = new Dictionary<string, FontFallbackChain>(StringComparer.Ordinal);
            _chains[key] = chain;
            return chain;
        }
    }

    /// <summary>Forgets every chain after the registered fonts change.</summary>
    internal static void Reset() {
        lock (Gate) {
            _chains = new Dictionary<string, FontFallbackChain>(StringComparer.Ordinal);
            System.Threading.Interlocked.Increment(ref _version);
        }
    }

    /// <summary>The families of the chain, in the order they are tried.</summary>
    internal IReadOnlyList<string> Families => _families;

    /// <summary>
    /// The first fallback face that covers <paramref name="codePoint"/>, or null when none does. With
    /// <paramref name="emoji"/>, emoji fonts are tried before the rest of the chain.
    /// </summary>
    internal TrueTypeFont? FaceFor(int codePoint, bool emoji = false) {
        lock (_lock) {
            var cache = emoji ? _emojiByCodePoint : _byCodePoint;
            if (cache.TryGetValue(codePoint, out var cached)) return cached;
            var single = new[] { codePoint };
            var found = Find(single, null, emoji, out _);
            cache[codePoint] = found;
            return found;
        }
    }

    /// <summary>
    /// The first fallback face that covers a whole cluster: its <paramref name="composed"/> (NFC)
    /// form, preferred within a face, or its <paramref name="sequence"/> of base and marks.
    /// </summary>
    internal TrueTypeFont? FaceForCluster(IReadOnlyList<int>? composed, IReadOnlyList<int> sequence, bool emoji, out bool useComposed) {
        lock (_lock) return Find(sequence, composed, emoji, out useComposed);
    }

    // Families in chain order; within a family, its faces from the closest weight and slant outward,
    // so a bold run whose bold face lacks a script still finds the family's regular face.
    private TrueTypeFont? Find(IReadOnlyList<int> sequence, IReadOnlyList<int>? composed, bool emoji, out bool useComposed) {
        for (var n = 0; n < _families.Length; n++) {
            var index = emoji ? _emojiFirst[n] : n;
            foreach (var candidate in CandidatesAt(index)) {
                if (composed != null && candidate.CoversAll(composed)) {
                    useComposed = true;
                    return candidate.Face;
                }

                if (candidate.CoversAll(sequence)) {
                    useComposed = false;
                    return candidate.Face;
                }
            }
        }

        useComposed = false;
        return null;
    }

    private FaceCandidate[] CandidatesAt(int index) {
        var candidates = _candidates[index];
        if (candidates != null) return candidates;
        var family = _families[index];
        var registered = FontRegistry.Ranked(family, _weight, _italic);
        var faces = registered.Count > 0 ? registered : InstalledFontCatalog.Ranked(family, _weight, _italic);
        candidates = new FaceCandidate[faces.Count];
        for (var i = 0; i < faces.Count; i++) candidates[i] = new FaceCandidate(faces[i]);
        _candidates[index] = candidates;
        return candidates;
    }

    private static IEnumerable<string> PlatformFamilies() {
        if (Path.DirectorySeparatorChar == '\\') return WindowsFamilies;
        return Directory.Exists("/System/Library/Fonts") ? MacFamilies : LinuxFamilies;
    }

    // Reads only the table directory and the cmap table of one face of a font file.
    private static FontCmap? ReadCmap(string path, int? collectionIndex) {
        try {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var header = Read(stream, 0, 12);
            if (header == null) return null;
            long directory = 0;
            if (UInt32(header, 0) == 0x74746366) {
                var entry = Read(stream, 12 + (collectionIndex ?? 0) * 4, 4);
                if (entry == null) return null;
                directory = UInt32(entry, 0);
            }

            var offsetTable = Read(stream, directory, 12);
            if (offsetTable == null) return null;
            var tableCount = (offsetTable[4] << 8) | offsetTable[5];
            var records = Read(stream, directory + 12, tableCount * 16);
            if (records == null) return null;
            for (var i = 0; i < tableCount; i++) {
                if (records[i * 16] != 'c' || records[i * 16 + 1] != 'm' || records[i * 16 + 2] != 'a' || records[i * 16 + 3] != 'p') continue;
                var length = UInt32(records, i * 16 + 12);
                if (length > 16 * 1024 * 1024) return null;
                var table = Read(stream, UInt32(records, i * 16 + 8), (int)length);
                return table == null ? null : FontCmap.Read(table, 0);
            }
        } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException) {
        }

        return null;
    }

    private static byte[]? Read(FileStream stream, long offset, int length) {
        if (offset < 0 || length < 0 || offset + length > stream.Length) return null;
        var buffer = new byte[length];
        stream.Position = offset;
        var read = 0;
        while (read < length) {
            var chunk = stream.Read(buffer, read, length - read);
            if (chunk <= 0) return null;
            read += chunk;
        }

        return buffer;
    }

    private static uint UInt32(byte[] data, int offset) => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

    // A fallback face known by its cmap; the cmap is read on the first question and the outlines
    // load the first time the face draws something.
    private sealed class FaceCandidate {
        private readonly InstalledFontFace _face;
        private FontCmap? _cmap;
        private TrueTypeFont? _font;
        private bool _loaded;

        public FaceCandidate(InstalledFontFace face) => _face = face;

        public bool CoversAll(IReadOnlyList<int> codePoints) {
            _cmap ??= ReadCmap(_face.Path, _face.CollectionIndex) ?? FontCmap.Empty;
            foreach (var cp in codePoints) if (_cmap.Map(cp) == 0) return false;
            return Face != null;
        }

        public TrueTypeFont? Face {
            get {
                if (_loaded) return _font;
                _loaded = true;
                _font = TrueTypeFont.TryLoadFromPath(_face.Path, _face.CollectionIndex);
                return _font;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>
/// An OpenType face read without a platform font engine: TrueType (<c>glyf</c>) or CFF/CFF2
/// outlines, <c>kern</c> and GPOS pair kerning. Text that needs more than the face itself, such as
/// characters it does not cover, right-to-left runs, or Arabic joining, is shaped by
/// <see cref="TextShaper"/> into glyphs from this face and its fallback faces.
/// </summary>
internal sealed partial class TrueTypeFont {
    /// <summary>Shares immutable font data while giving a rendering context its own face identity.</summary>
    internal TrueTypeFont WithRenderingIdentity() => (TrueTypeFont)MemberwiseClone();
    internal const double ObliqueShear = 0.22;
    private const string CoverageProbe = "ChartForgeX 0123456789";
    private static readonly object FontCacheLock = new();
    private static readonly Dictionary<string, TrueTypeFont?> FontCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly byte[] _data;
    private readonly Dictionary<string, int> _tables;
    private readonly FontCmap _cmap;
    private readonly CompactFontOutlines? _compact;
    private readonly int _glyf;
    private readonly int _head;
    private readonly int _hmtx;
    private readonly int _gpos;
    private readonly int _kern;
    private readonly int _loca;
    private readonly int _name;
    private readonly int _os2;
    private readonly int _unitsPerEm;
    private readonly short _ascender;
    private readonly short _descender;
    private readonly ushort _numGlyphs;
    private readonly ushort _numHMetrics;
    private readonly short _indexToLocFormat;
    private readonly int? _collectionIndex;
    private readonly string[] _fallbackFamilies;
    private readonly TrueTypeFont _root;
    private readonly object _viewLock = new();
    private Dictionary<string, TrueTypeFont>? _views;
    private double? _xHeight;
    private double? _capHeight;

    private TrueTypeFont(byte[] data, Dictionary<string, int> tables, int? collectionIndex, CompactFontOutlines? compact, TrueTypeFont? root, string[] fallbackFamilies) {
        _data = data;
        _tables = tables;
        _collectionIndex = collectionIndex;
        _compact = compact;
        _root = root ?? this;
        _fallbackFamilies = fallbackFamilies;
        _cmap = FontCmap.Read(data, tables["cmap"]);
        _glyf = tables.TryGetValue("glyf", out var glyf) ? glyf : -1;
        _loca = tables.TryGetValue("loca", out var loca) ? loca : -1;
        _head = tables["head"];
        var hhea = tables["hhea"];
        _hmtx = tables["hmtx"];
        _gpos = tables.TryGetValue("GPOS", out var gpos) ? gpos : -1;
        _kern = tables.TryGetValue("kern", out var kern) ? kern : -1;
        _name = tables.TryGetValue("name", out var name) ? name : -1;
        _os2 = tables.TryGetValue("OS/2", out var os2) && os2 + 64 <= data.Length ? os2 : -1;
        _unitsPerEm = ReadUInt16(_data, _head + 18);
        _indexToLocFormat = ReadInt16(_data, _head + 50);
        _ascender = ReadInt16(_data, hhea + 4);
        _descender = ReadInt16(_data, hhea + 6);
        _numHMetrics = ReadUInt16(_data, hhea + 34);
        _numGlyphs = ReadUInt16(_data, tables["maxp"] + 4);
    }

    public static TrueTypeFont? TryLoadDefault() {
        return TryLoadDefault(out _);
    }

    internal static TrueTypeFont? TryLoadDefault(out string? resolvedPath) {
        return TryLoadForFamily(null, out resolvedPath);
    }

    internal static TrueTypeFont? TryLoadForFamily(string? fontFamily, out string? resolvedPath) {
        foreach (var path in CandidatePaths(fontFamily)) {
            var font = TryLoadFromPath(path);
            if (font != null && font.HasGlyphs(CoverageProbe)) {
                resolvedPath = path;
                return font;
            }
        }

        resolvedPath = null;
        return null;
    }

    internal static PngFontInfo ResolveInfo(string? path, int? collectionIndex, string? faceName, string? themeFontFamily) {
        var requestedPath = FullPathOrNull(path);
        if (requestedPath != null) {
            var requested = TryLoadFromPath(requestedPath, collectionIndex, faceName);
            if (requested != null) return new PngFontInfo(PngFontSource.Requested, themeFontFamily, requestedPath, collectionIndex, faceName, requestedPath, requested.CollectionIndex, requested.DisplayName);
        }

        var automatic = TypographyFontResolver.ResolveFace(themeFontFamily, 400, italic: false);
        if (automatic.Font != null) return new PngFontInfo(PngFontSource.Automatic, themeFontFamily, requestedPath, collectionIndex, faceName, automatic.Path, automatic.Font.CollectionIndex, automatic.Font.DisplayName);
        return new PngFontInfo(PngFontSource.BuiltIn, themeFontFamily, requestedPath, collectionIndex, faceName, null, null, "ChartForgeX Tiny");
    }

    public static TrueTypeFont? TryLoadFromPath(string? path) => TryLoadFromPath(path, null, null);

    public static TrueTypeFont? TryLoadFromPath(string? path, int? collectionIndex) => TryLoadFromPath(path, collectionIndex, null);

    public static TrueTypeFont? TryLoadFromPath(string? path, int? collectionIndex, string? faceName) {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try {
            var fullPath = Path.GetFullPath(path);
            var cacheKey = fullPath + "#" + (collectionIndex.HasValue ? collectionIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "auto") + "#" + (faceName ?? string.Empty);
            lock (FontCacheLock) {
                if (FontCache.TryGetValue(cacheKey, out var cached)) return cached;
            }

            var font = File.Exists(fullPath) ? TryLoad(File.ReadAllBytes(fullPath), collectionIndex, faceName) : null;
            lock (FontCacheLock) {
                // Two threads may read the same file; both get the instance cached first.
                if (FontCache.TryGetValue(cacheKey, out var raced)) return raced;
                FontCache[cacheKey] = font;
            }

            return font;
        } catch (IOException) {
        } catch (UnauthorizedAccessException) {
        } catch (ArgumentException) {
        } catch (NotSupportedException) {
        } catch (IndexOutOfRangeException) {
        }

        return null;
    }

    public static TrueTypeFont? TryLoad(byte[] data) => TryLoad(data, null, null);

    public static TrueTypeFont? TryLoad(byte[] data, int? collectionIndex) => TryLoad(data, collectionIndex, null);

    public static TrueTypeFont? TryLoad(byte[] data, int? collectionIndex, string? faceName) {
        try {
            if (data.Length < 12) return null;
            var scaler = ReadUInt32(data, 0);
            if (scaler == 0x74746366) {
                var fontCount = ReadUInt32(data, 8);
                if (collectionIndex.HasValue) {
                    if (collectionIndex.Value >= fontCount) return null;
                    var indexedFont = TryLoad(data, CheckedOffset(data, ReadUInt32(data, 12 + collectionIndex.Value * 4)), collectionIndex.Value);
                    return indexedFont != null && indexedFont.MatchesName(faceName) ? indexedFont : null;
                }

                for (var i = 0; i < fontCount; i++) {
                    var directoryOffset = CheckedOffset(data, ReadUInt32(data, 12 + i * 4));
                    var font = TryLoad(data, directoryOffset, (int)i);
                    if (font != null && font.HasGlyphs(CoverageProbe) && font.MatchesName(faceName)) return font;
                }

                return null;
            }

            if (collectionIndex.HasValue && collectionIndex.Value > 0) return null;
            var standalone = TryLoad(data, 0, null);
            return standalone != null && standalone.MatchesName(faceName) ? standalone : null;
        } catch (ArgumentOutOfRangeException) {
            return null;
        } catch (IndexOutOfRangeException) {
            return null;
        }
    }

    private static TrueTypeFont? TryLoad(byte[] data, int directoryOffset, int? collectionIndex) {
        if (directoryOffset < 0 || directoryOffset + 12 > data.Length) return null;
        var scaler = ReadUInt32(data, directoryOffset);
        // TrueType outlines (0x00010000, 'true') or CFF outlines ('OTTO').
        if (scaler != 0x00010000 && scaler != 0x74727565 && scaler != 0x4F54544F) return null;
        var count = ReadUInt16(data, directoryOffset + 4);
        var tables = new Dictionary<string, int>(StringComparer.Ordinal);
        var lengths = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++) {
            var record = directoryOffset + 12 + i * 16;
            if (record + 16 > data.Length) return null;
            var tag = ((char)data[record]).ToString() + (char)data[record + 1] + (char)data[record + 2] + (char)data[record + 3];
            tables[tag] = CheckedOffset(data, ReadUInt32(data, record + 8));
            lengths[tag] = (int)Math.Min(int.MaxValue, ReadUInt32(data, record + 12));
        }

        foreach (var required in new[] { "cmap", "head", "hhea", "hmtx", "maxp" }) if (!tables.ContainsKey(required)) return null;
        if (tables.ContainsKey("glyf") && tables.ContainsKey("loca")) return new TrueTypeFont(data, tables, collectionIndex, null, null, Array.Empty<string>());
        CompactFontOutlines? compact = null;
        var unitsPerEm = ReadUInt16(data, tables["head"] + 18);
        if (tables.TryGetValue("CFF ", out var cff)) compact = CompactFontOutlines.TryRead(data, cff, lengths["CFF "], cff2: false, unitsPerEm);
        else if (tables.TryGetValue("CFF2", out var cff2)) compact = CompactFontOutlines.TryRead(data, cff2, lengths["CFF2"], cff2: true, unitsPerEm);
        return compact == null ? null : new TrueTypeFont(data, tables, collectionIndex, compact, null, Array.Empty<string>());
    }

    /// <summary>
    /// This face bound to the families that follow it in a CSS stack: characters it does not cover
    /// are looked up in those families before the registered and platform fallback faces. The same
    /// family list always returns the same instance.
    /// </summary>
    internal TrueTypeFont WithFallbackFamilies(IReadOnlyList<string> families) {
        if (families.Count == 0) return _root;
        var key = string.Join("\n", families);
        lock (_root._viewLock) {
            _root._views ??= new Dictionary<string, TrueTypeFont>(StringComparer.OrdinalIgnoreCase);
            if (_root._views.TryGetValue(key, out var view)) return view;
            var names = new string[families.Count];
            for (var i = 0; i < names.Length; i++) names[i] = families[i];
            view = new TrueTypeFont(_data, _tables, _collectionIndex, _compact, _root, names);
            _root._views[key] = view;
            return view;
        }
    }

    /// <summary>The stack families consulted first for characters this face does not cover.</summary>
    internal IReadOnlyList<string> FallbackFamilies => _fallbackFamilies;

    /// <summary>The face as loaded from its file, without a bound fallback stack.</summary>
    internal TrueTypeFont Root => _root;

    /// <summary>The CSS weight the face declares in its OS/2 table (or its <c>head</c> style bits).</summary>
    internal int Weight => _os2 >= 0 ? Math.Max(1, Math.Min(1000, (int)ReadUInt16(_data, _os2 + 4))) : (ReadUInt16(_data, _head + 44) & 1) != 0 ? 700 : 400;

    /// <summary>True when the face declares itself italic or oblique.</summary>
    internal bool IsItalic => _os2 >= 0 ? (ReadUInt16(_data, _os2 + 62) & 0x0201) != 0 : (ReadUInt16(_data, _head + 44) & 2) != 0;

    /// <summary>True when the face draws CFF (cubic) outlines rather than TrueType ones.</summary>
    internal bool HasCompactOutlines => _compact != null;

    internal int UnitsPerEm => Math.Max(1, _unitsPerEm);

    /// <summary>The x-height in font units: the OS/2 value, or the top of <c>x</c>.</summary>
    internal double XHeight => _xHeight ??= Os2Height(86) ?? GlyphTop('x') ?? _unitsPerEm * 0.5;

    /// <summary>The cap height in font units: the OS/2 value, or the top of <c>H</c>.</summary>
    internal double CapHeight => _capHeight ??= Os2Height(88) ?? GlyphTop('H') ?? _unitsPerEm * 0.7;

    private double? Os2Height(int offset) {
        if (_os2 < 0 || ReadUInt16(_data, _os2) < 2 || !InBounds(_os2 + offset, 2)) return null;
        var value = ReadInt16(_data, _os2 + offset);
        return value > 0 ? value : null;
    }

    public double Measure(string text, double fontSize) => Measure(text, fontSize, italic: false);

    internal double Measure(string text, double fontSize, bool italic) {
        var scale = ScaleFor(fontSize);
        var width = 0.0;
        ushort? previous = null;
        for (var index = 0; index < text.Length;) {
            var codePoint = ReadCodePoint(text, ref index);
            var glyph = TextShaper.IsSimple(codePoint) ? MapGlyph(codePoint) : (ushort)0;
            if (glyph == 0) {
                width = MeasureShaped(text, fontSize);
                break;
            }

            if (previous.HasValue) width += Kerning(previous.Value, glyph) * scale;
            width += AdvanceWidth(glyph) * scale;
            previous = glyph;
        }
        return width + (italic && text.Length > 0 ? ItalicOverhang(fontSize) : 0);
    }

    public double LineHeight(double fontSize) {
        return Math.Max(1, _ascender - _descender) * ScaleFor(fontSize);
    }

    internal double Ascent(double fontSize) => _ascender * ScaleFor(fontSize);

    public bool Draw(RgbaCanvas canvas, double x, double y, string text, ChartColor color, double fontSize) =>
        Draw(canvas, x, y, text, color, fontSize, italic: false);

    internal bool Draw(RgbaCanvas canvas, double x, double y, string text, ChartColor color, double fontSize, bool italic) {
        var scale = ScaleFor(fontSize);
        var cursor = x;
        var baseline = y + _ascender * scale;
        var rendered = false;
        if (!IsSimpleRun(text)) {
            TrueTypeFont? previousFace = null;
            ushort previousGlyph = 0;
            foreach (var shaped in TextShaper.Shape(this, text)) {
                var face = shaped.Face;
                var faceScale = face.ScaleFor(fontSize);
                if (ReferenceEquals(face, previousFace)) cursor += face.Kerning(previousGlyph, shaped.Glyph) * faceScale;
                rendered |= face.DrawGlyph(canvas, shaped.Glyph, cursor, baseline, faceScale, italic, color);
                cursor += face.AdvanceWidth(shaped.Glyph) * faceScale;
                previousFace = face;
                previousGlyph = shaped.Glyph;
            }

            return rendered;
        }

        ushort? previous = null;
        for (var index = 0; index < text.Length;) {
            var glyph = MapGlyph(ReadCodePoint(text, ref index));
            if (previous.HasValue) cursor += Kerning(previous.Value, glyph) * scale;
            rendered |= DrawGlyph(canvas, glyph, cursor, baseline, scale, italic, color);
            cursor += AdvanceWidth(glyph) * scale;
            previous = glyph;
        }

        return rendered;
    }

    private bool DrawGlyph(RgbaCanvas canvas, ushort glyph, double x, double baseline, double scale, bool italic, ChartColor color) {
        var contours = ReadGlyphContours(glyph, new FontTransform(scale, italic ? ObliqueShear * scale : 0, 0, -scale, x, baseline), 0);
        if (contours.Count == 0) return false;
        // Outlines are non-zero wound: variable fonts and composites overlap their contours, and an
        // even-odd fill would punch the overlaps out as holes.
        canvas.FillContours(contours, color, RasterFillRule.NonZero);
        return true;
    }

    private double MeasureShaped(string text, double fontSize) {
        var width = 0.0;
        TrueTypeFont? previousFace = null;
        ushort previousGlyph = 0;
        foreach (var shaped in TextShaper.Shape(this, text)) {
            var face = shaped.Face;
            var faceScale = face.ScaleFor(fontSize);
            if (ReferenceEquals(face, previousFace)) width += face.Kerning(previousGlyph, shaped.Glyph) * faceScale;
            width += face.AdvanceWidth(shaped.Glyph) * faceScale;
            previousFace = face;
            previousGlyph = shaped.Glyph;
        }

        return width;
    }

    // True when every character is drawn by this face exactly as written: no fallback, reordering,
    // joining, or composition. Plain Latin text never leaves this path.
    private bool IsSimpleRun(string text) {
        for (var index = 0; index < text.Length;) {
            var codePoint = ReadCodePoint(text, ref index);
            if (!TextShaper.IsSimple(codePoint) || MapGlyph(codePoint) == 0) return false;
        }

        return true;
    }

    internal static double ItalicOverhang(double fontSize) => Math.Max(0.5, Math.Max(1, fontSize) * ObliqueShear);

    internal int? CollectionIndex => _collectionIndex;

    /// <summary>True when the face covers basic Latin text, which symbol and icon fonts do not.</summary>
    internal bool IsTextFace => HasGlyphs(CoverageProbe);

    private bool HasGlyphs(string value) {
        for (var index = 0; index < value.Length;) {
            var codePoint = ReadCodePoint(value, ref index);
            if (MapGlyph(codePoint) == 0) {
                return false;
            }
        }

        return true;
    }

    internal bool HasGlyph(int codePoint) {
        return MapGlyph(codePoint) != 0;
    }

    internal double ScaleFor(double fontSize) {
        return fontSize / Math.Max(1, _unitsPerEm);
    }

    internal ushort MapGlyph(int codePoint) => _cmap.Map(codePoint);

    internal static int ReadCodePoint(string value, ref int index) {
        var first = value[index++];
        if (!char.IsHighSurrogate(first) || index >= value.Length || !char.IsLowSurrogate(value[index])) {
            return first;
        }
        return char.ConvertToUtf32(first, value[index++]);
    }

    internal int AdvanceWidth(ushort glyph) {
        if (_numHMetrics == 0) return 0;
        var record = _hmtx + Math.Min(glyph, _numHMetrics - 1) * 4;
        return InBounds(record, 2) ? ReadUInt16(_data, record) : 0;
    }

    internal int Kerning(ushort left, ushort right) {
        return KernPairAdjustment(left, right) + GposPairAdjustment(left, right);
    }

    private int KernPairAdjustment(ushort left, ushort right) {
        if (_kern < 0 || _kern + 4 > _data.Length || ReadUInt16(_data, _kern) != 0) return 0;
        var count = ReadUInt16(_data, _kern + 2);
        var p = _kern + 4;
        var adjustment = 0;
        for (var table = 0; table < count; table++) {
            if (p + 6 > _data.Length) break;
            var length = ReadUInt16(_data, p + 2);
            var coverage = ReadUInt16(_data, p + 4);
            var next = p + length;
            if (length < 14 || next <= p || next > _data.Length) break;
            if ((coverage >> 8) == 0) adjustment += KerningFormat0(p, left, right);
            p = next;
        }

        return adjustment;
    }

    private int KerningFormat0(int table, ushort left, ushort right) {
        var pairs = ReadUInt16(_data, table + 6);
        var pairOffset = table + 14;
        var key = ((uint)left << 16) | right;
        var low = 0;
        var high = pairs - 1;
        while (low <= high) {
            var mid = low + (high - low) / 2;
            var record = pairOffset + mid * 6;
            if (record + 6 > _data.Length) return 0;
            var candidate = (ReadUInt32(_data, record) & 0xffffffffu);
            if (candidate == key) return ReadInt16(_data, record + 4);
            if (candidate < key) low = mid + 1;
            else high = mid - 1;
        }

        return 0;
    }

    private int GposPairAdjustment(ushort left, ushort right) {
        if (_gpos < 0 || !InBounds(_gpos, 10) || ReadUInt16(_data, _gpos) != 1) return 0;
        var featureList = _gpos + ReadUInt16(_data, _gpos + 6);
        var lookupList = _gpos + ReadUInt16(_data, _gpos + 8);
        if (!InBounds(featureList, 2) || !InBounds(lookupList, 2)) return 0;

        var adjustment = 0;
        var seen = new HashSet<ushort>();
        foreach (var lookupIndex in GposFeatureLookupIndexes(featureList, "kern")) {
            if (seen.Add(lookupIndex)) adjustment += GposPairAdjustmentFromLookup(lookupList, lookupIndex, left, right);
        }

        return adjustment;
    }

    private IEnumerable<ushort> GposFeatureLookupIndexes(int featureList, string featureTag) {
        var featureCount = ReadUInt16(_data, featureList);
        for (var i = 0; i < featureCount; i++) {
            var record = featureList + 2 + i * 6;
            if (!InBounds(record, 6)) yield break;
            if (!TagEquals(record, featureTag)) continue;
            var feature = featureList + ReadUInt16(_data, record + 4);
            if (!InBounds(feature, 4)) yield break;
            var lookupCount = ReadUInt16(_data, feature + 2);
            for (var lookup = 0; lookup < lookupCount; lookup++) {
                var indexOffset = feature + 4 + lookup * 2;
                if (!InBounds(indexOffset, 2)) yield break;
                yield return ReadUInt16(_data, indexOffset);
            }
        }
    }

    private int GposPairAdjustmentFromLookup(int lookupList, ushort lookupIndex, ushort left, ushort right) {
        var lookupCount = ReadUInt16(_data, lookupList);
        if (lookupIndex >= lookupCount) return 0;
        var lookupOffset = lookupList + 2 + lookupIndex * 2;
        if (!InBounds(lookupOffset, 2)) return 0;
        var lookup = lookupList + ReadUInt16(_data, lookupOffset);
        if (!InBounds(lookup, 6) || ReadUInt16(_data, lookup) != 2) return 0;

        var adjustment = 0;
        var subtableCount = ReadUInt16(_data, lookup + 4);
        for (var i = 0; i < subtableCount; i++) {
            var subtableOffset = lookup + 6 + i * 2;
            if (!InBounds(subtableOffset, 2)) break;
            adjustment += GposPairAdjustmentFromSubtable(lookup + ReadUInt16(_data, subtableOffset), left, right);
        }

        return adjustment;
    }

    private int GposPairAdjustmentFromSubtable(int subtable, ushort left, ushort right) {
        if (!InBounds(subtable, 10) || ReadUInt16(_data, subtable) != 1) return 0;
        var coverage = subtable + ReadUInt16(_data, subtable + 2);
        var valueFormat1 = ReadUInt16(_data, subtable + 4);
        var valueFormat2 = ReadUInt16(_data, subtable + 6);
        var pairSetCount = ReadUInt16(_data, subtable + 8);
        var coverageIndex = CoverageIndex(coverage, left);
        if (coverageIndex < 0 || coverageIndex >= pairSetCount) return 0;

        var pairSetOffset = subtable + 10 + coverageIndex * 2;
        if (!InBounds(pairSetOffset, 2)) return 0;
        var pairSet = subtable + ReadUInt16(_data, pairSetOffset);
        if (!InBounds(pairSet, 2)) return 0;

        var value1Size = ValueRecordSize(valueFormat1);
        var value2Size = ValueRecordSize(valueFormat2);
        var recordSize = 2 + value1Size + value2Size;
        var low = 0;
        var high = ReadUInt16(_data, pairSet) - 1;
        while (low <= high) {
            var mid = low + (high - low) / 2;
            var record = pairSet + 2 + mid * recordSize;
            if (!InBounds(record, recordSize)) return 0;
            var candidate = ReadUInt16(_data, record);
            if (candidate == right) return ReadValueRecordXAdvance(record + 2, valueFormat1);
            if (candidate < right) low = mid + 1;
            else high = mid - 1;
        }

        return 0;
    }

    private int CoverageIndex(int coverage, ushort glyph) {
        if (!InBounds(coverage, 4)) return -1;
        var format = ReadUInt16(_data, coverage);
        if (format == 1) {
            var count = ReadUInt16(_data, coverage + 2);
            var low = 0;
            var high = count - 1;
            while (low <= high) {
                var mid = low + (high - low) / 2;
                var offset = coverage + 4 + mid * 2;
                if (!InBounds(offset, 2)) return -1;
                var candidate = ReadUInt16(_data, offset);
                if (candidate == glyph) return mid;
                if (candidate < glyph) low = mid + 1;
                else high = mid - 1;
            }

            return -1;
        }

        if (format != 2) return -1;
        var rangeCount = ReadUInt16(_data, coverage + 2);
        for (var i = 0; i < rangeCount; i++) {
            var range = coverage + 4 + i * 6;
            if (!InBounds(range, 6)) return -1;
            var start = ReadUInt16(_data, range);
            var end = ReadUInt16(_data, range + 2);
            if (glyph < start || glyph > end) continue;
            return ReadUInt16(_data, range + 4) + glyph - start;
        }

        return -1;
    }

    private int ReadValueRecordXAdvance(int offset, ushort valueFormat) {
        if ((valueFormat & 0x0001) != 0) offset += 2;
        if ((valueFormat & 0x0002) != 0) offset += 2;
        if ((valueFormat & 0x0004) == 0) return 0;
        return InBounds(offset, 2) ? ReadInt16(_data, offset) : 0;
    }

    private static int ValueRecordSize(ushort valueFormat) {
        var size = 0;
        for (var bit = 1; bit <= 0x0080; bit <<= 1) if ((valueFormat & bit) != 0) size += 2;
        return size;
    }

    private bool TagEquals(int offset, string tag) {
        return InBounds(offset, 4) && _data[offset] == tag[0] && _data[offset + 1] == tag[1] && _data[offset + 2] == tag[2] && _data[offset + 3] == tag[3];
    }

    private static ushort ReadUInt16(byte[] data, int offset) => (ushort)((data[offset] << 8) | data[offset + 1]);
    private static short ReadInt16(byte[] data, int offset) => (short)ReadUInt16(data, offset);
    private static double ReadF2Dot14(byte[] data, int offset) => ReadInt16(data, offset) / 16384.0;
    private static uint ReadUInt32(byte[] data, int offset) => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    private bool InBounds(int offset, int length) => offset >= 0 && length >= 0 && offset <= _data.Length - length;
    private static int CheckedOffset(byte[] data, uint offset) {
        if (offset > int.MaxValue || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        return (int)offset;
    }

    private static string? FullPathOrNull(string? path) {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try {
            return Path.GetFullPath(path);
        } catch (ArgumentException) {
        } catch (NotSupportedException) {
        }

        return path;
    }
}

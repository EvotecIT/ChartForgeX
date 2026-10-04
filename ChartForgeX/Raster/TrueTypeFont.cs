using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>
/// An OpenType face read without a platform font engine: TrueType (<c>glyf</c>) or CFF/CFF2
/// outlines, <c>kern</c> and OpenType layout tables. <see cref="TextShaper"/> selects
/// fallback faces, resolves bidi and joining, and executes substitutions and positioning
/// before measurement and painting consume the same glyph run.
/// </summary>
internal sealed partial class TrueTypeFont {
    /// <summary>Shares immutable font data while giving a rendering context its own face identity.</summary>
    internal TrueTypeFont WithRenderingIdentity() => (TrueTypeFont)MemberwiseClone();
    internal const double ObliqueShear = 0.22;
    private const string CoverageProbe = "ChartForgeX 0123456789";
    private readonly byte[] _data;
    private readonly Dictionary<string, int> _tables;
    private readonly FontCmap _cmap;
    private readonly CompactFontOutlines? _compact;
    private readonly int _glyf;
    private readonly int _head;
    private readonly int _hmtx;
    private readonly OpenTypeLayout _layout;
    private readonly ColorFontData? _colors;
    private readonly IReadOnlyDictionary<string, int> _tableLengths;
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
    private readonly int? _fallbackWeight;
    private readonly bool? _fallbackItalic;
    private readonly string? _languageTag;
    private readonly TrueTypeFont _root;
    private readonly object _viewLock = new();
    private Dictionary<string, TrueTypeFont>? _views;
    private double? _xHeight;
    private double? _capHeight;

    private TrueTypeFont(byte[] data, Dictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int? collectionIndex, CompactFontOutlines? compact, TrueTypeFont? root, string[] fallbackFamilies, int? fallbackWeight = null, bool? fallbackItalic = null, string? languageTag = null, FontVariationSettings? variations = null) {
        _data = data;
        _tables = tables;
        _tableLengths = lengths;
        _collectionIndex = collectionIndex;
        _compact = compact;
        _root = root ?? this;
        _fallbackFamilies = fallbackFamilies;
        _fallbackWeight = fallbackWeight;
        _fallbackItalic = fallbackItalic;
        _languageTag = languageTag;
        _cmap = FontCmap.Read(data, tables["cmap"]);
        _glyf = tables.TryGetValue("glyf", out var glyf) ? glyf : -1;
        _loca = tables.TryGetValue("loca", out var loca) ? loca : -1;
        _head = tables["head"];
        var hhea = tables["hhea"];
        _hmtx = tables["hmtx"];
        _kern = tables.TryGetValue("kern", out var kern) ? kern : -1;
        _name = tables.TryGetValue("name", out var name) ? name : -1;
        _os2 = tables.TryGetValue("OS/2", out var os2) && os2 + 64 <= data.Length ? os2 : -1;
        _unitsPerEm = ReadUInt16(_data, _head + 18);
        _indexToLocFormat = ReadInt16(_data, _head + 50);
        _ascender = ReadInt16(_data, hhea + 4);
        _descender = ReadInt16(_data, hhea + 6);
        _numHMetrics = ReadUInt16(_data, hhea + 34);
        _numGlyphs = ReadUInt16(_data, tables["maxp"] + 4);
        _variationSettings = variations ?? FontVariationSettings.Default;
        _variation = FontVariationContext.Create(data, tables, lengths, _variationSettings);
        if (_variation?.HasNonzeroCoordinates == true && compact != null) _compact = compact.WithVariation(_variation);
        _glyphVariations = _variation?.HasNonzeroCoordinates != true ? null : GlyphVariationData.Create(data, tables, lengths, _numGlyphs, _indexToLocFormat != 0, _variation.Coordinates);
        _hvarTable = _variation == null ? null : VariationTable("HVAR"); _mvarTable = _variation == null ? null : VariationTable("MVAR");
        _horizontalVariations = VariationStore(_hvarTable, 4, true); _metricVariations = VariationStore(_mvarTable, 10, false);
        var layout = root?._layout ?? new OpenTypeLayout(data, tables, lengths, _numGlyphs);
        _layout = languageTag == null && _variation == null ? layout : layout.WithContext(languageTag, _variation);
        _colors = root?._colors ?? ColorFontData.Create(data, tables, lengths, _numGlyphs, _unitsPerEm);
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
                    if (font != null && font.IsTextFace && font.MatchesName(faceName)) return font;
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
        if (tables.ContainsKey("glyf") && tables.ContainsKey("loca")) return new TrueTypeFont(data, tables, lengths, collectionIndex, null, null, Array.Empty<string>());
        CompactFontOutlines? compact = null;
        var unitsPerEm = ReadUInt16(data, tables["head"] + 18);
        if (tables.TryGetValue("CFF ", out var cff)) compact = CompactFontOutlines.TryRead(data, cff, lengths["CFF "], cff2: false, unitsPerEm);
        else if (tables.TryGetValue("CFF2", out var cff2)) compact = CompactFontOutlines.TryRead(data, cff2, lengths["CFF2"], cff2: true, unitsPerEm);
        var bitmapOnly = tables.ContainsKey("sbix") || tables.ContainsKey("CBDT") && tables.ContainsKey("CBLC");
        return compact == null && !bitmapOnly ? null : new TrueTypeFont(data, tables, lengths, collectionIndex, compact, null, Array.Empty<string>());
    }

    /// <summary>
    /// This face bound to the families that follow it in a CSS stack: characters it does not cover
    /// are looked up in those families before the registered and platform fallback faces. The same
    /// family list always returns the same instance.
    /// </summary>
    internal TrueTypeFont WithFallbackFamilies(IReadOnlyList<string> families, int? weight = null, bool? italic = null) =>
        View(families, weight, italic, _languageTag, _variationSettings);

    /// <summary>Binds immutable language selection to a face identity, including its shaped-run cache.</summary>
    internal TrueTypeFont WithLanguage(string? tag) => tag == _languageTag ? this : View(_fallbackFamilies, _fallbackWeight, _fallbackItalic, tag, _variationSettings);
    internal string? LanguageTag => _languageTag;

    private TrueTypeFont View(IReadOnlyList<string> families, int? weight, bool? italic, string? languageTag, FontVariationSettings variations) {
        var requestedWeight = weight ?? _root.Weight;
        var requestedItalic = italic ?? _root.IsItalic;
        if (families.Count == 0 && requestedWeight == _root.Weight && requestedItalic == _root.IsItalic && languageTag == null && variations.Count == 0) return _root;
        var key = string.Join("\n", families) + "|" + requestedWeight.ToString(System.Globalization.CultureInfo.InvariantCulture) + (requestedItalic ? "|i" : "|n") + "|" + languageTag + "|" + variations.Key;
        lock (_root._viewLock) {
            _root._views ??= new Dictionary<string, TrueTypeFont>(StringComparer.Ordinal);
            if (_root._views.TryGetValue(key, out var view)) return view;
            var names = new string[families.Count];
            for (var i = 0; i < names.Length; i++) names[i] = families[i];
            view = new TrueTypeFont(_data, _tables, _tableLengths, _collectionIndex, _root._compact, _root, names, requestedWeight, requestedItalic, languageTag, variations);
            if (_root._views.Count >= 32) _root._views.Clear();
            _root._views[key] = view;
            return view;
        }
    }

    /// <summary>The stack families consulted first for characters this face does not cover.</summary>
    internal IReadOnlyList<string> FallbackFamilies => _fallbackFamilies;

    /// <summary>Requested fallback style, which can differ from a primary face requiring synthetic emphasis or slant.</summary>
    internal int FallbackWeight => _fallbackWeight ?? Weight;
    internal bool FallbackItalic => _fallbackItalic ?? IsItalic;

    /// <summary>The face as loaded from its file, without a bound fallback stack.</summary>
    internal TrueTypeFont Root => _root;

    /// <summary>The CSS weight the face declares in its OS/2 table (or its <c>head</c> style bits).</summary>
    internal int Weight => _os2 >= 0 ? Math.Max(1, Math.Min(1000, (int)ReadUInt16(_data, _os2 + 4))) : (ReadUInt16(_data, _head + 44) & 1) != 0 ? 700 : 400;

    /// <summary>True when the face declares itself italic or oblique.</summary>
    internal bool IsItalic => _os2 >= 0 ? (ReadUInt16(_data, _os2 + 62) & 0x0201) != 0 : (ReadUInt16(_data, _head + 44) & 2) != 0;

    /// <summary>True when the face draws CFF (cubic) outlines rather than TrueType ones.</summary>
    internal bool HasCompactOutlines => _compact != null;

    internal int UnitsPerEm => Math.Max(1, _unitsPerEm);
    internal OpenTypeLayout Layout => _layout;

    /// <summary>The x-height in font units: the OS/2 value, or the top of <c>x</c>.</summary>
    internal double XHeight => _xHeight ??= Os2Height(86) is double height ? height + MetricVariation("xhgt") : GlyphTop('x') ?? _unitsPerEm * 0.5;

    /// <summary>The cap height in font units: the OS/2 value, or the top of <c>H</c>.</summary>
    internal double CapHeight => _capHeight ??= Os2Height(88) is double height ? height + MetricVariation("cpht") : GlyphTop('H') ?? _unitsPerEm * 0.7;

    private double? Os2Height(int offset) {
        if (_os2 < 0 || ReadUInt16(_data, _os2) < 2 || !InBounds(_os2 + offset, 2)) return null;
        var value = ReadInt16(_data, _os2 + offset);
        return value > 0 ? value : null;
    }

    public double Measure(string text, double fontSize) => Measure(text, fontSize, italic: false);

    internal double Measure(string text, double fontSize, bool italic) {
        if (!IsSimpleRun(text)) return MeasureShaped(text, fontSize) + (italic && text.Length > 0 ? ItalicOverhang(fontSize) : 0);
        var scale = ScaleFor(fontSize);
        var width = 0.0;
        ushort? previous = null;
        for (var index = 0; index < text.Length;) {
            var codePoint = ReadCodePoint(text, ref index);
            var glyph = MapGlyph(codePoint);

            if (previous.HasValue) width += Kerning(previous.Value, glyph) * scale;
            width += AdvanceWidth(glyph) * scale;
            previous = glyph;
        }
        return width + (italic && text.Length > 0 ? ItalicOverhang(fontSize) : 0);
    }

    public double LineHeight(double fontSize) {
        return Math.Max(1, _ascender + AscentVariation - _descender - DescentVariation) * ScaleFor(fontSize);
    }

    internal double Ascent(double fontSize) => (_ascender + AscentVariation) * ScaleFor(fontSize);

    public bool Draw(RgbaCanvas canvas, double x, double y, string text, ChartColor color, double fontSize) =>
        Draw(canvas, x, y, text, color, fontSize, italic: false);

    internal bool Draw(RgbaCanvas canvas, double x, double y, string text, ChartColor color, double fontSize, bool italic, double boldOffset = 0) {
        var scale = ScaleFor(fontSize);
        var cursor = x;
        // Small text sits on a whole pixel and has its x-height and cap height fitted to the grid.
        var fit = GlyphGridFit.Create(canvas, fontSize, y + Ascent(fontSize));
        var baseline = fit?.Baseline ?? y + Ascent(fontSize);
        var rendered = false;
        if (!IsSimpleRun(text)) {
            return DrawGlyphs(canvas, x, y, TextShaper.Shape(this, text, fontSize), color, fontSize, italic, boldOffset: boldOffset);
        }

        ushort? previous = null;
        for (var index = 0; index < text.Length;) {
            var glyph = MapGlyph(ReadCodePoint(text, ref index));
            if (previous.HasValue) cursor += Kerning(previous.Value, glyph) * scale;
            rendered |= DrawGlyph(canvas, glyph, cursor, baseline, scale, italic, color, fit, boldOffset);
            cursor += AdvanceWidth(glyph) * scale;
            previous = glyph;
        }

        return rendered;
    }

    private bool DrawGlyph(RgbaCanvas canvas, ushort glyph, double x, double baseline, double scale, bool italic, ChartColor color, GlyphGridFit? fit, double boldOffset = 0) {
        if (glyph == 0) return false;
        if (canvas.GlyphPaintMode != FontGlyphPaintMode.All) {
            var colored = IsColorGlyph(glyph);
            if (colored != (canvas.GlyphPaintMode == FontGlyphPaintMode.ColourOnly)) return true;
        }
        if (DrawColorGlyph(canvas, glyph, x, baseline, scale, italic, color)) return true;
        return DrawOutlineGlyph(canvas, glyph, x, baseline, scale, italic, color, fit, boldOffset);
    }

    private bool DrawOutlineGlyph(RgbaCanvas canvas, ushort glyph, double x, double baseline, double scale, bool italic, ChartColor color, GlyphGridFit? fit, double boldOffset = 0) {
        var contours = ReadGlyphContours(glyph, new FontTransform(scale, italic ? ObliqueShear * scale : 0, 0, -scale, x, baseline), 0);
        if (contours.Count == 0) return false;
        fit?.Apply(contours, XHeight * scale, CapHeight * scale, !italic && !IsItalic && !HasSelectedAxis("ital") && !HasSelectedAxis("slnt"));
        // Outlines are non-zero wound: variable fonts and composites overlap their contours, and an
        // even-odd fill would punch the overlaps out as holes.
        canvas.FillTextContours(contours, color, boldOffset);
        return true;
    }

    private double MeasureShaped(string text, double fontSize) => MeasureGlyphs(TextShaper.Shape(this, text, fontSize), fontSize);

    // True when every character is drawn by this face exactly as written: no fallback, reordering,
    // joining, composition, or OpenType layout.
    private bool IsSimpleRun(string text) {
        if (_layout.HasLayout("latn")) return false;
        for (var index = 0; index < text.Length;) {
            var codePoint = ReadCodePoint(text, ref index);
            if (!TextShaper.IsSimple(codePoint) || MapGlyph(codePoint) == 0) return false;
            if (_layout.HasLayout(OpenTypeScriptData.Script(codePoint))) return false;
        }

        return true;
    }

    internal static double ItalicOverhang(double fontSize) => Math.Max(0.5, Math.Max(1, fontSize) * ObliqueShear);

    internal int? CollectionIndex => _collectionIndex;

    /// <summary>True when the face covers basic Latin text, which symbol and icon fonts do not.</summary>
    internal bool IsTextFace => HasGlyphs(CoverageProbe) || IsColorFace;

    /// <summary>An authored colour face retains emoji clusters before system emoji fallback.</summary>
    internal bool IsColorFace => _colors != null;

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

    internal double AdvanceWidth(ushort glyph) => AdvanceWidth(glyph, 0);
    private double AdvanceWidth(ushort glyph, int depth) {
        if (_numHMetrics == 0) return 0;
        if (depth < 8 && _horizontalVariations == null && _glyphVariations != null && TryMetricComponent(glyph, out var component))
            return AdvanceWidth(component, depth + 1);
        var record = _hmtx + Math.Min(glyph, _numHMetrics - 1) * 4;
        return InBounds(record, 2) ? Math.Floor(ReadUInt16(_data, record) + AdvanceVariation(glyph) + 0.5) : 0;
    }

    internal int Kerning(ushort left, ushort right) {
        return KernPairAdjustment(left, right);
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

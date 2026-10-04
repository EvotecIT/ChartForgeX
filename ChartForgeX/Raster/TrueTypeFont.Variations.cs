using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private readonly FontVariationSettings _variationSettings;
    private readonly FontVariationContext? _variation;
    private readonly GlyphVariationData? _glyphVariations;
    private readonly FontTableReader? _hvarTable, _mvarTable;
    private readonly ItemVariationStore? _horizontalVariations, _metricVariations;
    internal FontVariationSettings Variations => _variationSettings;
    internal bool HasSelectedAxis(string tag) => _variation != null && _variationSettings.ContainsKey(tag) && Array.IndexOf(_variation.Tags, tag) >= 0;
    internal TrueTypeFont WithVariations(FontVariationSettings settings) => settings.Key == _variationSettings.Key ? this : View(_fallbackFamilies, _fallbackWeight, _fallbackItalic, _languageTag, settings);
    private FontTableReader? VariationTable(string tag) {
        if (!_tables.TryGetValue(tag, out var at) || !_tableLengths.TryGetValue(tag, out var length)) return null;
        try { return new FontTableReader(_data, at, length); } catch (FontLayoutException) { return null; }
    }
    private ItemVariationStore? VariationStore(FontTableReader? table, int field, bool wide) {
        if (_variation?.HasNonzeroCoordinates != true || !table.HasValue) return null;
        try { return ItemVariationStore.Read(table.Value, table.Value.Offset(0, field, optional: true, wide: wide), _variation.Coordinates); }
        catch (FontLayoutException) { return null; }
    }
    private double AdvanceVariation(ushort glyph) {
        if (_horizontalVariations != null && _hvarTable.HasValue) {
            try {
                var index = ItemVariationStore.Map(_hvarTable.Value, _hvarTable.Value.Offset(0, 8, optional: true, wide: true), glyph);
                return _horizontalVariations.Delta(index.Outer, index.Inner);
            } catch (FontLayoutException) { }
        }
        return _glyphVariations?.Get(glyph)?.AdvanceDelta ?? 0;
    }
    private double MetricVariation(string tag) {
        if (_metricVariations == null || !_mvarTable.HasValue) return 0;
        try {
            var table = _mvarTable.Value; var count = table.U16(8); var stride = table.U16(6);
            if (stride < 8) return 0;
            table.Require(12, count * stride);
            var low = 0; var high = count - 1;
            while (low <= high) {
                var middle = low + (high - low) / 2; var at = 12 + middle * stride;
                var compare = string.CompareOrdinal(tag, table.Tag(at));
                if (compare == 0) return _metricVariations.Delta(table.U16(at + 4), table.U16(at + 6));
                if (compare < 0) high = middle - 1; else low = middle + 1;
            }
        } catch (FontLayoutException) { }
        return 0;
    }
    // Without HVAR, USE_MY_METRICS selects the component's interpolated phantom
    // metrics rather than the composite's phantom-point deltas. Transform scale
    // changes the outline; it does not scale the selected advance.
    private bool TryMetricComponent(ushort glyph, out ushort component) {
        component = 0;
        var found = false;
        if (glyph >= _numGlyphs) return false;
        try {
            var glyf = VariationTable("glyf"); var loca = VariationTable("loca");
            if (!glyf.HasValue || !loca.HasValue) return false;
            var begin = _indexToLocFormat == 0 ? loca.Value.U16(glyph * 2) * 2L : loca.Value.U32(glyph * 4);
            var end = _indexToLocFormat == 0 ? loca.Value.U16((glyph + 1) * 2) * 2L : loca.Value.U32((glyph + 1) * 4);
            if (begin == end || begin > int.MaxValue || end > glyf.Value.Length || end < begin) return false;
            var table = glyf.Value; var at = (int)begin;
            if (table.I16(at) >= 0) return false;
            at += 10;
            for (var count = 0; count < 4096; count++) {
                table.Require(at, 4);
                var flags = table.U16(at); var candidate = table.U16(at + 2);
                if ((flags & 0x200) != 0 && candidate < _numGlyphs) { component = (ushort)candidate; found = true; }
                at += 4 + ((flags & 1) != 0 ? 4 : 2) + ((flags & 8) != 0 ? 2 : (flags & 64) != 0 ? 4 : (flags & 128) != 0 ? 8 : 0);
                if (at > end) return false;
                if ((flags & 32) == 0) return found;
            }
        } catch (FontLayoutException) { }
        return false;
    }
    private double GlyphOrigin(ushort glyph, int depth = 0) {
        if (_glyphVariations == null || glyph >= _numGlyphs || depth >= 8) return 0;
        if (TryMetricComponent(glyph, out var component)) return GlyphOrigin(component, depth + 1);
        try {
            var glyf = VariationTable("glyf"); var hmtx = VariationTable("hmtx");
            if (!glyf.HasValue || !hmtx.HasValue) return 0;
            var at = GlyphOffset(glyph);
            // Empty glyphs still have four variation phantom points. A visible
            // composite may select their origin through USE_MY_METRICS.
            if (at == GlyphOffset((ushort)(glyph + 1))) return _glyphVariations.Get(glyph)?.LeftPhantomDelta ?? 0;
            var lsbAt = glyph < _numHMetrics ? glyph * 4 + 2 : _numHMetrics * 4 + (glyph - _numHMetrics) * 2;
            var baseOrigin = glyf.Value.I16(at + 2) - hmtx.Value.I16(lsbAt);
            return baseOrigin + (_glyphVariations.Get(glyph)?.LeftPhantomDelta ?? 0);
        } catch (FontLayoutException) { return 0; }
    }
    // MVAR's hasc/hdsc target OS/2 typo metrics, not arbitrary hhea values. Follow a hhea
    // value only when the face declares the same corresponding typo metric.
    private double AscentVariation => _os2 >= 0 && InBounds(_os2 + 68, 2) && ReadInt16(_data, _os2 + 68) == _ascender ? MetricVariation("hasc") : 0;
    private double DescentVariation => _os2 >= 0 && InBounds(_os2 + 70, 2) && ReadInt16(_data, _os2 + 70) == _descender ? MetricVariation("hdsc") : 0;
}

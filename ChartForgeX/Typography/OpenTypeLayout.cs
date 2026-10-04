using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

/// <summary>Dependency-free GSUB/GPOS execution over a bounded glyph buffer, selected by script and language.</summary>
internal sealed partial class OpenTypeLayout {
    private readonly FontTableReader? _gsub, _gpos, _gdef;
    private readonly int _glyphCount;
    private readonly string? _languageTag;
    private readonly Dictionary<string, LayoutPlan> _plans = new(StringComparer.Ordinal);

    internal OpenTypeLayout(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, int glyphCount) {
        _gsub = Table("GSUB"); _gpos = Table("GPOS"); _gdef = Table("GDEF"); _glyphCount = glyphCount;
        FontTableReader? Table(string tag) {
            if (!tables.TryGetValue(tag, out var start) || !lengths.TryGetValue(tag, out var length)) return null;
            try { return new FontTableReader(data, start, length); } catch (FontLayoutException) { return null; }
        }
    }

    private OpenTypeLayout(OpenTypeLayout source, string? languageTag) {
        _gsub = source._gsub; _gpos = source._gpos; _gdef = source._gdef;
        _glyphCount = source._glyphCount; _languageTag = languageTag;
    }

    /// <summary>Shares table bytes while isolating immutable language selection and its cached plans.</summary>
    internal OpenTypeLayout WithLanguage(string? tag) => new(this, tag);
    internal bool HasFeature(string script, string feature, bool positioning = false) {
        var plan = Plan(script, positioning);
        return plan.Features.ContainsKey(feature) || plan.RequiredTag == feature;
    }

    internal bool HasLayout(string script) => Plan(script, false).Features.Count != 0 || Plan(script, false).Required.Count != 0 || Plan(script, true).Features.Count != 0 || Plan(script, true).Required.Count != 0;

    /// <summary>Distinguishes a declared script from the DFLT fallback when selecting modern Indic tags.</summary>
    internal bool HasScript(string script, bool positioning = false) => Plan(script, positioning).Script == script;

    /// <summary>Font-classified spacing marks are zeroed before script-specific distance adjustments.</summary>
    internal bool IsMarkGlyph(LayoutGlyph glyph) {
        try { return GlyphClass(glyph) == 3; } catch (FontLayoutException) { return glyph.IsMark; }
    }

    /// <summary>Applies a feature stage in lookup-list order, once per lookup even when shared by several features.</summary>
    internal void Apply(List<LayoutGlyph> glyphs, string script, IReadOnlyList<string> features, bool positioning = false, bool required = false, bool rightToLeft = false, LayoutExecution? budget = null) {
        var table = positioning ? _gpos : _gsub;
        if (!table.HasValue || glyphs.Count == 0) return;
        var plan = Plan(script, positioning);
        var selected = new SortedDictionary<int, List<string>>();
        if (required) foreach (var index in plan.Required) selected[index] = new List<string> { "" };
        foreach (var feature in features) {
            if (!plan.Features.TryGetValue(feature, out var indexes)) continue;
            foreach (var index in indexes) {
                if (!selected.TryGetValue(index, out var tags)) selected[index] = tags = new List<string>();
                if (!tags.Contains(feature)) tags.Add(feature);
            }
        }
        var execution = budget ?? new LayoutExecution(glyphs.Count, rightToLeft);
        execution.Prepare(glyphs.Count); var originalCount = glyphs.Count;
        foreach (var lookup in selected) {
            try {
                var at = Lookup(table.Value, plan.LookupList, lookup.Key);
                var reverse = !positioning && LookupType(table.Value, at) == 8;
                if (reverse) {
                    for (var i = glyphs.Count - 1; i >= 0; i--) if (Enabled(glyphs[i], lookup.Value)) Execute(table.Value, plan.LookupList, at, glyphs, i, positioning, execution, 0);
                } else {
                    for (var i = 0; i < glyphs.Count;) {
                        var next = Enabled(glyphs[i], lookup.Value) ? Execute(table.Value, plan.LookupList, at, glyphs, i, positioning, execution, 0) : -1;
                        i = next < 0 ? i + 1 : next;
                    }
                }
            } catch (FontLayoutException) { /* Keep the usable glyph sequence when an optional lookup is malformed. */ }
            if (execution.Remaining <= 0) break;
        }
        execution.AccountGrowth(glyphs.Count - originalCount);
    }
    private static bool Enabled(LayoutGlyph glyph, List<string> features) {
        foreach (var feature in features) if (glyph.Allows(feature)) return true;
        return false;
    }
    private LayoutPlan Plan(string script, bool positioning) {
        var key = (positioning ? "p:" : "s:") + script;
        lock (_plans) {
            if (_plans.TryGetValue(key, out var cached)) return cached;
            var table = positioning ? _gpos : _gsub;
            var plan = new LayoutPlan();
            if (table.HasValue) {
                try { ReadPlan(table.Value, script, _languageTag, plan); } catch (FontLayoutException) { plan = new LayoutPlan(); }
            }
            return _plans[key] = plan;
        }
    }
    private static void ReadPlan(FontTableReader table, string tag, string? languageTag, LayoutPlan plan) {
        if (table.U16(0) != 1) return;
        var scripts = table.Offset(0, 4); var features = table.Offset(0, 6); plan.LookupList = table.Offset(0, 8);
        var scriptCount = table.U16(scripts); table.Require(scripts + 2, scriptCount * 6);
        var selected = -1; var fallback = -1;
        for (var i = 0; i < scriptCount; i++) {
            var record = scripts + 2 + i * 6; var scriptTag = table.Tag(record);
            if (scriptTag == tag) selected = table.Offset(scripts, record + 4);
            if (scriptTag == "DFLT") fallback = table.Offset(scripts, record + 4);
        }
        plan.Script = selected >= 0 ? tag : "DFLT";
        if (selected < 0) selected = fallback;
        if (selected < 0) return;
        var language = table.Offset(selected, selected, optional: true);
        if (languageTag != null) {
            var languageCount = table.U16(selected + 2); table.Require(selected + 4, languageCount * 6);
            for (var i = 0; i < languageCount; i++) {
                var record = selected + 4 + i * 6;
                if (table.Tag(record) == languageTag) { language = table.Offset(selected, record + 4); break; }
            }
        }
        if (language < 0) return;
        var featureCount = table.U16(features); table.Require(features + 2, featureCount * 6);
        var required = table.U16(language + 2); var count = table.U16(language + 4); table.Require(language + 6, count * 2);
        if (required != 0xffff) Add(required, true);
        for (var i = 0; i < count; i++) Add(table.U16(language + 6 + i * 2), false);
        void Add(int index, bool isRequired) {
            if (index >= featureCount) throw new FontLayoutException();
            var record = features + 2 + index * 6; var feature = table.Offset(features, record + 4);
            var lookupCount = table.U16(feature + 2); table.Require(feature + 4, lookupCount * 2);
            var indexes = new int[lookupCount];
            for (var i = 0; i < lookupCount; i++) indexes[i] = table.U16(feature + 4 + i * 2);
            var featureTag = table.Tag(record);
            if (isRequired) { plan.Required.AddRange(indexes); plan.RequiredTag = featureTag; return; }
            if (plan.Features.TryGetValue(featureTag, out var old)) { var combined = new int[old.Length + indexes.Length]; old.CopyTo(combined, 0); indexes.CopyTo(combined, old.Length); indexes = combined; }
            plan.Features[featureTag] = indexes;
        }
    }
    private static int Lookup(FontTableReader table, int list, int index) {
        var count = table.U16(list);
        if (index < 0 || index >= count) throw new FontLayoutException();
        table.Require(list + 2, count * 2);
        return table.Offset(list, list + 2 + index * 2);
    }
    private static int LookupType(FontTableReader table, int lookup) {
        var type = table.U16(lookup);
        if (type == 7 && table.U16(lookup + 4) != 0) {
            var subtable = table.Offset(lookup, lookup + 6);
            if (table.U16(subtable) == 1) return table.U16(subtable + 2);
        }
        return type;
    }
    private int Execute(FontTableReader table, int list, int lookup, List<LayoutGlyph> glyphs, int index, bool positioning, LayoutExecution execution, int depth) {
        if (depth >= 16 || --execution.Remaining < 0 || index < 0 || index >= glyphs.Count) return -1;
        var type = table.U16(lookup); var flags = table.U16(lookup + 2); var count = table.U16(lookup + 4);
        table.Require(lookup + 6, count * 2);
        var filter = (flags & 16) != 0 ? table.U16(lookup + 6 + count * 2) : -1;
        if (Skipped(glyphs[index], flags, filter)) return -1;
        for (var i = 0; i < count; i++) {
            var subtable = table.Offset(lookup, lookup + 6 + i * 2);
            var actualType = type;
            if (type == (positioning ? 9 : 7)) {
                if (table.U16(subtable) != 1) continue;
                actualType = table.U16(subtable + 2);
                if (actualType == type) throw new FontLayoutException();
                subtable = table.Offset(subtable, subtable + 4, wide: true);
            }
            var next = positioning ? Position(table, list, actualType, subtable, glyphs, index, flags, filter, execution, depth)
                : Substitute(table, list, actualType, subtable, glyphs, index, flags, filter, execution, depth);
            if (next >= 0) return next; // The first matching subtable, not the sum of alternative subtables.
        }
        return -1;
    }
    private bool Skipped(LayoutGlyph glyph, int flags, int filter) {
        if (glyph.SkipForSubstitution) return true;
        var kind = GlyphClass(glyph);
        if (kind == 1 && (flags & 2) != 0 || kind == 2 && (flags & 4) != 0 || kind == 3 && (flags & 8) != 0) return true;
        if (kind != 3 || !_gdef.HasValue) return false;
        var table = _gdef.Value;
        if ((flags & 16) != 0) {
            if (table.U16(0) != 1 || table.U16(2) < 2) return true;
            var sets = table.Offset(0, 12, optional: true);
            if (sets < 0 || table.U16(sets) != 1 || filter < 0 || filter >= table.U16(sets + 2)) return true;
            return table.Coverage(table.Offset(sets, sets + 4 + filter * 4, wide: true), glyph.Glyph) < 0;
        }
        var attachment = flags >> 8;
        return attachment != 0 && table.Class(table.Offset(0, 10, optional: true), glyph.Glyph) != attachment;
    }
    private int GlyphClass(LayoutGlyph glyph) {
        if (_gdef.HasValue) {
            var table = _gdef.Value; var classes = table.Offset(0, 4, optional: true);
            if (classes >= 0) return table.Class(classes, glyph.Glyph);
        }
        return glyph.IsMark ? 3 : glyph.ComponentClusters != null ? 2 : 1;
    }
    private int Next(List<LayoutGlyph> glyphs, int index, int direction, int flags, int filter, LayoutExecution execution) {
        do {
            if (--execution.Remaining < 0) return -1;
            index += direction;
        } while (index >= 0 && index < glyphs.Count && Skipped(glyphs[index], flags, filter));
        return index >= 0 && index < glyphs.Count ? index : -1;
    }
    private ushort ValidGlyph(int glyph) {
        if (glyph < 0 || glyph >= _glyphCount) throw new FontLayoutException();
        return (ushort)glyph;
    }
    private sealed class LayoutPlan {
        internal string? Script;
        internal int LookupList;
        internal readonly List<int> Required = new();
        internal string? RequiredTag;
        internal readonly Dictionary<string, int[]> Features = new(StringComparer.Ordinal);
    }
    /// <summary>Shares finite lookup work and glyph growth across every feature stage in one font run.</summary>
    internal sealed class LayoutExecution {
        private long _availableGrowth;
        internal LayoutExecution(int count, bool rightToLeft, double pixelSize = 0, int unitsPerEm = 0) {
            Remaining = (int)Math.Min(16000000L, Math.Max(4096L, (long)count * 1024));
            _availableGrowth = (long)count * 7 + 64; Prepare(count); RightToLeft = rightToLeft;
            PixelSize = pixelSize; UnitsPerEm = unitsPerEm;
        }
        internal void Prepare(int count) {
            MaximumGlyphs = (int)Math.Min(int.MaxValue, (long)count + Math.Max(0, _availableGrowth));
            PreviousBases = null;
        }
        internal int[]? PreviousBases;
        internal int BaseFlags, BaseFilter;
        internal void AccountGrowth(int change) => _availableGrowth -= change;
        internal int Remaining;
        internal int MaximumGlyphs;
        internal readonly bool RightToLeft;
        internal readonly double PixelSize;
        internal readonly int UnitsPerEm;
    }
}

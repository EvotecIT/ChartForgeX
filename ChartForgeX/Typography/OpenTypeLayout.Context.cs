using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    /// <summary>GSUB and GPOS share all three glyph/class/coverage contextual table formats.</summary>
    private int Context(FontTableReader table, int list, int at, List<LayoutGlyph> glyphs, int index, int flags, int filter, bool chained, bool positioning, LayoutExecution execution, int depth) {
        var format = table.U16(at);
        if (format == 3) return CoverageContext(table, list, at, glyphs, index, flags, filter, chained, positioning, execution, depth);
        if (format != 1 && format != 2) return -1;
        var covered = table.Coverage(table.Offset(at, at + 2), glyphs[index].Glyph);
        if (covered < 0) return -1;
        var inputClass = -1; var beforeClass = -1; var afterClass = -1;
        var countAt = at + 4;
        if (format == 2) {
            if (chained) {
                beforeClass = table.Offset(at, at + 4); inputClass = table.Offset(at, at + 6); afterClass = table.Offset(at, at + 8);
                countAt = at + 10;
            } else { inputClass = table.Offset(at, at + 4); countAt = at + 6; }
            covered = table.Class(inputClass, glyphs[index].Glyph);
        }
        var setCount = table.U16(countAt); table.Require(countAt + 2, setCount * 2);
        if (covered >= setCount) return -1;
        var set = table.Offset(at, countAt + 2 + covered * 2, optional: true);
        if (set < 0) return -1;
        var ruleCount = table.U16(set); table.Require(set + 2, ruleCount * 2);
        for (var r = 0; r < ruleCount; r++) {
            if (--execution.Remaining < 0) return -1;
            var rule = table.Offset(set, set + 2 + r * 2); var cursor = rule;
            if (chained) {
                var beforeCount = table.U16(cursor); cursor += 2;
                if (!Match(table, cursor, beforeCount, glyphs, index, -1, flags, filter, format, beforeClass, null)) continue;
                cursor += beforeCount * 2;
            }
            var inputCount = table.U16(cursor); cursor += 2;
            var recordCount = chained ? -1 : table.U16(cursor);
            if (!chained) cursor += 2;
            if (inputCount < 1 || inputCount > 256) throw new FontLayoutException();
            var inputs = new List<LayoutGlyph> { glyphs[index] };
            if (!Match(table, cursor, inputCount - 1, glyphs, index, 1, flags, filter, format, inputClass, inputs)) continue;
            cursor += (inputCount - 1) * 2;
            if (chained) {
                var afterCount = table.U16(cursor); cursor += 2;
                var last = glyphs.IndexOf(inputs[inputs.Count - 1]);
                if (!Match(table, cursor, afterCount, glyphs, last, 1, flags, filter, format, afterClass, null)) continue;
                cursor += afterCount * 2; recordCount = table.U16(cursor); cursor += 2;
            }
            return ApplyContext(table, list, cursor, recordCount, inputs, glyphs, positioning, execution, depth);
        }
        return -1;
    }
    private bool Match(FontTableReader table, int at, int count, List<LayoutGlyph> glyphs, int index, int direction, int flags, int filter, int format, int classAt, List<LayoutGlyph>? matches) {
        if (count > 256) throw new FontLayoutException();
        table.Require(at, count * 2);
        for (var i = 0; i < count; i++) {
            index = Next(glyphs, index, direction, flags, filter);
            if (index < 0) return false;
            var actual = format == 1 ? glyphs[index].Glyph : table.Class(classAt, glyphs[index].Glyph);
            if (actual != table.U16(at + i * 2)) return false;
            matches?.Add(glyphs[index]);
        }
        return true;
    }
    private int CoverageContext(FontTableReader table, int list, int at, List<LayoutGlyph> glyphs, int index, int flags, int filter, bool chained, bool positioning, LayoutExecution execution, int depth) {
        var cursor = at + 2;
        if (chained) {
            var beforeCount = table.U16(cursor); cursor += 2;
            if (!MatchCoverages(table, at, cursor, beforeCount, glyphs, index, -1, flags, filter, false, null)) return -1;
            cursor += beforeCount * 2;
        }
        var inputCount = table.U16(cursor); cursor += 2;
        var recordCount = chained ? -1 : table.U16(cursor);
        if (!chained) cursor += 2;
        if (inputCount == 0) throw new FontLayoutException();
        var inputs = new List<LayoutGlyph>();
        if (!MatchCoverages(table, at, cursor, inputCount, glyphs, index, 1, flags, filter, true, inputs)) return -1;
        cursor += inputCount * 2;
        if (chained) {
            var afterCount = table.U16(cursor); cursor += 2;
            if (!MatchCoverages(table, at, cursor, afterCount, glyphs, glyphs.IndexOf(inputs[inputs.Count - 1]), 1, flags, filter, false, null)) return -1;
            cursor += afterCount * 2; recordCount = table.U16(cursor); cursor += 2;
        }
        return ApplyContext(table, list, cursor, recordCount, inputs, glyphs, positioning, execution, depth);
    }
    private int ApplyContext(FontTableReader table, int list, int at, int count, List<LayoutGlyph> inputs, List<LayoutGlyph> glyphs, bool positioning, LayoutExecution execution, int depth) {
        var last = glyphs.IndexOf(inputs[inputs.Count - 1]);
        var following = last + 1 < glyphs.Count ? glyphs[last + 1] : null;
        ApplyRecords(table, list, at, count, inputs, glyphs, positioning, execution, depth);
        // A matched context consumes its input sequence. Track glyph identities across expansion and ligation.
        if (following == null) return glyphs.Count;
        var next = glyphs.IndexOf(following);
        if (next >= 0) return next;
        var end = -1;
        foreach (var input in inputs) end = Math.Max(end, glyphs.IndexOf(input));
        return end >= 0 ? end + 1 : glyphs.Count;
    }
    private bool MatchCoverages(FontTableReader table, int origin, int at, int count, List<LayoutGlyph> glyphs, int index, int direction, int flags, int filter, bool includeFirst, List<LayoutGlyph>? matches) {
        if (count > 256) throw new FontLayoutException();
        table.Require(at, count * 2);
        for (var i = 0; i < count; i++) {
            if (!includeFirst || i > 0) index = Next(glyphs, index, direction, flags, filter);
            if (index < 0 || table.Coverage(table.Offset(origin, at + i * 2), glyphs[index].Glyph) < 0) return false;
            matches?.Add(glyphs[index]);
        }
        return true;
    }
    private void ApplyRecords(FontTableReader table, int list, int at, int count, List<LayoutGlyph> inputs, List<LayoutGlyph> glyphs, bool positioning, LayoutExecution execution, int depth) {
        if (count > 256) throw new FontLayoutException();
        table.Require(at, count * 4);
        for (var i = 0; i < count; i++) {
            var sequence = table.U16(at + i * 4);
            if (sequence >= inputs.Count) throw new FontLayoutException();
            var index = glyphs.IndexOf(inputs[sequence]);
            if (index >= 0) Execute(table, list, Lookup(table, list, table.U16(at + i * 4 + 2)), glyphs, index, positioning, execution, depth + 1);
        }
    }
}

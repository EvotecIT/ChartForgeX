using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    private int Substitute(FontTableReader table, int list, int type, int at, List<LayoutGlyph> glyphs, int index, int flags, int filter, LayoutExecution execution, int depth) {
        if (type == 5 || type == 6) return Context(table, list, at, glyphs, index, flags, filter, type == 6, false, execution, depth);
        var format = table.U16(at);
        var covered = table.Coverage(table.Offset(at, at + 2), glyphs[index].Glyph);
        if (covered < 0) return -1;
        switch (type) {
            case 1:
                var replacement = format == 1 ? (glyphs[index].Glyph + table.I16(at + 4)) & 0xffff
                    : format == 2 && covered < table.U16(at + 4) ? table.U16(at + 6 + covered * 2) : -1;
                glyphs[index].Glyph = ValidGlyph(replacement);
                return index + 1;
            case 2:
            case 3:
                if (format != 1 || covered >= table.U16(at + 4)) return -1;
                var sequence = table.Offset(at, at + 6 + covered * 2); var count = table.U16(sequence);
                table.Require(sequence + 2, count * 2);
                if (type == 3) {
                    if (count == 0) return -1;
                    glyphs[index].Glyph = ValidGlyph(table.U16(sequence + 2));
                    return index + 1;
                }
                if ((long)glyphs.Count + count - 1 > execution.MaximumGlyphs) throw new FontLayoutException();
                var outputs = new ushort[count];
                for (var i = 0; i < count; i++) outputs[i] = ValidGlyph(table.U16(sequence + 2 + i * 2));
                if (count == 0) { glyphs.RemoveAt(index); return index; }
                var original = glyphs[index]; original.Glyph = outputs[0];
                for (var i = 1; i < count; i++) glyphs.Insert(index + i, original.Copy(outputs[i]));
                return index + count;
            case 4:
                if (format != 1 || covered >= table.U16(at + 4)) return -1;
                return Ligate(table, table.Offset(at, at + 6 + covered * 2), glyphs, index, flags, filter, execution);
            case 8:
                if (format != 1) return -1;
                return ReverseSubstitute(table, at, covered, glyphs, index, flags, filter, execution);
            default: return -1;
        }
    }
    private int Ligate(FontTableReader table, int set, List<LayoutGlyph> glyphs, int index, int flags, int filter, LayoutExecution execution) {
        var count = table.U16(set); table.Require(set + 2, count * 2);
        for (var i = 0; i < count; i++) {
            if (--execution.Remaining < 0) return -1;
            var ligature = table.Offset(set, set + 2 + i * 2); var replacement = ValidGlyph(table.U16(ligature));
            var components = table.U16(ligature + 2);
            if (components == 0 || components > 256) throw new FontLayoutException();
            table.Require(ligature + 4, (components - 1) * 2);
            var positions = new List<int> { index }; var current = index; var matched = true;
            for (var c = 1; c < components; c++) {
                current = Next(glyphs, current, 1, flags, filter, execution);
                if (current < 0 || glyphs[current].Glyph != table.U16(ligature + 2 + c * 2)) { matched = false; break; }
                positions.Add(current);
            }
            if (!matched) continue;
            var first = glyphs[index]; var clusters = new List<int>();
            foreach (var position in positions) {
                var component = glyphs[position];
                if (component.ComponentClusters != null) clusters.AddRange(component.ComponentClusters);
                else clusters.Add(component.Cluster);
                first.Cluster = Math.Min(first.Cluster, component.Cluster);
            }
            // Marks skipped by IgnoreMarks remain in the buffer and retain their ligature component.
            var componentIndex = 0;
            for (var c = 0; c < positions.Count; c++) {
                var componentCount = glyphs[positions[c]].ComponentClusters?.Length ?? 1;
                if (c + 1 < positions.Count) for (var m = positions[c] + 1; m < positions[c + 1]; m++) {
                    glyphs[m].Ligature = first; glyphs[m].Component = componentIndex + componentCount - 1;
                    glyphs[m].Cluster = first.Cluster;
                }
                componentIndex += componentCount;
            }
            for (var m = positions[positions.Count - 1] + 1; m < glyphs.Count && GlyphClass(glyphs[m]) == 3; m++) {
                glyphs[m].Ligature = first; glyphs[m].Component = clusters.Count - 1; glyphs[m].Cluster = first.Cluster;
            }
            var visible = false;
            foreach (var position in positions) if (!glyphs[position].Ignorable) visible = true;
            first.Glyph = replacement; first.Ignorable = !visible; first.ComponentClusters = clusters.ToArray();
            for (var c = positions.Count - 1; c > 0; c--) glyphs.RemoveAt(positions[c]);
            return index + 1;
        }
        return -1;
    }
    private int ReverseSubstitute(FontTableReader table, int at, int covered, List<LayoutGlyph> glyphs, int index, int flags, int filter, LayoutExecution execution) {
        var cursor = at + 4; var beforeCount = table.U16(cursor); cursor += 2;
        if (beforeCount > 256) throw new FontLayoutException();
        var previous = index;
        for (var i = 0; i < beforeCount; i++) {
            previous = Next(glyphs, previous, -1, flags, filter, execution);
            if (previous < 0 || table.Coverage(table.Offset(at, cursor + i * 2), glyphs[previous].Glyph) < 0) return -1;
        }
        cursor += beforeCount * 2; var afterCount = table.U16(cursor); cursor += 2;
        if (afterCount > 256) throw new FontLayoutException();
        var next = index;
        for (var i = 0; i < afterCount; i++) {
            next = Next(glyphs, next, 1, flags, filter, execution);
            if (next < 0 || table.Coverage(table.Offset(at, cursor + i * 2), glyphs[next].Glyph) < 0) return -1;
        }
        cursor += afterCount * 2;
        if (covered >= table.U16(cursor)) return -1;
        glyphs[index].Glyph = ValidGlyph(table.U16(cursor + 2 + covered * 2));
        return index + 1;
    }
}

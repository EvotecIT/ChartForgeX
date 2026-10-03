using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    private int Position(FontTableReader table, int list, int type, int at, List<LayoutGlyph> glyphs, int index, int flags, int filter, LayoutExecution execution, int depth) {
        if (type == 7 || type == 8) return Context(table, list, at, glyphs, index, flags, filter, type == 8, true, execution, depth);
        var format = table.U16(at); var covered = table.Coverage(table.Offset(at, at + 2), glyphs[index].Glyph);
        if (covered < 0) return -1;
        if (type == 1) {
            var valueFormat = table.U16(at + 4); var size = ValueSize(valueFormat);
            if (format == 1) ApplyValue(table, at + 6, valueFormat, glyphs[index]);
            else if (format == 2 && covered < table.U16(at + 6)) ApplyValue(table, at + 8 + covered * size, valueFormat, glyphs[index]);
            else return -1;
            return index + 1;
        }
        if (type == 2) return Pair(table, at, covered, glyphs, index, flags, filter);
        if (type == 3 && format == 1) return Cursive(table, at, covered, glyphs, index, flags, filter, execution.RightToLeft);
        if (type >= 4 && type <= 6 && format == 1) return Mark(table, at, covered, glyphs, index, type, flags, filter);
        return -1;
    }
    private int Pair(FontTableReader table, int at, int covered, List<LayoutGlyph> glyphs, int index, int flags, int filter) {
        var second = Next(glyphs, index, 1, flags, filter);
        if (second < 0) return -1;
        var format = table.U16(at); var firstFormat = table.U16(at + 4); var secondFormat = table.U16(at + 6);
        var firstSize = ValueSize(firstFormat); var size = firstSize + ValueSize(secondFormat); var record = -1;
        if (format == 1) {
            if (covered >= table.U16(at + 8)) return -1;
            var set = table.Offset(at, at + 10 + covered * 2); var count = table.U16(set); table.Require(set + 2, count * (size + 2));
            var low = 0; var high = count - 1;
            while (low <= high) {
                var middle = low + (high - low) / 2; var candidate = set + 2 + middle * (size + 2); var glyph = table.U16(candidate);
                if (glyph < glyphs[second].Glyph) low = middle + 1;
                else if (glyph > glyphs[second].Glyph) high = middle - 1;
                else { record = candidate + 2; break; }
            }
        } else if (format == 2) {
            var class1 = table.Class(table.Offset(at, at + 8), glyphs[index].Glyph); var class2 = table.Class(table.Offset(at, at + 10), glyphs[second].Glyph);
            var firstCount = table.U16(at + 12); var secondCount = table.U16(at + 14);
            if (class1 >= firstCount || class2 >= secondCount) throw new FontLayoutException();
            var relative = 16L + ((long)class1 * secondCount + class2) * size;
            if (relative > int.MaxValue - at) throw new FontLayoutException();
            record = at + (int)relative;
        }
        if (record < 0) return -1;
        table.Require(record, size);
        ApplyValue(table, record, firstFormat, glyphs[index]); ApplyValue(table, record + firstSize, secondFormat, glyphs[second]);
        return secondFormat == 0 ? index + 1 : second + 1;
    }
    private static int ValueSize(int format) {
        if ((format & ~0xff) != 0) throw new FontLayoutException();
        var size = 0;
        for (var bit = 1; bit <= 128; bit <<= 1) if ((format & bit) != 0) size += 2;
        return size;
    }
    private static void ApplyValue(FontTableReader table, int at, int format, LayoutGlyph glyph) {
        table.Require(at, ValueSize(format));
        if ((format & 1) != 0) { var value = table.I16(at); glyph.XOffset += value; if (glyph.Attachment != null) glyph.AttachmentAdjustmentX += value; at += 2; }
        if ((format & 2) != 0) { var value = table.I16(at); glyph.YOffset += value; if (glyph.Attachment != null) glyph.AttachmentAdjustmentY += value; at += 2; }
        if ((format & 4) != 0) { glyph.XAdvance += table.I16(at); at += 2; }
        if ((format & 8) != 0) glyph.YAdvance += table.I16(at);
        // Device/variation offsets occupy their records but the engine renders the default design instance.
    }
    private static (double X, double Y)? Anchor(FontTableReader table, int origin, int field) {
        var at = table.Offset(origin, field, optional: true);
        if (at < 0) return null;
        var format = table.U16(at);
        table.Require(at, format == 1 ? 6 : format == 2 ? 8 : format == 3 ? 10 : throw new FontLayoutException());
        return (table.I16(at + 2), table.I16(at + 4));
    }
    private int Cursive(FontTableReader table, int at, int covered, List<LayoutGlyph> glyphs, int index, int flags, int filter, bool rightToLeft) {
        var second = Next(glyphs, index, 1, flags, filter);
        if (second < 0 || covered >= table.U16(at + 4)) return -1;
        var nextCovered = table.Coverage(table.Offset(at, at + 2), glyphs[second].Glyph);
        if (nextCovered < 0 || nextCovered >= table.U16(at + 4)) return -1;
        var exit = Anchor(table, at, at + 8 + covered * 4); var entry = Anchor(table, at, at + 6 + nextCovered * 4);
        if (!exit.HasValue || !entry.HasValue) return -1;
        var first = glyphs[index]; var next = glyphs[second];
        if (rightToLeft) {
            first.XAdvance -= exit.Value.X + first.XOffset;
            first.XOffset = -exit.Value.X;
            next.XAdvance = entry.Value.X + next.XOffset;
        } else {
            first.XAdvance = exit.Value.X + first.XOffset;
            next.XAdvance -= entry.Value.X + next.XOffset;
            next.XOffset = -entry.Value.X;
        }
        if ((flags & 1) != 0) Attach(first, next, 0, entry.Value.Y - exit.Value.Y, cursive: true);
        else Attach(next, first, 0, exit.Value.Y - entry.Value.Y, cursive: true);
        return index + 1;
    }
    private int Mark(FontTableReader table, int at, int covered, List<LayoutGlyph> glyphs, int index, int type, int flags, int filter) {
        var classes = table.U16(at + 6); var markArray = table.Offset(at, at + 8); var baseArray = table.Offset(at, at + 10);
        if (covered >= table.U16(markArray)) return -1;
        var markRecord = markArray + 2 + covered * 4; var markClass = table.U16(markRecord);
        if (markClass >= classes) throw new FontLayoutException();
        var markAnchor = Anchor(table, markArray, markRecord + 2);
        if (!markAnchor.HasValue) return -1;
        var previous = Next(glyphs, index, -1, flags, filter);
        // Base/ligature attachment skips marks; mark-to-mark stops at the preceding non-mark.
        while (previous >= 0 && type != 6 && (GlyphClass(glyphs[previous]) == 3 || glyphs[previous].Ignorable)) previous = Next(glyphs, previous, -1, flags, filter);
        if (previous < 0 || type == 6 && GlyphClass(glyphs[previous]) != 3) return -1;
        if (type == 6 && (!ReferenceEquals(glyphs[index].Ligature, glyphs[previous].Ligature) || glyphs[index].Component != glyphs[previous].Component)) return -1;
        var baseIndex = table.Coverage(table.Offset(at, at + 4), glyphs[previous].Glyph);
        if (baseIndex < 0 || baseIndex >= table.U16(baseArray)) return -1;
        (double X, double Y)? baseAnchor;
        if (type == 5) {
            var ligature = table.Offset(baseArray, baseArray + 2 + baseIndex * 2); var componentCount = table.U16(ligature);
            if (componentCount == 0) return -1;
            var mark = glyphs[index]; var parent = glyphs[previous]; var component = componentCount - 1;
            if (ReferenceEquals(mark.Ligature, parent)) component = Math.Min(component, mark.Component);
            else if (parent.ComponentClusters != null) {
                for (var c = 0; c < Math.Min(componentCount, parent.ComponentClusters.Length); c++) {
                    if (parent.ComponentClusters[c] <= mark.Cluster) component = c;
                }
            }
            baseAnchor = Anchor(table, ligature, table.Record(ligature + 2, (long)component * classes + markClass, 2));
        } else baseAnchor = Anchor(table, baseArray, table.Record(baseArray + 2, (long)baseIndex * classes + markClass, 2));
        if (!baseAnchor.HasValue) return -1;
        Attach(glyphs[index], glyphs[previous], baseAnchor.Value.X - markAnchor.Value.X, baseAnchor.Value.Y - markAnchor.Value.Y, cursive: false);
        return index + 1;
    }
    private static void Attach(LayoutGlyph child, LayoutGlyph parent, double x, double y, bool cursive) {
        child.Attachment = parent; child.AnchorX = x; child.AnchorY = y; child.CursiveAttachment = cursive;
        child.AttachmentAdjustmentX = child.AttachmentAdjustmentY = 0;
    }
    /// <summary>Resolves attachment chains after all advance and placement adjustments, without recursive font-controlled calls.</summary>
    internal static void ResolveAttachments(List<LayoutGlyph> glyphs) {
        var indexes = new Dictionary<LayoutGlyph, int>(); var penX = new double[glyphs.Count]; var penY = new double[glyphs.Count];
        var x = 0.0; var y = 0.0;
        for (var i = 0; i < glyphs.Count; i++) { indexes[glyphs[i]] = i; penX[i] = x; penY[i] = y; x += glyphs[i].XAdvance; y += glyphs[i].YAdvance; }
        var done = new HashSet<LayoutGlyph>();
        foreach (var glyph in glyphs) {
            if (done.Contains(glyph)) continue;
            var chain = new List<LayoutGlyph>(); var visiting = new HashSet<LayoutGlyph>(); var current = glyph;
            while (!done.Contains(current) && current.Attachment != null && indexes.ContainsKey(current.Attachment) && visiting.Add(current)) {
                chain.Add(current); current = current.Attachment;
            }
            if (visiting.Contains(current)) { foreach (var item in chain) item.Attachment = null; }
            for (var i = chain.Count - 1; i >= 0; i--) {
                var child = chain[i]; var parent = child.Attachment;
                if (parent != null) {
                    if (!child.CursiveAttachment) child.XOffset = parent.XOffset + child.AnchorX + child.AttachmentAdjustmentX + penX[indexes[parent]] - penX[indexes[child]];
                    child.YOffset = parent.YOffset + child.AnchorY + child.AttachmentAdjustmentY + penY[indexes[parent]] - penY[indexes[child]];
                }
                done.Add(child);
            }
            done.Add(current);
        }
    }
}
